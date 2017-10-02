using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetLaunchPadPosition : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        this.transform.position = new Vector3(0, (Calibration.RectSize.Height / 2f) / 100f, -3);
	}
	
}
