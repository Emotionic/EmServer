using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;
using UnityEngine.SceneManagement;

public class WSServer : MonoBehaviour
{
    public CustomData customData;
    
    public delegate void LikeHandler(LikeData data);
    public event LikeHandler Like;

    private WebSocket ws = null;
    private Queue msgQueue;
    private byte[] QRData;
    private string IP;
    private bool initCustomized = false;
    private float waitBeforePerform = 10;

    private Canvas _Canvas;

    private void Connect()
    {
        ws = new WebSocket("ws://localhost/ws");

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

        Connect();
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

        if (Input.GetKey(KeyCode.C) && (Application.isEditor || Debug.isDebugBuild))
        {
            SceneManager.LoadScene("MainScene");
        }

        if (ws == null || !ws.IsAlive)
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
        ardata.EnabledEffects = customData.EnabledEffects;

        snd = "CLIENT\n";
        snd += "AR_OK\n";
        snd += JsonUtility.ToJson(ardata) + "\n";

        ws.Send(snd);
    }

}
