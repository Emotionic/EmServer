using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;

class Calibration : MonoBehaviour
{
    public GameObject ColorManager;

    public RawImage rawImage;
    
    private ColorSourceManagerForOpenCV _ColorManager;

    Mat image, texImage;

    private Texture2D texture;
    
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

            for(int k = 0;k < 3;k++)
            {
                if(lower[k] <= upper[k])
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

    private Point[] DetectFourCorners(Mat src)
    {
        Point[] fourCorners = new Point[4];
        Point q;
        bool isDetected;
        Point p = new Point(src.Width / 2, src.Height / 2);

        // up left
        isDetected = false;
        q = p;
        for (; q.X >= 0; q.X--)
        {
            for (; q.Y >= 0; q.Y--)
            {
                if (src.At<int>(q.X, q.Y) > 0)
                {
                    fourCorners[0] = q;
                    isDetected = true;
                    break;
                }

            }

            if (isDetected)
                break;
        }

        // up right
        isDetected = false;
        q = p;
        for (; q.X < src.Width; q.X++)
        {
            for (; q.Y >= 0; q.Y--)
            {
                if (src.At<int>(q.Y, q.X) > 0)
                {
                    fourCorners[1] = q;
                    isDetected = true;
                    break;
                }

            }

            if (isDetected)
                break;
        }

        // down left
        isDetected = false;
        q = p;
        for (; q.X >= 0; q.X--)
        {
            for (; q.Y < src.Height; q.Y++)
            {
                if (src.At<int>(q.Y, q.X) > 0)
                {
                    fourCorners[2] = q;
                    isDetected = true;
                    break;
                }

            }
            if (isDetected)
                break;
        }

        // down right
        isDetected = false;
        q = p;
        for (; q.X < src.Width; q.X++)
        {
            for (; q.Y < src.Height; q.Y++)
            {
                if (src.At<int>(q.Y, q.X) > 0)
                {
                    fourCorners[3] = q;
                    isDetected = true;
                    break;
                }

            }

            if (isDetected)
                break;
        }
        
        return fourCorners;
    }
    
    private void Start()
    {
        _ColorManager = ColorManager.GetComponent<ColorSourceManagerForOpenCV>();

        // Texture2D tex = Resources.Load("emotionic_e_marker") as Texture2D;
    }
    
    private void Update()
    {
        image = _ColorManager.ColorImage;
        texImage = image.CvtColor(ColorConversionCodes.BGR2RGB);

        if (texture == null)
        {
            texture = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false);
            rawImage.texture = texture;
        }

        texture.LoadRawTextureData(texImage.ImEncode(".bmp"));
        texture.Apply();

        // 青色を検出
        var skinMat = ColorExtraction(image, ColorConversionCodes.BGR2HSV, 90, 110, 200, 255, 200, 255);
        
        var pos = DetectFourCorners(skinMat);
        foreach (Point x in pos)
        {
            Debug.Log(x);
            image.PutText("koko", x, HersheyFonts.HersheyScriptComplex, 10, Scalar.Red);
        }

        Cv2.ImShow("image", image);
        Cv2.ImShow("result", skinMat);

        texImage.Dispose();
        // image.Dispose();
        // skinMat.Dispose();
    }

    private void OnApplicationQuit()
    {
        Cv2.DestroyAllWindows();
    }
}

