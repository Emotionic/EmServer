﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Effekseer;
using Kinect = Windows.Kinect;

namespace Assets.KinectView.Scripts
{
    public class EffectsFromGesture : MonoBehaviour
    {
        /// <summary>
        /// TrailRendererを表示するためのマテリアル
        /// </summary>
        public Material TrailMaterial;

        public GameObject GestureManager;
        
        public GameObject BodySourceManager;

        public GameObject Trail;

        private WSServer _WSServer;

        private Camera _MainCamera;

        private Dictionary<string, EffectOption> _EffectsCustomize;

        /// <summary>
        /// エフェクト名
        /// </summary>
        private readonly string[] _EffectNames = { "StairBroken", "punch", "linetrail_ver2" };
        
        private GestureManager _GestureManager;

        private bool _IsRegMethod = false;

        /// <summary>
        /// Kinect画像と取得した関節情報を表示する
        /// </summary>
        private ColorBodySourceView _ColorBodyView;

        private BodySourceManager _BodyManager;

        private Dictionary<ulong, Dictionary<JointType, GameObject>> _Joints;

        private Dictionary<string, EffectAttributes> _GestureFromEffectAttributes;

        private RainbowColor _RbColor;

        // Use this for initialization
        void Start()
        {
            // loadEffect
            foreach (var efkName in _EffectNames)
                EffekseerSystem.LoadEffect(efkName);

            _MainCamera = GameObject.Find("ConvertCamera").GetComponent<Camera>();

            if (GameObject.Find("WSServer") != null)
            {
                _WSServer = GameObject.Find("WSServer").GetComponent<WSServer>();

                _WSServer.Like += _WSServer_Like;

                _WSServer.Customize += _WSServer_Customize;
            }

            _GestureManager = GestureManager.GetComponent<GestureManager>();
            _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
            _ColorBodyView = BodySourceManager.GetComponent<ColorBodySourceView>();

            // Add effect attributes
            _GestureFromEffectAttributes = new Dictionary<string, EffectAttributes>();
            _GestureFromEffectAttributes["Jump02"] = new EffectAttributes(0.6, JointType.SpineMid, _EffectNames[0]);
            _GestureFromEffectAttributes["Punch_Left"] = new EffectAttributes(0.2, JointType.HandRight, _EffectNames[1]);
            _GestureFromEffectAttributes["Punch_Right"] = new EffectAttributes(0.2, JointType.HandLeft, _EffectNames[1]);

            _RbColor = new RainbowColor();
        }

        private void _WSServer_Customize(CustomData data)
        {
            _EffectsCustomize = JsonUtility.FromJson<Serialization<string, EffectOption>>(data.EffectsCustomize).ToDictionary();
        }

        private void _WSServer_Like(LikeData data)
        {
            switch(data.name)
            {
                case "heart":
                    Debug.Log("HEART");
                    _MainCamera.backgroundColor = data.color;
                    break;
                case "star":
                    Debug.Log("STAR");
                    _MainCamera.backgroundColor = data.color;
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_GestureManager == null || _ColorBodyView == null || _BodyManager == null)
            {
                _GestureManager = GestureManager.GetComponent<GestureManager>();
                _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
                _ColorBodyView = BodySourceManager.GetComponent<ColorBodySourceView>();
            }

            if (_MainCamera == null)
                _MainCamera = GameObject.Find("ConvertCamera").GetComponent<Camera>();

            if(!_IsRegMethod)
            {
                Debug.Log("REG");
                _GestureManager.GestureDetected += _GestureManager_GestureDetected;
                _IsRegMethod = true;
            }

            _Joints = _ColorBodyView.JointsFromBodies;
            
            foreach (GameObject body in _ColorBodyView.GetBodies())
            {
                AddingTrailRendererToBody(body);
            }

            _RbColor.Update();
        }

        private void _GestureManager_GestureDetected(KeyValuePair<Gesture, DiscreteGestureResult> result, ulong id)
        {
            if (!_GestureFromEffectAttributes.ContainsKey(result.Key.Name))
                return;

            EffectAttributes ea = _GestureFromEffectAttributes[result.Key.Name];
            if (result.Value.Confidence < ea.Threshold)
                return;

            Debug.Log(result.Key.Name + "Confidence : " + result.Value.Confidence);

            Vector3 pos =
                        _Joints[id][ea.AttachPosition].transform.position;

            EffekseerSystem.PlayEffect(ea.EffectName, pos);
        }

        /// <summary>
        /// 両手足にTrailRendererを付ける
        /// </summary>
        /// <param name="body">エフェクトを付けるBody</param>
        private void AddingTrailRendererToBody(GameObject body)
        {
            GameObject[] joints =
            {
                _Joints[ulong.Parse(body.name)][JointType.HandTipRight],
                _Joints[ulong.Parse(body.name)][JointType.HandTipLeft],
                _Joints[ulong.Parse(body.name)][JointType.FootRight],
                _Joints[ulong.Parse(body.name)][JointType.FootLeft]
            };

            TrailRenderer[] joints_tr = new TrailRenderer[joints.Length];

            for (int i = 0; i < joints.Length; i++)
            {
                if (_WSServer != null && _WSServer.IsConnected && !_EffectsCustomize.ContainsKey("Joint_" + joints[i].name))
                {
                    if (joints[i].GetComponent<TrailRenderer>() != null)
                    {
                        Destroy(joints[i].GetComponent<TrailRenderer>());
                    }
                    continue;
                }

                /*
                joints_tr[i] = joints[i].GetComponent<TrailRenderer>();

                if (joints_tr[i] == null)
                {
                    joints_tr[i] = joints[i].AddComponent<TrailRenderer>();
                    joints_tr[i].material = TrailMaterial;
                    joints_tr[i].startWidth = 0.2f;
                    joints_tr[i].endWidth = 0.05f;
                    joints_tr[i].startColor = _RbColor.Rainbow;
                    joints_tr[i].endColor = new Color(1, 1, 1, 0);
                    joints_tr[i].time = 0.5f;
                }
                EffectOption eOption = (_WSServer != null && _WSServer.IsConnected) ? _EffectsCustomize["Joint_" + joints[i].name] : new EffectOption("LINE", Color.black, true);

                joints_tr[i].startColor = (eOption.isRainbow)
                    ? _RbColor.Rainbow
                    : eOption.Color;
                */

                if(!joints[i].transform.Find(Trail.name))
                {
                    var obj = Instantiate(Trail, joints[i].transform);
                    obj.name = "Trail";
                    // obj.GetComponent<TrailRenderer>().startColor = _RbColor.Rainbow;
                    var obj2 = obj.transform.Find("Hand Particle");
                    // obj2.GetComponent<ParticleSystem>().startColor = _RbColor.Rainbow;
                    // obj2.Find("NG Hand Particle").GetComponent<ParticleSystem>().startColor = _RbColor.Rainbow;
                }
                
            }

            // trail prefab instantiate
            /*
            if(!handTipLeft.transform.Find(Trail.name))
            {
                handTipLeft.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                var obj = Instantiate(Trail, handTipLeft.transform);
                obj.name = "Trail";
            }
            */
            
        }
        
    }
}