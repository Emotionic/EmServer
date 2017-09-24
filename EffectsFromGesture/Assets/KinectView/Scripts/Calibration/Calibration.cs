using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Util;

class Calibration : MonoBehaviour
{
    /// <summary>
    /// 画像Managerのインスタンス取得用
    /// </summary>
    public GameObject ColorManager;

    /// <summary>
    /// Kinectの画像をMat型に変換したオブジェクト取得用
    /// </summary>
    private ColorSourceManagerForOpenCV _ColorManager;

    /// <summary>
    /// マーカーを検出した場所
    /// </summary>
    private List<Point> detectedPos = new List<Point>();

    /// <summary>
    /// パターンマッチング用
    /// </summary>
    private Mat rawTemp, temp, image, result, bin;
    
    private int count = 0;
    private double threshold = 0.4;

    private void Gamma(InputArray src, OutputArray dst, double gamma)
    {
        byte[] lut = new byte[256];

        for (int i = 0; i < 256; i++)
        {
            lut[i] = (byte)(Math.Pow(i / 255.0, 1.0 / gamma) * 255.0);
        }

        Mat lutMat = new Mat(1, 256, MatType.CV_8UC1, lut);

        Cv2.LUT(src, lutMat, dst);
    }


    private void Start()
    {
        // インスタンスを取得
        _ColorManager = ColorManager.GetComponent<ColorSourceManagerForOpenCV>();

        // 検出するマーカー読み込み
        rawTemp = new Mat(@"Assets\Resources\Emotionic_e_marker.png", ImreadModes.GrayScale);
    }

    private void Update()
    {
        // managerからKinect画像を取得
        image = _ColorManager.ColorImage;

        // 左右反転していたので元に戻す
        Cv2.Flip(image, image, FlipMode.Y);

        Gamma(image, image, 1.2);
        
        // Kinect画像を白黒に変換
        bin = new Mat(image.Size(), MatType.CV_32FC1);
        Cv2.CvtColor(image, bin, ColorConversionCodes.BGR2GRAY);

        // マーカーを拡大縮小
        temp = new Mat();
        Cv2.Resize(rawTemp, temp, new Size(50 + count * 3, 50 + count * 3));

        // 検出結果用Matの準備
        result = new Mat(bin.Rows - temp.Rows + 1, bin.Cols - temp.Cols + 1, MatType.CV_32FC1);

        // パターンマッチング
        Cv2.MatchTemplate(bin, temp, result, TemplateMatchModes.CCoeffNormed);

        // 閾値以下を捨てる
        Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);

        // 検出した座標をListに追加
        // 検出したらその範囲にはもうないので飛ばす
        bool isAdded;
        for (int y = 0; y < result.Height; y++)
        {
            isAdded = false;
            for (int x = 0; x < result.Width; x++)
            {
                if (result.At<int>(y, x) > 0)
                {
                    Debug.Log(new Point(x, y));
                    detectedPos.Add(new Point(x, y));
                    x += temp.Width - 1;
                    isAdded = true;
                }
            }

            if (isAdded)
                y += temp.Height - 1;
        }

        // 検出結果を赤い四角で描写
        Point endpt;
        foreach (Point pos in detectedPos)
        {
            endpt = new Point(pos.X + temp.Rows, pos.Y + temp.Cols);

            Cv2.Rectangle(image, pos, endpt, Scalar.Red);
        }
        // Cv2.Resize(image, image, new Size(640, 480));

        // マーカーと検出結果を表示
        Cv2.ImShow("temp", temp);
        Cv2.ImShow("result", image);

        // 検出座標初期化
        detectedPos.Clear();

        // マーカーの大きさを変えて再度検出
        count = (count + 1) % 20;

        Debug.Log("----" + count + "----");
    }

    private void OnApplicationQuit()
    {
        Cv2.DestroyAllWindows();
    }
    
}

