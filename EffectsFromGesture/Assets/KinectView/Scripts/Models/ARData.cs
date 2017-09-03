using UnityEngine;

[System.Serializable]
public class ARData
{

    public Vector3[] MarkerPos; // マーカーの位置
    public Vector3 MarkerScale; // マーカーの大きさの比率
    public string[] EnabledEffects; // いいねが許可されたエフェクト
	
}
