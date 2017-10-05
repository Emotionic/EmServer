using UnityEngine;
using System.Collections;

public class SetCameraSize : MonoBehaviour
{
    // Update is called once per frame
    void Update ()
    {
        if(Calibration.RectSize.Height > 0)
            Camera.main.orthographicSize = (Calibration.RectSize.Height / 2f) / 100f;
	}
}
