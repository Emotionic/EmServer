using UnityEngine;
using System.Collections;

public class SetCameraSize : MonoBehaviour
{
    public Camera MainCamera;

    // Update is called once per frame
    void Update ()
    {
        if(Calibration.RectSize.Height > 0)
            MainCamera.orthographicSize = (Calibration.RectSize.Height / 2f) / 100f;
	}
}
