using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Effekseer;
using Kinect = Windows.Kinect;
using System;
using UTJ.FrameCapturer;
using System.IO;

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

        public GameObject FireWorks_Botan;

        public GameObject FireWorks_Senrin;

        public GameObject LaunchPad;

        public GameObject Cube;

        public delegate void EffectCreateHandler(EffectData data);
        public event EffectCreateHandler EffectCreated;

        public UnityEngine.AudioSource Audio;

        public AudioClip Clip;

        public RecorderBase Recorder;

        public RenderTexture cap;

        public bool PlayNewBodySound;

        private WSServer _WSServer;

        private Camera _MainCamera;

        private CustomData _Customize;

        private bool initCustomized = false;
        
        /// <summary>
        /// エフェクト名
        /// </summary>
        private readonly string[] _EffectNames = { "StairBroken", "punch", "laser" };

        private readonly Dictionary<string, Emotionic.Gesture> _GestureRelation = new Dictionary<string, Emotionic.Gesture>()
        {
            { "Jump", Emotionic.Gesture.Jump },
            { "Punch", Emotionic.Gesture.Punch },
            {"ChimpanzeeClap_Left", Emotionic.Gesture.ChimpanzeeClap  },
            {"ChimpanzeeClap_Right", Emotionic.Gesture.ChimpanzeeClap },
            {"Daisuke", Emotionic.Gesture.Daisuke },
            {"Kamehameha", Emotionic.Gesture.Kamehameha },
        };

        private readonly Dictionary<Emotionic.Effect, string> _EffectRelation = new Dictionary<Emotionic.Effect, string>()
        {
            { Emotionic.Effect.Impact, "StairBroken" }
        };

        private readonly Dictionary<Emotionic.Effect, GameObject> _EffectPrefabs = new Dictionary<Emotionic.Effect, GameObject>()
        {
            { Emotionic.Effect.Beam, Resources.Load<GameObject>("Prefabs/KamehameCharge") },
            { Emotionic.Effect.Ripple, Resources.Load<GameObject>("Prefabs/punch")},
            {Emotionic.Effect.Ripple, Resources.Load<GameObject>("Prefabs/clap_effe") }
        };

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

        private List<float> _timeLeft;

        private int _bodyCount = 0;

        private Color _CameraBackColor = Color.black;

        private EffectAttributes _TestEA;
        
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
                _WSServer.EndPerform += _WSServer_EndPerform;

                _WSServer.OnMainSceneLoaded();
            }

            _GestureManager = GestureManager.GetComponent<GestureManager>();
            _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
            _ColorBodyView = BodySourceManager.GetComponent<ColorBodySourceView>();

            // Add effect attributes
            _GestureFromEffectAttributes = new Dictionary<string, EffectAttributes>();
            _GestureFromEffectAttributes["Jump"] = new EffectAttributes(0.35, JointType.SpineMid, 1, _EffectNames[0]);
            _GestureFromEffectAttributes["Punch"] = new EffectAttributes(0.3, JointType.HandRight, 1, Emotionic.Effect.Ripple);
            _GestureFromEffectAttributes["ChimpanzeeClap_Left"] = new EffectAttributes(0.3, JointType.HandTipLeft, 1, Emotionic.Effect.Clap);
            _GestureFromEffectAttributes["ChimpanzeeClap_Right"] = new EffectAttributes(0.3, JointType.HandTipRight, 1, Emotionic.Effect.Clap);
            _GestureFromEffectAttributes["Daisuke"] = new EffectAttributes(0.4, JointType.Head, 3, _EffectNames[0]);
            _GestureFromEffectAttributes["Kamehameha"] = new EffectAttributes(0.2, JointType.HandLeft, 1, Emotionic.Effect.Beam);
            
            _RbColor = new RainbowColor(0, 0.001f);

            // 開始時間の記録
            _StartedTime = Time.realtimeSinceStartup;

            _timeLeft = new List<float>();
            for (var i = 0; i < 6; i++)
                _timeLeft.Add(0);

            // 音声認識イベントの登録
            GameObject.Find("VoiceManager").GetComponent<VoiceManager>().Recognized += EffectsFromGesture_Recognized;

            var path = new DataPath(DataPath.Root.Current, "Capture/" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            path.CreateDirectory();
            Recorder.outputDir = path;

            Recorder.BeginRecording();

        }

        private void _WSServer_EndPerform()
        {
            // 演技の終了時
            EndPerform();
        }

        private void EndPerform()
        {
            /* TODO : 連番png ffmpegによる出力 */

            if (Recorder.isRecording)
                Recorder.EndRecording();
            SceneManager.LoadScene("FinishScene");
        }

        private void EffectsFromGesture_Recognized(string keyword)
        {
            switch (keyword)
            {
                case "プロコン":
                    Audio.pitch = 0.7f;
                    Audio.PlayOneShot(Clip);

                    var path = Environment.CurrentDirectory + "/Capture/" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

                    Texture2D tex = new Texture2D(cap.width, cap.height, TextureFormat.RGB24, false);
                    RenderTexture.active = cap;
                    tex.ReadPixels(new Rect(0, 0, cap.width, cap.height), 0, 0);
                    tex.Apply();

                    // Encode texture into PNG
                    byte[] bytes = tex.EncodeToPNG();
                    Destroy(tex);

                    //Write to a file in the project folder
                    File.WriteAllBytes(path, bytes);
                    
                    break;

            }

        }

        private void _WSServer_Customize(CustomData data)
        {
            Debug.Log("CUSTOMIZED");
            _Customize = data;

            if (!initCustomized)
            {
                if (_Customize.DoShare)
                {
                    Recorder.BeginRecording();
                }

                initCustomized = true;
            }

        }

        private void _WSServer_Like(LikeData data)
        {
            GameObject fw;
            ParticleSystem ps;
            FireWorksManager fwm;
            switch(data.name)
            {
                case "heart":
                    fw = Instantiate(FireWorks_Botan, LaunchPad.transform);
                    ps = fw.GetComponent<ParticleSystem>();
                    fwm = fw.GetComponent<FireWorksManager>();
                    fwm.StartColor = data.color;
                    ps.Play(true);
                    Destroy(fw.gameObject, 7);
                    break;
                case "star":
                    fw = Instantiate(FireWorks_Senrin, LaunchPad.transform);
                    ps = fw.GetComponent<ParticleSystem>();
                    fwm = fw.GetComponent<FireWorksManager>();
                    fwm.StartColor = data.color;
                    ps.Play(true);
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
                _GestureManager.GestureDetected += _GestureManager_GestureDetected;
                _IsRegMethod = true;
            }

            _Joints = _ColorBodyView.JointsFromBodies;
            
            if (PlayNewBodySound && _Joints.Count > _bodyCount)
            {
                Audio.pitch = 1.0f;
                Audio.PlayOneShot(Clip);
            }
            _bodyCount = _Joints.Count;
            
            foreach (GameObject body in _ColorBodyView.GetBodies())
            {
                AddingTrailRendererToBody(body);
            }

            _RbColor.Update();
            
            Camera.main.backgroundColor = _CameraBackColor;
            if (_CameraBackColor != Color.black)
                _CameraBackColor = Color.black;

            if (IsConnected)
            {
                // 時間制限
                if (_Customize != null && _Customize.TimeLimit != 0)
                {
                    if ((_Customize.TimeLimit * 60) - (Time.realtimeSinceStartup - _StartedTime) <= 0)
                    {
                        // 時間経過 -> シーン遷移
                        _WSServer.OnPerformEndedAuto();
                        EndPerform();
                    }
                }

                // 残像切り替え
                Cube.SetActive(_Customize.IsZNZOVisibled);

                for (var i = 0; i < 6; i++)
                    _timeLeft[i] -= Time.deltaTime;
            }

        }

        private void _GestureManager_GestureDetected(KeyValuePair<Gesture, DiscreteGestureResult> result, ulong id)
        {
            if (!_GestureFromEffectAttributes.ContainsKey(result.Key.Name))
                return;

            EffectAttributes ea = _GestureFromEffectAttributes[result.Key.Name];
            if (result.Value.Confidence < ea.Threshold)
                return;
            
            if (IsConnected)
            {
                var gesture = _GestureRelation[result.Key.Name];
                if (_Customize.EffectsCustomize[gesture].Count == 0)
                {
                    return;
                }

                Transform transform;
                string effectName;

                foreach (var custom in _Customize.EffectsCustomize[gesture])
                {
                    foreach (var parts in custom.Value.AttachedParts)
                    {
                        transform = _Joints[id][(JointType)Enum.Parse(typeof(JointType), parts)].transform;

                        if (_EffectRelation.ContainsKey(custom.Key))
                        {
                            var h = EffekseerSystem.PlayEffect(_EffectRelation[custom.Key], transform.position);
                            h.SetRotation(transform.rotation);
                            h.SetScale(GetScaleVec(custom.Value.Scale));
                        }
                        else
                        {
                            var effe = Instantiate(_EffectPrefabs[custom.Key], transform);
                            effe.transform.position = transform.position;
                            effe.transform.rotation = transform.rotation;
                            effe.GetComponent<ParticleSystem>().Play(true);
                            Destroy(effe.gameObject, 10);
                        }

                        StartCoroutine(SendEffect(
                            custom.Key.ToString(),
                            _Joints[id][ea.AttachPosition].transform.position,
                            FloatListToColor(custom.Value.Color),
                            GetScaleVec(custom.Value.Scale),
                            transform.rotation
                        ));

                    }
                }
            }
            else
            {
                switch (ea.Type)
                {
                    case EffectAttributes.EffectType.Effekseer:
                        var h = EffekseerSystem.PlayEffect(ea.EffectName, _Joints[id][ea.AttachPosition].transform.position);
                        h.SetScale(ea.Scale);
                        h.SetRotation(_Joints[id][ea.AttachPosition].transform.rotation);
                        break;

                    case EffectAttributes.EffectType.ParticleSystem:
                        var effe = Instantiate(_EffectPrefabs[ea.EffectKey], _Joints[id][ea.AttachPosition].transform);
                        effe.transform.position = _Joints[id][ea.AttachPosition].transform.position;
                        effe.transform.rotation = _Joints[id][ea.AttachPosition].transform.rotation;
                        effe.GetComponent<ParticleSystem>().Play(true);
                        Destroy(effe.gameObject, 10);
                        break;
                }
            }

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
                _Joints[ulong.Parse(body.name)][JointType.FootLeft],
                _Joints[ulong.Parse(body.name)][JointType.Head],
                _Joints[ulong.Parse(body.name)][JointType.SpineBase]
            };
            
            for (int i = 0; i < joints.Length; i++)
            {
                EffectOption eOption;
                if (IsConnected)
                {
                    if (!_Customize.EffectsCustomize[Emotionic.Gesture.Always].ContainsKey(Emotionic.Effect.Line)
                        || !_Customize.EffectsCustomize[Emotionic.Gesture.Always][Emotionic.Effect.Line].AttachedParts.Contains(joints[i].name))
                    {
                        if (joints[i].transform.Find(Trail.name) != null)
                        {
                            Destroy(joints[i].transform.Find(Trail.name).gameObject);
                        }
                        continue;
                    }

                    eOption = _Customize.EffectsCustomize[Emotionic.Gesture.Always][Emotionic.Effect.Line];

                }
                else
                {
                    // 切断時は全てON
                    eOption = new EffectOption();
                    eOption.IsRainbow = true;
                    eOption.Scale = 1.0f;
                }

                Transform trail;
                if(!joints[i].transform.Find(Trail.name))
                {
                    Instantiate(Trail, joints[i].transform).name = "Trail";
                }

                trail = joints[i].transform.Find(Trail.name);

                TrailRenderer tr = trail.GetComponent<TrailRenderer>();
                tr.widthMultiplier = 0.21f * eOption.Scale;

                ParticleSystem[] pss =
                    {
                    trail.Find("Hand Particle").GetComponent<ParticleSystem>(),
                    trail.Find("NG Hand Particle").GetComponent<ParticleSystem>()
                    };

                if (eOption.IsRainbow)
                {
                    tr.startColor = _RbColor.Rainbow;
                    foreach (ParticleSystem ps in pss)
                    {
                        ps.startColor = _RbColor.Rainbow;
                    }
                }
                else
                {
                    tr.startColor = FloatListToColor(eOption.Color);
                    foreach (ParticleSystem ps in pss)
                    {
                        ps.startColor = FloatListToColor(eOption.Color);
                    }
                }

                // データをEmClientに送信
                if (_timeLeft[i] <= 0)
                {
                    _timeLeft[i] = (1.0f / 5);

                    StartCoroutine(SendEffect(
                        "LINE_" + joints[i].name,
                        joints[i].transform.position,
                        eOption.IsRainbow ? _RbColor.Rainbow : FloatListToColor(eOption.Color),
                        GetScaleVec(eOption.Scale),
                        Quaternion.identity
                    ));

                }

            }

        }

        private Color FloatListToColor(List<float> _list)
        {
            if (_list == null)
                return Color.black;

            var col = new Color();
            col.r = _list[0];
            col.g = _list[1];
            col.b = _list[2];
            col.a = _list[3];
            return col;
        }

        private IEnumerator SendEffect(string name, Vector3 pos, Color color, Vector3 scale, Quaternion rotation)
        {
            if (!IsConnected || _Customize.JoinType.ToString()[0] == '0')
                yield break;

            var data = new EffectData();
            data.Name = name;
            data.Position = Camera.main.WorldToViewportPoint(pos);
            data.Color = color;
            data.Scale = scale;
            data.Rotation = rotation;

            EffectCreated(data);
        }

        private Vector3 GetScaleVec(float scale)
        {
            return new Vector3(scale, scale, scale);
        }

        private bool IsConnected
        {
            get { return _WSServer != null && _WSServer.IsConnected; }
        }
        
    }
}