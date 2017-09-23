using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Util;

class Calibration : MonoBehaviour
{
    public GameObject ColorManager;

    private ColorSourceManagerForOpenCV _ColorManager;

    List<Point> detectedPos = new List<Point>();
    Mat temp, image, result, bin;

    private void Start()
    {
        _ColorManager = ColorManager.GetComponent<ColorSourceManagerForOpenCV>();

        temp = new Mat(@"Assets\Resources\Emotionic_e_marker.png", ImreadModes.GrayScale);
        Cv2.Resize(temp, temp, new Size(100, 100));

        Cv2.ImShow("temp", temp);
    }

    private void Update()
    {
        if (_ColorManager.ColorImage.Data == null)
        {
            Debug.Log("kinect is not connected");
            return;
        }

        image = _ColorManager.ColorImage;
        Cv2.Flip(image, image, FlipMode.Y);
        bin = new Mat(image.Size(), MatType.CV_32FC1);
        Cv2.CvtColor(image, bin, ColorConversionCodes.BGR2GRAY);
        
        result = new Mat(bin.Rows - temp.Rows + 1, bin.Cols- temp.Cols + 1, MatType.CV_32FC1);

        Cv2.MatchTemplate(bin, temp, result, TemplateMatchModes.CCoeffNormed);

        double threshold = 0.8;
        Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);

        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                if (result.At<int>(y, x) > threshold)
                {
                    Debug.Log(new Point(x, y));
                    detectedPos.Add(new Point(x, y));
                }
            }
        }
        
        Point endpt;
        foreach (Point pos in detectedPos)
        {
            endpt = new Point(pos.X + temp.Rows, pos.Y + temp.Cols);

            Cv2.Rectangle(image, pos, endpt, Scalar.Red);
        }

        Cv2.ImShow("result image", image);
    }

    private void OnApplicationQuit()
    {
        Cv2.DestroyAllWindows();
    }
}

