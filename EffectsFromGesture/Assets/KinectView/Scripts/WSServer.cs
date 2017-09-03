using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using UnityEngine.SceneManagement;

public class WSServer : MonoBehaviour
{
    public CustomData customData;

    public delegate void LikeHandler(LikeData data);
    public event LikeHandler Like;

    private WebSocket ws = null;
    private bool waitLaunch  = true;
    private Queue msgQueue;
    private byte[] QRData;

    private void Connect()
    {
        ws = new WebSocket("ws://localhost/ws");

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket Open");
            waitLaunch = false;
        };

        ws.OnMessage += (sender, e) =>
        {
            if (e.IsBinary)
            {
                // QR Data
                Debug.Log("QRCode data is coming.");
                QRData = e.RawData;
                msgQueue.Enqueue("SERV\nQR\n");
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

	private void Awake ()
    {
        DontDestroyOnLoad(this.gameObject);
        msgQueue = Queue.Synchronized(new Queue());
    }
	
    private void Start()
    {
        Connect();

    }

	private void Update ()
    {
        if (Input.GetKey(KeyCode.C) && (Application.isEditor || Debug.isDebugBuild))
        {
            SceneManager.LoadScene("MainScene");
        }

        if (waitLaunch) return;

        lock (msgQueue.SyncRoot)
        {
            foreach (var _msg in msgQueue)
            {
                var msg = ((string)_msg).Split();
                var snd = "";

                switch (msg[1])
                {
                    /* IP ・ QRコード */
                    case "IP":
                        break;

                    case "QR":
                        break;

                    /* パフォーマー */
                    case "CALIB":
                        // キャリブレーションの開始
                        customData = new CustomData();
                        customData.DoShare = false;
                        customData.JoinType = System.Convert.ToInt32("001", 2); // 2進数から変換

                        snd = "PERFORMER\n";
                        snd += "CALIB_OK\n";
                        snd += JsonUtility.ToJson(customData);

                        ws.Send(snd);

                        SceneManager.LoadScene("MainScene");

                        break;

                    case "CUSTOMIZE":
                        // カスタマイズ
                        customData = JsonUtility.FromJson<CustomData>(msg[2]);
                        break;

                    /* AR */
                    case "AR":
                        // AR準備完了
                        var ardata = new ARData();
                        ardata.MarkerPos = new[] { Vector3.zero };
                        ardata.MarkerScale = Vector3.zero;
                        ardata.EnabledEffects = new[] { "heart", "star" };
                        
                        snd = "CLIENT\n";
                        snd += "AR_OK\n";
                        snd += JsonUtility.ToJson(ardata) + "\n";

                        ws.Send(snd);

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

            msgQueue.Clear();

        }

	}


}
