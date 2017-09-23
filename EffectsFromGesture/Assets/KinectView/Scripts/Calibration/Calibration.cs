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
        Cv2.ImShow("before temp", temp);
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
        bin = new Mat(image.Size(), MatType.CV_32FC1);
        Cv2.CvtColor(image, bin, ColorConversionCodes.BGR2GRAY);
        
        result = new Mat(bin.Rows - temp.Rows + 1, bin.Cols- temp.Cols + 1, MatType.CV_32FC1);

        Cv2.MatchTemplate(bin, temp, result, TemplateMatchModes.CCoeffNormed);

        float threshold = 0.8f;
        Point maxPt, minPt;
        Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Binary);

        Cv2.MinMaxLoc(result, out minPt, out maxPt);

        image.Rectangle(maxPt, new Point(maxPt.X + temp.Width, maxPt.Y + temp.Height), Scalar.Red);
        
        Cv2.ImShow("result image", image);
    }
}

