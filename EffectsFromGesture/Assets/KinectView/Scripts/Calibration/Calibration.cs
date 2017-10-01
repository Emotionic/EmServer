using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpenCvSharp;
using System.Linq;

class Calibration : MonoBehaviour
{
    public static Vector3 CameraPosition;

    public GameObject ColorManager;
    
    private ColorSourceManagerForOpenCV _ColorManager;

    Mat image;
    
    private Mat ColorExtraction(InputArray src, ColorConversionCodes code,
        int ch1Lower, int ch1Upper, int ch2Lower, int ch2Upper, int ch3Lower, int ch3Upper)
    {
        if (src == null)
            throw new ArgumentException("src");

        Mat colorMat = new Mat(src.Size(), MatType.CV_8UC3);
        Cv2.CvtColor(src, colorMat, code);

        Mat lut = new Mat(256, 1, MatType.CV_8UC3);

        int[] lower = { ch1Lower, ch2Lower, ch3Lower };
        int[] upper = { ch1Upper, ch2Upper, ch3Upper };

        var mat3 = new MatOfByte3(lut);

        var indexer = mat3.GetIndexer();

        for (int i = 0; i < 256; i++)
        {
            Vec3b color = indexer[i];
            byte temp;

            for (int k = 0; k < 3; k++)
            {
                if (lower[k] <= upper[k])
                {
                    temp = (byte)((lower[k] <= i && i <= upper[k]) ? 255 : 0);
                }
                else
                {
                    temp = (byte)((i <= upper[k] || lower[k] <= i) ? 255 : 0);
                }

                color[k] = temp;
            }

            indexer[i] = color;
        }

        Cv2.LUT(colorMat, lut, colorMat);

        var channelMat = colorMat.Split();

        var maskMat = new Mat();

        Cv2.BitwiseAnd(channelMat[0], channelMat[1], maskMat);
        Cv2.BitwiseAnd(maskMat, channelMat[2], maskMat);

        return maskMat;
    }
    
    private void Start()
    {
        _ColorManager = ColorManager.GetComponent<ColorSourceManagerForOpenCV>();
        
        image = new Mat();
    }
    
    private void Update()
    {
        image = _ColorManager.ColorImage;
        
        if (image == null)
            return;

        // 青色を検出
        // var skinMat = ColorExtraction(image, ColorConversionCodes.BGR2HSV, 90, 120, 0, 255, 200, 255);
         var skinMat = ColorExtraction(image, ColorConversionCodes.BGR2HSV, 90, 120, 0, 255, 220, 255);

        ConnectedComponents cc = Cv2.ConnectedComponentsEx(skinMat);

        if (cc.LabelCount <= 1)
            return;
        
        var largestBlob = cc.GetLargestBlob();

        // シーン遷移
        if (count < 0)
        {
            SceneManager.LoadScene("WaitPerformer");
        }
        
        // カメラの座標を合わせてキャリブレーション
        Point2d pos = new Point2d(largestBlob.Centroid.X - (1920 - Screen.width) / 2, largestBlob.Centroid.Y - (1080 - Screen.height) / 2);
        
        int X = largestBlob.Height;
        CameraPosition = new Vector3(
            (float)pos.X - Screen.width / 2,
            Screen.height / 2 - (float)pos.Y,
            -X);

        CameraPosition /= 100f;
        
        count--;
    }

    private int count = 25;
}

