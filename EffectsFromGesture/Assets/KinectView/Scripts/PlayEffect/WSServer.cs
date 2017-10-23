using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using Assets.KinectView.Scripts;

public class WSServer : MonoBehaviour
{
    public delegate void CustomizeHandler(CustomData data);
    public event CustomizeHandler Customize;
    
    public delegate void LikeHandler(LikeData data);
    public event LikeHandler Like;

    public delegate void EndPerformHandler();
    public event EndPerformHandler EndPerform;

    public bool IsConnected
    {
        get
        {
            return ws != null && ws.IsAlive;
        }
    }

    private WebSocket ws = null;
    private Queue msgQueue;
    private byte[] QRData;
    private string Address = "localhost";
    private string outerAddress; // 外から見たアドレス(IP)
    private bool initCustomized = false;
    private float waitBeforePerform = 10;
    private CustomData customData;

    private Canvas _Canvas;

    private const string EnabledJoinType = "1101"; // 許可された観客参加機能 (AR, Kinect, 拍手, いいねの順)

    public void OnMainSceneLoaded()
    {
        GameObject.Find("EffectEmitter").GetComponent<EffectsFromGesture>().EffectCreated += WSServer_EffectCreated;
        Customize(customData);
    }

    public void Connect()
    {
        var res = RequestHTTP(Method.GET, "emserver");
        if (res != "ok") throw new Exception("Cannot connect EmServerWS");

        ws = new WebSocket("ws://" + Address + "/ws");

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket Open");
        };

        ws.OnMessage += (sender, e) =>
        {
            if (e.IsBinary)
            {
                // QR Data
                Debug.Log("QRCode data is coming.");
                QRData = e.RawData;
                return;
            }

            Debug.Log("Data: " + e.Data);
            msgQueue.Enqueue(e.Data);
        };

        ws.OnError += (sender, e) =>
        {
            Debug.Log("WebSocket Error Message: " + e.Message);
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket Close");
        };

        ws.Connect();

    }

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        msgQueue = Queue.Synchronized(new Queue());
    }

    private void Start()
    {
        _Canvas = GameObject.Find("Canvas").GetComponent<Canvas>();

        SceneManager.activeSceneChanged += (prev, next) =>
        {
            _Canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
            _Canvas.transform.Find("LabelIP").GetComponent<Text>().text = outerAddress;

            var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            texture.LoadRawTextureData(QRData);
            texture.Apply();

            _Canvas.transform.Find("QR").GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.zero);
            Canvas.ForceUpdateCanvases();
        };

        Connect();
        
    }

    private void Update()
    {
        // 開始カウントダウン -> 遷移
        if (initCustomized && SceneManager.GetActiveScene().name == "WaitPerformer")
        {
            waitBeforePerform -= Time.deltaTime;
            _Canvas.transform.Find("LabelIP").GetComponent<Text>().text = waitBeforePerform <= 0 ? "Loading..." : ((int)waitBeforePerform).ToString();
            if (waitBeforePerform <= 0)
            {
                SceneManager.LoadScene("MainScene");
            }
        }

        if (!IsConnected)
            return;

        lock (msgQueue.SyncRoot)
        {
            try
            {
                foreach (var _msg in msgQueue)
                {
                    var msg = ((string)_msg).Split();
                    var snd = "";

                    switch (msg[1])
                    {
                        /* IP */
                        case "IP":
                            outerAddress = msg[2];
                            _Canvas.transform.Find("LabelIP").GetComponent<Text>().text = msg[2];
                            break;

                        /* パフォーマー */
                        case "CALIB":
                            // キャリブレーションの開始
                            snd = "PERFORMER\n";
                            snd += "CALIB_OK\n";
                            snd += EnabledJoinType;
                            ws.Send(snd);

                            break;

                        case "CUSTOMIZE":
                            // カスタマイズ
                            customData = JsonConvert.DeserializeObject<CustomData>(msg[2]);
                            Debug.Log("CUSTOMDATA");
                            ReplyAR();
                            if (!initCustomized)
                            {
                                initCustomized = true;
                                if (customData.JoinType.ToString()[1] == '1')
                                    SendKinectJoin("INIT");
                            } else
                            {
                                Customize(customData);
                            }

                            break;

                        /* AR */
                        case "AR":
                            // AR準備完了
                            if (!initCustomized)
                                break;

                            ReplyAR();

                            break;

                        case "LIKE":
                            // いいね！
                            var data = JsonUtility.FromJson<LikeData>(msg[2]);
                            Like(data);

                            break;

                        /* Kinectによる観客参加 */
                        case "KINECTJOIN":
                            // ジェスチャー受信
                            Debug.Log("KINECTJOIN : " + msg[2]);

                            if (msg[2] == "INIT" && initCustomized && customData.JoinType.ToString()[1] == '1')
                            {
                                SendKinectJoin("INIT");
                                break;
                            }

                            // 対応するエフェクトの表示
                            switch (msg[2])
                            {
                                case "Jump": break;
                                case "Punch": break;
                                case "ChimpanzeeClap_Left": break;
                                case "ChimpanzeeClap_Right": break;
                                case "Daisuke": break;
                                case "Kamehameha":break;
                                default: break;
                            }

                            break;

                        /* 再起動 */
                        case "RESTART":
                            SendKinectJoin("RESTART");
                            initCustomized = false;
                            SceneManager.LoadScene("WaitPerformer");
                            break;

                        /* 演技の終了 */
                        case "ENDPERFORM":
                            SendKinectJoin("ENDPERFORM");
                            EndPerform();
                            break;

                        default:
                            break;
                    }

                }
            }
            finally
            {
                msgQueue.Clear();
            }
        }

    }

    public void OnPerformEndedAuto()
    {
        SendKinectJoin("ENDPERFORM");

        var snd = "SERV\n";
        snd += "ENDPERFORM\n";
        snd += (customData.DoShare ? "DOSHARE" : "") + "\n";

        ws.Send(snd);
    }

    private void WSServer_EffectCreated(EffectData data)
    {
        var snd = "CLIENT\n";
        snd += "GENEFF\n";
        snd += JsonUtility.ToJson(data) + "\n";

        ws.SendAsync(snd, b => { });
    }

    private void ReplyAR()
    {
        var ardata = new ARData();
        ardata.EnabledEffects = customData.EnabledLikes;
        ardata.isLikeEnabled = customData.JoinType % 10 == 1;

        var snd = "CLIENT\n";
        snd += "AR_OK\n";
        snd += JsonUtility.ToJson(ardata) + "\n";

        ws.Send(snd);
    }

    private void SendKinectJoin(string action)
    {
        var snd = "KINECTJOIN";
        snd += action + "\n";

        ws.Send(snd);
    }

    private string RequestHTTP(Method method, string action, string value = null)
    {
        string url = "http://" + Address + "/" + action;

        try
        {
            var wc = new System.Net.WebClient();
            string resText = "";

            switch (method)
            {
                case Method.GET:
                    {
                        byte[] resData = wc.DownloadData(url);
                        resText = System.Text.Encoding.UTF8.GetString(resData);
                    }
                    break;

                case Method.POST:
                    {
                        if (value == null)
                            throw new ArgumentException();

                        var ps = new System.Collections.Specialized.NameValueCollection();
                        ps.Add("value", value);
                        byte[] resData = wc.UploadValues(url, ps);
                        resText = System.Text.Encoding.UTF8.GetString(resData);
                    }
                    break;

                default:
                    throw new ArgumentException();

            }

            wc.Dispose();

            return resText;
        }
        catch (Exception)
        {
            return null;
        }
    }

}

public enum Method
{
    GET,
    POST
}
