using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCameraPosition : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
        Camera.main.transform.position = Calibration.CameraPosition / 100f;
        Debug.Log(Calibration.CameraPosition);
        Debug.Log(Camera.main.transform.position);
    }

    private void Update()
    {
        Debug.Log(Camera.main.transform.position);
    }
}
