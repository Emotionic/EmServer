using OpenCvSharp;
using Windows.Kinect;
using UnityEngine;

public class ColorSourceManagerForOpenCV : MonoBehaviour
{
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }

    public Mat ColorImage { get; private set; }

    public bool isInit { get; private set; }
    
    private KinectSensor _Sensor;
    private ColorFrameReader _Reader;

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.ColorFrameSource.OpenReader();

            var frameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = frameDesc.Width;
            ColorHeight = frameDesc.Height;

            ColorImage = new Mat(ColorHeight, ColorWidth, MatType.CV_8UC4);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }

    void Update()
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();

            if (frame != null)
            {
                frame.CopyConvertedFrameDataToIntPtr
                    (
                    ColorImage.Data,
                    (uint)(ColorImage.Total() * ColorImage.ElemSize()),
                    ColorImageFormat.Bgra
                    );
                Cv2.Flip(ColorImage, ColorImage, FlipMode.Y);

                frame.Dispose();
                frame = null;

                isInit = true;
            }
        }
    }

    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}
