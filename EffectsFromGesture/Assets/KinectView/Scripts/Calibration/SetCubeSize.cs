using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCubeSize : MonoBehaviour
{
    
	// Use this for initialization
	void Start ()
    {
        this.transform.localScale = new Vector3(Screen.width, Screen.height, 1);

        gameObject.GetComponent<Renderer>().material.mainTexture = Resources.Load("emotionic_e_marker") as Texture2D;
    }
}
