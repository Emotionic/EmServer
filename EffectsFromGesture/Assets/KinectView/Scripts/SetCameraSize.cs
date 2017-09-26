using UnityEngine;
using System.Collections;

public class SetCameraSize : MonoBehaviour
{
    public Camera _MainCamera;
    
    // Update is called once per frame
    void Update ()
    {
        float frustumHeight = Screen.width / 100f / ((float)Screen.width / Screen.height);
        
        float distance = frustumHeight * 0.5f;

        _MainCamera.orthographicSize = distance;
	}
}
