using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Kinect;
using UnityEngine;

class EffectAttributes
{
    public double Threshold { get; private set; }
    public JointType AttachPosition { get; private set; }
    public EffectType Type { get; private set; }

    public string EffectName { get; private set; }
    public GameObject Effect { get; private set; }

    public EffectAttributes(double threshold, JointType jt)
    {
        Threshold = threshold;
        AttachPosition = jt;
    }

    public EffectAttributes(double threshold, JointType jt, string name) : this(threshold, jt)
    {
        EffectName = name;
        Type = EffectType.Effekseer;
    }

    public EffectAttributes(double threshold, JointType jt, GameObject effect): this (threshold, jt)
    {
        Effect = effect;
        Type = EffectType.ParticleSystem;
    }

    public enum EffectType
    {
        Effekseer,
        ParticleSystem,
    }
}