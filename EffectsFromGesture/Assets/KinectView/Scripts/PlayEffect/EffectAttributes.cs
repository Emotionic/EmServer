using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Kinect;

class EffectAttributes
{
    public double Threshold { get; private set; }
    public JointType AttachPosition { get; private set; }
    public string EffectName { get; private set; }

    public EffectAttributes(double threshold, JointType jt, string name)
    {
        Threshold = threshold;
        AttachPosition = jt;
        EffectName = name;
    }
}