using UnityEngine;

[System.Serializable]
public class EffectOption
{

    public string Name; // エフェクト名
    public Vector3 Scale; // エフェクトの大きさ
    public Color Color; // エフェクトの色
    public bool isRainbow; // 虹色

    public EffectOption(string _Name, Vector3 _Scale, Color _Color, bool _isRainbow)
    {
        Name = _Name;
        Scale = _Scale;
        Color = _Color;
        isRainbow = _isRainbow;
    }

    public EffectOption(string _Name, Color _Color, bool _isRainbow)
    {
        Name = _Name;
        Scale = Vector3.one;
        Color = _Color;
        isRainbow = _isRainbow;
    }

}