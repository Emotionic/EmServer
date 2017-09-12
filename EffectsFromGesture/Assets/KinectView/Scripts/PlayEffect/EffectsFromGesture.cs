using UnityEngine;
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
            GameObject handTipLeft = _Joints[ulong.Parse(body.name)][JointType.HandTipRight];
            GameObject handTipRight = _Joints[ulong.Parse(body.name)][JointType.HandTipLeft];

            GameObject thumbLeft = _Joints[ulong.Parse(body.name)][JointType.FootRight];
            GameObject thumbRight = _Joints[ulong.Parse(body.name)][JointType.FootLeft];

            if (handTipLeft.GetComponent<TrailRenderer>() != null)
            {
                handTipLeft.GetComponent<TrailRenderer>().startColor = _RbColor.Rainbow;
                handTipRight.GetComponent<TrailRenderer>().startColor = _RbColor.Rainbow;
                thumbLeft.GetComponent<TrailRenderer>().startColor = Color.red;
                thumbRight.GetComponent<TrailRenderer>().startColor = Color.red;

                return;
            }

            TrailRenderer[] hands_tr =
            {
            handTipLeft.AddComponent<TrailRenderer>(),
            handTipRight.AddComponent<TrailRenderer>(),
            thumbLeft.AddComponent<TrailRenderer>(),
            thumbRight.AddComponent<TrailRenderer>()
            };

            // trail prefab instantiate
            /*
            if(!handTipLeft.transform.Find(Trail.name))
            {
                handTipLeft.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                var obj = Instantiate(Trail, handTipLeft.transform);
                obj.name = "Trail";
            }
            */

            foreach (TrailRenderer hand_tr in hands_tr)
            {
                hand_tr.material = TrailMaterial;
                hand_tr.startWidth = 0.2f;
                hand_tr.endWidth = 0.05f;
                hand_tr.startColor = _RbColor.Rainbow;
                hand_tr.endColor = new Color(1, 1, 1, 0);
                hand_tr.time = 0.5f;
            }
        }
        
    }
}