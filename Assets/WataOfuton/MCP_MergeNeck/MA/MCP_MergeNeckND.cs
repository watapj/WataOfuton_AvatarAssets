#if UNITY_EDITOR
using UnityEngine;
using VRC.SDKBase;

namespace WataOfuton.Tools.MCP_MergeNeck
{
    public class MCP_MergeNeckND : MonoBehaviour, IEditorOnly
    {
        [SerializeField] public SkinnedMeshRenderer _targetFaceRenderer; // 差分を適用する顔メッシュ
        [SerializeField] public SkinnedMeshRenderer _targetBodyRenderer; // 差分を適用する体メッシュ
        [SerializeField] public TriangleDiffDataAll _triangleDiffDataAll; // 差分データ(ScriptableObject)
        [SerializeField] public Vector3 _scale; // アバターのScale

        public void TryApplyDiffDataNDMF(Vector3 scale)
        {
            MCP_MergeNeckCore.ApplyDiffDataNDMF(_targetFaceRenderer, _targetBodyRenderer, _triangleDiffDataAll, scale);
        }
    }
}
#endif