using UnityEngine;

[System.Serializable]
public class CustomData
{
    public bool DoShare; // 共有するかどうか
    public int JoinType; // 観客参加機能のON/OFF, ビットで表現(0ビット目 : いいね, 1 : 拍手, 2 : Kinect)
    public string[] EnabledLikes; // 使用できるいいね
    public string EffectsCustomize; // エフェクトのカスタマイズデータ(ジェスチャー名, エフェクト名・大きさ・色のデータ)

    [System.NonSerialized]
    private const string _JsonName = "CustomDefaults.json";

    public static CustomData GetDefault()
    {
        return JsonLoader<CustomData>.Load(_JsonName);
    }

}