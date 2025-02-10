using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace WataOfuton.Tools.MMDSetup
{
    public class MMDSetup : MonoBehaviour, IEditorOnly
    {
        [SerializeField] public Transform faceMesh;
        [SerializeField] public List<Transform> bodyMeshes;
        [SerializeField] public bool enableGenerateBS;
        [SerializeField] public int[] blendShapeIndices1;
        [SerializeField] public float[] blendShapePowers1;
        [SerializeField] public bool[] enableBlendBS;
        [SerializeField] public int[] blendShapeIndices2;
        [SerializeField] public float[] blendShapePowers2;
        [SerializeField] public bool[] enableOverrideBS;
    }
}
