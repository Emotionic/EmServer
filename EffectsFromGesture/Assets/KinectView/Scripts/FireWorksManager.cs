using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FireWorksManager : MonoBehaviour
{
    private const double timeLeft = 7;
    public Color StartColor;

    private void Start()
    {
        var components = transform.GetComponentsInChildren<ParticleSystem>();
        foreach(var component in components)
        {
            component.startColor = StartColor;
        }

    }

    // Update is called once per frame
    void Update ()
    {
        
    }
}
