﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;

public class BodySourceManager : MonoBehaviour 
{
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
    private Body[] _Data = null;
    
    public Body[] GetData()
    {
        return _Data;
    }

    public KinectSensor Sensor
    {
        get { return _Sensor; }
    }

    public Windows.Kinect.Vector4 FloorClipPlane { get; private set; }

    public List<Windows.Kinect.Vector4> GetJointOrientations(JointType jt)
    {
        List<Windows.Kinect.Vector4> orientations = new List<Windows.Kinect.Vector4>();
        
        foreach(var body in GetData())
        {
            orientations.Add(body.JointOrientations[jt].Orientation);
        }

        return orientations;
    }

    void Start () 
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.BodyFrameSource.OpenReader();
            
            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }   
    }
    
    void Update () 
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                if (_Data == null)
                {
                    _Data = new Body[_Sensor.BodyFrameSource.BodyCount];
                }
                
                frame.GetAndRefreshBodyData(_Data);

                FloorClipPlane = frame.FloorClipPlane;
                
                frame.Dispose();
                frame = null;
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
