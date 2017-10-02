using UnityEngine;
using UnityEngine.SceneManagement;
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

        public GameObject Spacium;

        public GameObject FireWorks;
        
        public GameObject Cube;

        public GameObject LaunchPad;

        private WSServer _WSServer;

        private Camera _MainCamera;

        private CustomData _Customize;

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

        private float _StartedTime;

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

            _RbColor = new RainbowColor(0, 0.001f);

            // 開始時間の記録
            _StartedTime = Time.realtimeSinceStartup;

        }

        private void _WSServer_Customize(CustomData data)
        {
            Debug.Log("CUSTOMIZED");
            _Customize = data;
        }

        private void _WSServer_Like(LikeData data)
        {
            GameObject fw;
            ParticleSystem ps;
            switch(data.name)
            {
                case "heart":
                    fw = Instantiate(FireWorks, LaunchPad.transform);
                    ps = fw.GetComponent<ParticleSystem>();
                    ps.startLifetime = Calibration.RectSize.Height / 5f / 100f;
                    ps.startColor = data.color;
                    Destroy(fw.gameObject, 7);
                    break;
                case "star":
                    fw = Instantiate(FireWorks, LaunchPad.transform);
                    ps = fw.GetComponent<ParticleSystem>();
                    ps.startLifetime = Calibration.RectSize.Height / 5f / 100f;
                    ps.startColor = data.color;
                    Destroy(fw.gameObject, 7);
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

            // 時間制限
            if (_Customize.TimeLimit != 0)
            {
                if ((_Customize.TimeLimit * 60) - (Time.realtimeSinceStartup - _StartedTime) <= 0)
                {
                    // 時間経過 -> シーン遷移
                    SceneManager.LoadScene("FinishScene");
                }
            }

            // 残像切り替え
            Cube.SetActive(_Customize.IsZNZOVisibled);
            
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
            
            for (int i = 0; i < joints.Length; i++)
            {
                //if (_WSServer != null && _WSServer.IsConnected && !_EffectsCustomize.ContainsKey("Joint_" + joints[i].name))
                //{
                //    if (joints[i].transform.Find(Trail.name) != null)
                //    {
                //        Destroy(joints[i].transform.Find(Trail.name));
                //    }
                //    continue;
                //}
                
                // EffectOption eOption = (_WSServer != null && _WSServer.IsConnected) ? _EffectsCustomize["Joint_" + joints[i].name] : new EffectOption("LINE", Color.black, true);

                Transform trail;
                if(!joints[i].transform.Find(Trail.name))
                {
                    Instantiate(Trail, joints[i].transform).name = "Trail";
                    Instantiate(Spacium, joints[i].transform).name = "Spacium";
                }

                trail = joints[i].transform.Find(Trail.name);
                TrailRenderer tr = trail.GetComponent<TrailRenderer>();
                ParticleSystem[] pss =
                    {
                    trail.Find("Hand Particle").GetComponent<ParticleSystem>(),
                    trail.Find("NG Hand Particle").GetComponent<ParticleSystem>()
                    };

                tr.startColor = _RbColor.Rainbow;
                foreach (ParticleSystem ps in pss)
                {
                    ps.startColor = _RbColor.Rainbow;
                }

                //if (eOption.isRainbow)
                //{
                //    tr.startColor = _RbColor.Rainbow;
                //    foreach(ParticleSystem ps in pss)
                //    {
                //        ps.startColor = _RbColor.Rainbow;
                //    }
                //}
                //else
                //{
                //    tr.startColor = eOption.Color;
                //    foreach(ParticleSystem ps in pss)
                //    {
                //        ps.startColor = eOption.Color;
                //    }
                //}

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

        private Color FloatListToColor(List<float> _list)
        {
            var col = new Color();
            col.r = _list[0];
            col.g = _list[1];
            col.b = _list[2];
            col.a = _list[3];
            return col;
        }
        
    }
}