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

    Mat image, texImage, dstImage;

    private Texture2D texture;

    public void ColorExtraction(Mat srcImage, Mat dstImage, ColorConversionCodes code,
        int ch1Lower, int ch1Upper, int ch2Lower, int ch2Upper, int ch3Lower, int ch3Upper)
    {
        if (srcImage == null)
            throw new ArgumentNullException("srcImage");
        else if (dstImage == null)
            throw new ArgumentNullException("dstImage");


        Mat colorImage;
        Mat[] ch3sImage = new Mat[3];
        Mat maskImage;

        int i, k;
        int[] lower = new int[3];
        int[] upper = new int[3];
        int[] val = new int[3];

        Mat lut;

        colorImage = new Mat(srcImage.Size(), srcImage.Depth(), srcImage.Channels());
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

        Cv2.LUT(colorImage, colorImage, lut);
        lut.Release();

        ch3sImage[0] = new Mat(colorImage.Size(), colorImage.Depth(), 1);
        ch3sImage[1] = new Mat(colorImage.Size(), colorImage.Depth(), 1);
        ch3sImage[2] = new Mat(colorImage.Size(), colorImage.Depth(), 1);

        Cv2.Split(colorImage, out ch3sImage);

        maskImage = new Mat(colorImage.Size(), colorImage.Depth(), 1);
        Cv2.BitwiseAnd(ch3sImage[0], ch3sImage[1], maskImage);
        Cv2.BitwiseAnd(maskImage, ch3sImage[2], maskImage);
        
        // Cv2.Zero(dstImage);
        srcImage.CopyTo(dstImage, maskImage);

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
        ColorExtraction(image, dstImage, ColorConversionCodes.BGR2HSV, 90, 110, 200, 255, 200, 255);
        
        Cv2.ImShow("image", image);
        Cv2.ImShow("result", dstImage);

        texImage.Dispose();
        // image.Dispose();
    }

    private void OnApplicationQuit()
    {
        Cv2.DestroyAllWindows();
    }
}

