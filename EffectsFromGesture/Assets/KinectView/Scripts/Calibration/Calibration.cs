using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;
using System.Linq;

class Calibration : MonoBehaviour
{
    public GameObject ColorManager;

    public RawImage rawImage;
    
    private ColorSourceManagerForOpenCV _ColorManager;

    Mat image, texImage, dstImage;

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

    public void ColorExtraction(Mat srcImage, Mat dstImage, ColorConversionCodes code,
        int ch1Lower, int ch1Upper, int ch2Lower, int ch2Upper, int ch3Lower, int ch3Upper)
    {
        if (srcImage == null)
            throw new ArgumentNullException("srcImage");
        else if (dstImage == null)
            throw new ArgumentNullException("dstImage");

        Debug.Log(srcImage.Type());

        Mat colorImage;
        Mat[] ch3sImage = new Mat[3];
        Mat maskImage;

        int i, k;
        int[] lower = new int[3];
        int[] upper = new int[3];
        int[] val = new int[3];

        Mat lut;

        colorImage = new Mat(srcImage.Size(), MatType.CV_8U);
        // colorImage = new Mat(srcImage.Size(), srcImage.Depth(), srcImage.Channels());
        Cv2.CvtColor(srcImage, colorImage, code);

        lut = new Mat(256, 1, MatType.CV_8UC3);

        lower[0] = ch1Lower;
        lower[1] = ch2Lower;
        lower[2] = ch3Lower;

        upper[0] = ch1Upper;
        upper[1] = ch2Upper;
        upper[2] = ch3Upper;

        for (i = 0; i < 256; i++)
        {
            for (k = 0; k < 3; k++)
            {
                if (lower[k] <= upper[k])
                {
                    if ((lower[k] <= i) && (i <= upper[k]))
                    {
                        val[k] = 255;
                    }
                    else
                    {
                        val[k] = 0;
                    }
                }
                else
                {
                    if ((i <= upper[k]) || (lower[k] <= i))
                    {
                        val[k] = 255;
                    }
                    else
                    {
                        val[k] = 0;
                    }
                }
            }
            lut.Set(i, new Scalar(val[0], val[1], val[2]));
        }

        Debug.Log(colorImage.Type());
        Debug.Log(lut.Type());

        Cv2.LUT(colorImage, lut, colorImage);
        
        lut.Release();

        ch3sImage[0] = new Mat(colorImage.Size(), colorImage.Depth(), 1);
        ch3sImage[1] = new Mat(colorImage.Size(), colorImage.Depth(), 1);
        ch3sImage[2] = new Mat(colorImage.Size(), colorImage.Depth(), 1);

        Cv2.Split(colorImage, out ch3sImage);

        maskImage = new Mat(colorImage.Size(), colorImage.Depth(), 1);
        Cv2.BitwiseAnd(ch3sImage[0], ch3sImage[1], maskImage);
        Cv2.BitwiseAnd(maskImage, ch3sImage[2], maskImage);

        // Cv2.Zero(dstImage);
        maskImage.CopyTo(dstImage);

        colorImage.Release();
        ch3sImage[0].Release();
        ch3sImage[1].Release();
        ch3sImage[2].Release();
        maskImage.Release();
    }

    private void Start()
    {
        _ColorManager = ColorManager.GetComponent<ColorSourceManagerForOpenCV>();

        dstImage = new Mat();
        // Texture2D tex = Resources.Load("emotionic_e_marker") as Texture2D;
    }
    
    private void Update()
    {
        image = _ColorManager.ColorImage;

        if (image == null)
            return;

        texImage = image.CvtColor(ColorConversionCodes.BGR2RGB);

        if (texture == null)
        {
            texture = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false);
            rawImage.texture = texture;
        }

        texture.LoadRawTextureData(texImage.ImEncode(".bmp"));
        texture.Apply();

        // 青色を検出
        var skinMat = ColorExtraction(image, ColorConversionCodes.BGR2HSV, 110, 130, 0, 255, 0, 255);
        // ColorExtraction(image, dstImage, ColorConversionCodes.BGR2HSV, 0, 255, 0, 255, 0, 255);

        ConnectedComponents cc = Cv2.ConnectedComponentsEx(skinMat);

        Cv2.PutText(image, x.ToString(), new Point(10, 180), HersheyFonts.HersheyComplexSmall, 1, new Scalar(255, 0, 255));
        foreach (var blob in cc.Blobs.Skip(1))
        {
            if (blob.Area < 100)
                continue;

            image.Rectangle(blob.Rect, Scalar.Red);
        }

        Cv2.ImShow("image", image);
        Cv2.ImShow("result", skinMat);

        texImage.Dispose();
        // image.Dispose();
        x = (x + 1) % 245;
    }
    int x = 0;

    private void OnApplicationQuit()
    {
        Cv2.DestroyAllWindows();
    }
}

