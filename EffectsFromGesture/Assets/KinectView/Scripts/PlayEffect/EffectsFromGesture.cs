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

        private WSServer _WSServer;

        private Camera _MainCamera;

        /// <summary>
        /// エフェクト名
        /// </summary>
        private readonly string[] _EffectNames = { "StairBroken", "punch", "sonicboom", "linetrail_ver2" };

        // 仮 HSVのH
        private float H = 0f;

        private GestureManager _GestureManager;

        private bool _IsRegMethod = false;

        /// <summary>
        /// Kinect画像と取得した関節情報を表示する
        /// </summary>
        private ColorBodySourceView _ColorBodyView;

        private BodySourceManager _BodyManager;

        private Dictionary<ulong, Dictionary<JointType, GameObject>> _Joints;

        // Use this for initialization
        void Start()
        {
            // loadEffect
            foreach (var efkName in _EffectNames)
                EffekseerSystem.LoadEffect(efkName);

            _MainCamera = GameObject.Find("ConvertCamera").GetComponent<Camera>();

            _WSServer = GameObject.Find("WSServer").GetComponent<WSServer>();

            _WSServer.Like += _WSServer_Like;
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
            _GestureManager = GestureManager.GetComponent<GestureManager>();
            _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
            _ColorBodyView = BodySourceManager.GetComponent<ColorBodySourceView>();
            
            if (_GestureManager == null || _ColorBodyView == null || _BodyManager == null)
                return;

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
            
        }

        private void _GestureManager_GestureDetected(KeyValuePair<Gesture, DiscreteGestureResult> result, ulong id)
        {
            Debug.Log("REC EVNET : " + result.Key.Name + " : " + id);
            switch (result.Key.Name)
            {
                case "Jump02":

                    if (result.Value.Confidence < 0.6)
                        return;

                    Debug.Log("Jump02 Confidence : " + result.Value.Confidence);

                    // Jumpした
                    Vector3 pos =
                        _Joints[id][JointType.SpineMid].transform.position;
                    
                    EffekseerSystem.PlayEffect(_EffectNames[0], pos);

                    break;
                case "OpenMenu":

                    if (result.Value.Confidence < 0.5)
                        return;

                    _MainCamera.backgroundColor = ((_MainCamera.backgroundColor == Color.black) ? Color.gray : Color.black);

                    Debug.Log("OpenMenu Confidence : " + result.Value.Confidence);
                    break;

                case "Punch_Left":
                    Debug.Log("Punch Left" + result.Value.Confidence);

                    if (result.Value.Confidence < 0.2)
                        return;

                    EffekseerSystem.PlayEffect(_EffectNames[1], _Joints[id][JointType.HandRight].transform.position);
                    break;

                case "Punch_Right":
                    Debug.Log("Punch Right" + result.Value.Confidence);

                    if (result.Value.Confidence < 0.2)
                        return;
                    
                    EffekseerSystem.PlayEffect(_EffectNames[1], _Joints[id][JointType.HandLeft].transform.position);
                    break;
            }
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
                handTipLeft.GetComponent<TrailRenderer>().startColor = Color.HSVToRGB(H, 255, 255);
                handTipRight.GetComponent<TrailRenderer>().startColor = Color.red;
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

            foreach (TrailRenderer hand_tr in hands_tr)
            {
                hand_tr.material = TrailMaterial;
                hand_tr.startWidth = 0.2f;
                hand_tr.endWidth = 0.05f;
                hand_tr.startColor = Color.HSVToRGB(H, 255, 255);
                hand_tr.endColor = new Color(255, 255, 255, 0);
                hand_tr.time = 0.5f;
            }
        }
        
    }
}