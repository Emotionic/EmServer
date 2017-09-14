using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;
using UnityEngine.SceneManagement;

public class WSServer : MonoBehaviour
{
    public delegate void CustomizeHandler(CustomData data);
    public event CustomizeHandler Customize;
    
    public delegate void LikeHandler(LikeData data);
    public event LikeHandler Like;

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
    private string IP;
    private bool initCustomized = false;
    private float waitBeforePerform = 10;
    private CustomData customData;

    private Canvas _Canvas;

    public void Connect()
    {
        var _ip = _Canvas.transform.Find("InputIP").GetComponent<InputField>().text;
        if (!string.IsNullOrEmpty(_ip))
            IP = _ip;

        var res = RequestHTTP(Method.GET, "emserver");
        if (res != "ok") throw new Exception("Cannot connect EmServerWS");

        ws = new WebSocket("ws://" + IP + "/ws");

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
            _Canvas.transform.Find("LabelIP").GetComponent<Text>().text = IP;

            var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            texture.LoadRawTextureData(QRData);
            texture.Apply();

            _Canvas.transform.Find("QR").GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.zero);
            Canvas.ForceUpdateCanvases();
        };
        
    }

    private void Update()
    {
        // 開始カウントダウン -> 遷移
        if (initCustomized && SceneManager.GetActiveScene().name == "Calibration")
        {
            waitBeforePerform -= Time.deltaTime;
            _Canvas.transform.Find("LabelIP").GetComponent<Text>().text = ((int)waitBeforePerform).ToString();
            if (waitBeforePerform <= 0)
            {
                SceneManager.LoadScene("MainScene");
            }
        }

        if ((Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.C)) && (Application.isEditor || Debug.isDebugBuild))
        {
            SceneManager.LoadScene("MainScene");
        }

        if (!IsConnected && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Return))
        {
            Connect();
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
                            IP = msg[2];
                            _Canvas.transform.Find("LabelIP").GetComponent<Text>().text = IP;
                            break;

                        /* パフォーマー */
                        case "CALIB":
                            // キャリブレーションの開始
                            if (!initCustomized)
                            {
                                customData = CustomData.GetDefault();
                            }

                            snd = "PERFORMER\n";
                            snd += "CALIB_OK\n";
                            snd += JsonUtility.ToJson(customData);

                            ws.Send(snd);

                            break;

                        case "CUSTOMIZE":
                            // カスタマイズ
                            customData = JsonUtility.FromJson<CustomData>(msg[2]);
                            Customize(customData);
                            Debug.Log("CUSTOMDATA");
                            ReplyAR();
                            if (!initCustomized)
                            {
                                initCustomized = true;
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

    private void ReplyAR()
    {
        var snd = "";
        var ardata = new ARData();
        ardata.MarkerPos = new[] { Vector3.zero };
        ardata.MarkerScale = Vector3.zero;
        ardata.EnabledEffects = customData.EnabledLikes;
        ardata.isLikeEnabled = customData.JoinType % 10 == 1;

        snd = "CLIENT\n";
        snd += "AR_OK\n";
        snd += JsonUtility.ToJson(ardata) + "\n";

        ws.Send(snd);
    }

    private string RequestHTTP(Method method, string action, string value = null)
    {
        string url = "http://" + IP + "/" + action;

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
