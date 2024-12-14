using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace WataOfuton.Tools.MMDSetup
{
    public class MMDSetup : MonoBehaviour, IEditorOnly
    {
        [SerializeField] public Transform faceMesh;
        [SerializeField] public List<Transform> bodyMeshes;
#if UNITY_2022_3_OR_NEWER
        [SerializeField] public bool enableGenerateBS;
        [SerializeField] public int[] blendShapeIndices1;
        [SerializeField] public float[] blendShapePowers1;
        [SerializeField] public bool[] enableBlendBS;
        [SerializeField] public int[] blendShapeIndices2;
        [SerializeField] public float[] blendShapePowers2;
        [SerializeField] public bool[] enableOverrideBS;
#endif
    }
}
