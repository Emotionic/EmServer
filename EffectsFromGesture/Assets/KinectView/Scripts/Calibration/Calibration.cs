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
    Mat rawTemp, temp, image, result, bin;

    private void Start()
    {
        _ColorManager = ColorManager.GetComponent<ColorSourceManagerForOpenCV>();

        rawTemp = new Mat(@"Assets\Resources\Emotionic_e_marker.png", ImreadModes.GrayScale);
    }

    private int count = 0;

    private void Update()
    {
        image = _ColorManager.ColorImage;
        Cv2.Flip(image, image, FlipMode.Y);
        // Cv2.Resize(image, image, new Size(image.Size().Width / 3, image.Size().Height / 3));
        bin = new Mat(image.Size(), MatType.CV_32FC1);
        Cv2.CvtColor(image, bin, ColorConversionCodes.BGR2GRAY);

        temp = new Mat();
        Cv2.Resize(rawTemp, temp, new Size(100 + count * 3, 100 + count * 3));

        result = new Mat(bin.Rows - temp.Rows + 1, bin.Cols - temp.Cols + 1, MatType.CV_32FC1);

        Cv2.MatchTemplate(bin, temp, result, TemplateMatchModes.CCoeffNormed);

        double threshold = 0.5;
        Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);

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

        Point endpt;
        foreach (Point pos in detectedPos)
        {
            endpt = new Point(pos.X + temp.Rows, pos.Y + temp.Cols);

            Cv2.Rectangle(image, pos, endpt, Scalar.Red);
        }

        Cv2.ImShow("temp", temp);
        Cv2.ImShow("result", image);
        detectedPos.Clear();

        count = (count + 1) % 10;

        Debug.Log("----" + count + "----");
    }

    private void OnApplicationQuit()
    {
        Cv2.DestroyAllWindows();
    }
    
}

