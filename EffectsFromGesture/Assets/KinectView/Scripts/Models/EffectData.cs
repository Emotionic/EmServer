using UnityEngine;

[System.Serializable]
public class EffectData
{

    public string Name; //エフェクト名
    public Vector3 Position; // エフェクトの位置
    public Quaternion Rotation; // エフェクトの回転
    public Vector3 Scale; // エフェクトの大きさ
    public bool DoLoop; // エフェクトをループ再生するか

}
