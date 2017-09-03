using UnityEngine;

[System.Serializable]
public class CustomData
{
    public bool DoShare; // 共有するかどうか
    public int JoinType; // 観客参加機能のON/OFF, ビットで表現(0ビット目 : いいね, 1 : 拍手, 2 : Kinect)


}
