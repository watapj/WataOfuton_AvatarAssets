using UnityEngine;

namespace WataOfuton.Tools.MCP_MergeNeck
{
    /// <summary>
    /// 差分用 ScriptableObject. 三角形ごとに uv(uv0,uv1,uv2) と差分(posDelta...)を持つ。
    /// </summary>
    public class TriangleDiffDataAll : ScriptableObject
    {
        public TriangleDiff[] faceTriangles;
        public TriangleDiff[] bodyTriangles;
    }

    /// <summary>
    /// オリジナルメッシュ1三角形分の差分データ。
    /// - uv0,uv1,uv2 : オリジナルメッシュの三角形頂点UV
    /// - posDelta0,1,2: (編集後頂点 - オリジナル頂点) の差分
    /// - normalDelta0,1,2: (編集後頂点 - オリジナル頂点) の法線の差分
    /// - tangentDelta0,1,2: (編集後頂点 - オリジナル頂点) の接線の差分
    /// - boneWeight0,1,2: ボーンウェイト
    /// </summary>
    [System.Serializable]
    public struct TriangleDiff
    {
        public Vector2 uv0, uv1, uv2; // オリジナルメッシュ三角形頂点のUV
        public Vector3 posDelta0, posDelta1, posDelta2; // 位置差分
        public Vector3 normalDelta0, normalDelta1, normalDelta2; // 法線の差分
        public Vector3 tangentDelta0, tangentDelta1, tangentDelta2; // 接線の差分
        public BoneWeight boneWeight0, boneWeight1, boneWeight2; // ボーンウェイト
    }
}