#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using UnityEngine.Rendering;

namespace WataOfuton.Tools.RemoveMeshByBlendshape
{
    public class RemoveMeshByBlendshape : MonoBehaviour, IEditorOnly
    {
        [SerializeField] public SkinnedMeshRenderer _target;
        [SerializeField] public List<bool> _isSelected;
        [SerializeField] public float _weightThreshold = 0;


        public void GetSMR()
        {
            _target = this.GetComponent<SkinnedMeshRenderer>();
        }

        public void TryRemoveMeshByBlendshape()
        {
            // Undo.RecordObject(_target, "Remove Mesh");

            Mesh originalMesh = _target.sharedMesh;
            Mesh newMesh = Instantiate(originalMesh);

            Vector3[] originalVertices = newMesh.vertices;
            Vector3[] originalNormals = newMesh.normals;
            Vector4[] originalTangents = newMesh.tangents;
            Color[] originalColors = newMesh.colors;
            Color32[] originalColors32 = newMesh.colors32;
            Vector2[] originalUv = newMesh.uv;
            Vector2[] originalUv2 = newMesh.uv2;
            Vector2[] originalUv3 = newMesh.uv3;
            Vector2[] originalUv4 = newMesh.uv4;
            Vector2[] originalUv5 = newMesh.uv5;
            Vector2[] originalUv6 = newMesh.uv6;
            Vector2[] originalUv7 = newMesh.uv7;
            Vector2[] originalUv8 = newMesh.uv8;
            BoneWeight[] originalBoneWeights = newMesh.boneWeights;

            int vertexCount = newMesh.vertexCount;
            bool[] vertexToRemove = new bool[vertexCount];

            // BlendShapeにより影響を受ける頂点を特定
            for (int i = 0; i < newMesh.blendShapeCount; i++)
            {
                if (i < _isSelected.Count && _isSelected[i] && _target.GetBlendShapeWeight(i) > _weightThreshold)
                {
                    int frameCount = newMesh.GetBlendShapeFrameCount(i);
                    for (int frame = 0; frame < frameCount; frame++)
                    {
                        Vector3[] deltaVertices = new Vector3[vertexCount];
                        Vector3[] deltaNormals = new Vector3[vertexCount];
                        Vector3[] deltaTangents = new Vector3[vertexCount];

                        newMesh.GetBlendShapeFrameVertices(i, frame, deltaVertices, deltaNormals, deltaTangents);

                        for (int v = 0; v < vertexCount; v++)
                        {
                            if (deltaVertices[v].magnitude > 0.001f)
                            {
                                vertexToRemove[v] = true;
                            }
                        }
                    }
                }
            }

            // old -> new 頂点インデックスマッピングを作成
            Dictionary<int, int> oldToNewIndexMap = new Dictionary<int, int>();
            List<Vector3> newVerticesList = new List<Vector3>();
            List<Vector3> newNormalsList = new List<Vector3>();
            List<Vector4> newTangentsList = new List<Vector4>();
            List<Color> newColorsList = (originalColors != null && originalColors.Length == vertexCount) ? new List<Color>() : null;
            List<Color32> newColors32List = (originalColors32 != null && originalColors32.Length == vertexCount) ? new List<Color32>() : null;
            List<Vector2> newUvList = (originalUv != null && originalUv.Length == vertexCount) ? new List<Vector2>() : null;
            List<Vector2> newUv2List = (originalUv2 != null && originalUv2.Length == vertexCount) ? new List<Vector2>() : null;
            List<Vector2> newUv3List = (originalUv3 != null && originalUv3.Length == vertexCount) ? new List<Vector2>() : null;
            List<Vector2> newUv4List = (originalUv4 != null && originalUv4.Length == vertexCount) ? new List<Vector2>() : null;
            List<Vector2> newUv5List = (originalUv5 != null && originalUv5.Length == vertexCount) ? new List<Vector2>() : null;
            List<Vector2> newUv6List = (originalUv6 != null && originalUv6.Length == vertexCount) ? new List<Vector2>() : null;
            List<Vector2> newUv7List = (originalUv7 != null && originalUv7.Length == vertexCount) ? new List<Vector2>() : null;
            List<Vector2> newUv8List = (originalUv8 != null && originalUv8.Length == vertexCount) ? new List<Vector2>() : null;
            List<BoneWeight> newBoneWeightsList = (originalBoneWeights != null && originalBoneWeights.Length == vertexCount) ? new List<BoneWeight>() : null;

            for (int i = 0; i < vertexCount; i++)
            {
                if (!vertexToRemove[i])
                {
                    oldToNewIndexMap[i] = newVerticesList.Count;
                    newVerticesList.Add(originalVertices[i]);
                    if (originalNormals != null && originalNormals.Length == vertexCount) newNormalsList.Add(originalNormals[i]);
                    if (originalTangents != null && originalTangents.Length == vertexCount) newTangentsList.Add(originalTangents[i]);
                    if (newColorsList != null) newColorsList.Add(originalColors[i]);
                    if (newColors32List != null) newColors32List.Add(originalColors32[i]);
                    if (newUvList != null) newUvList.Add(originalUv[i]);
                    if (newUv2List != null) newUv2List.Add(originalUv2[i]);
                    if (newUv3List != null) newUv3List.Add(originalUv3[i]);
                    if (newUv4List != null) newUv4List.Add(originalUv4[i]);
                    if (newUv5List != null) newUv5List.Add(originalUv5[i]);
                    if (newUv6List != null) newUv6List.Add(originalUv6[i]);
                    if (newUv7List != null) newUv7List.Add(originalUv7[i]);
                    if (newUv8List != null) newUv8List.Add(originalUv8[i]);
                    if (newBoneWeightsList != null) newBoneWeightsList.Add(originalBoneWeights[i]);
                }
            }

            // Triangles/SubMesh再構築
            List<int[]> newSubMeshTriangles = new List<int[]>();
            for (int subMeshIndex = 0; subMeshIndex < newMesh.subMeshCount; subMeshIndex++)
            {
                int[] originalTriangles = newMesh.GetTriangles(subMeshIndex);
                List<int> filteredTriangles = new List<int>();
                for (int t = 0; t < originalTriangles.Length; t += 3)
                {
                    int index0 = originalTriangles[t];
                    int index1 = originalTriangles[t + 1];
                    int index2 = originalTriangles[t + 2];

                    if (oldToNewIndexMap.ContainsKey(index0) &&
                        oldToNewIndexMap.ContainsKey(index1) &&
                        oldToNewIndexMap.ContainsKey(index2))
                    {
                        filteredTriangles.Add(oldToNewIndexMap[index0]);
                        filteredTriangles.Add(oldToNewIndexMap[index1]);
                        filteredTriangles.Add(oldToNewIndexMap[index2]);
                    }
                }
                newSubMeshTriangles.Add(filteredTriangles.ToArray());
            }

            newMesh.Clear();

            newMesh.SetVertices(newVerticesList);
            if (newNormalsList.Count > 0) newMesh.SetNormals(newNormalsList);
            if (newTangentsList.Count > 0) newMesh.SetTangents(newTangentsList);
            if (newColorsList != null && newColorsList.Count > 0) newMesh.SetColors(newColorsList);
            if (newColors32List != null && newColors32List.Count > 0) newMesh.SetColors(newColors32List);

            if (newUvList != null) newMesh.SetUVs(0, newUvList);
            if (newUv2List != null) newMesh.SetUVs(1, newUv2List);
            if (newUv3List != null) newMesh.SetUVs(2, newUv3List);
            if (newUv4List != null) newMesh.SetUVs(3, newUv4List);
            if (newUv5List != null) newMesh.SetUVs(4, newUv5List);
            if (newUv6List != null) newMesh.SetUVs(5, newUv6List);
            if (newUv7List != null) newMesh.SetUVs(6, newUv7List);
            if (newUv8List != null) newMesh.SetUVs(7, newUv8List);

            if (newBoneWeightsList != null && newBoneWeightsList.Count > 0) newMesh.boneWeights = newBoneWeightsList.ToArray();
            newMesh.bindposes = originalMesh.bindposes;

            newMesh.subMeshCount = newSubMeshTriangles.Count;
            for (int i = 0; i < newSubMeshTriangles.Count; i++)
            {
                newMesh.SetTriangles(newSubMeshTriangles[i], i);
            }

            // BlendShape再構築
            RebuildBlendShapes(originalMesh, newMesh, oldToNewIndexMap);

            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();

            _target.sharedMesh = newMesh;
            Debug.Log($"New mesh applied: {newVerticesList.Count} vertices, {newSubMeshTriangles.Count} submeshes.");

        }

        private void RebuildBlendShapes(Mesh originalMesh, Mesh newMesh, Dictionary<int, int> oldToNewIndexMap)
        {
            // BlendShape名、フレーム、ウェイトを全て再取得して、新メッシュ用に頂点数再構築
            int blendShapeCount = originalMesh.blendShapeCount;
            for (int shapeIndex = 0; shapeIndex < blendShapeCount; shapeIndex++)
            {
                string shapeName = originalMesh.GetBlendShapeName(shapeIndex);
                int frameCount = originalMesh.GetBlendShapeFrameCount(shapeIndex);
                for (int frame = 0; frame < frameCount; frame++)
                {
                    float frameWeight = originalMesh.GetBlendShapeFrameWeight(shapeIndex, frame);
                    Vector3[] deltaVertices = new Vector3[originalMesh.vertexCount];
                    Vector3[] deltaNormals = new Vector3[originalMesh.vertexCount];
                    Vector3[] deltaTangents = new Vector3[originalMesh.vertexCount];
                    originalMesh.GetBlendShapeFrameVertices(shapeIndex, frame, deltaVertices, deltaNormals, deltaTangents);

                    List<Vector3> newDeltaVertices = new List<Vector3>();
                    List<Vector3> newDeltaNormals = new List<Vector3>();
                    List<Vector3> newDeltaTangents = new List<Vector3>();

                    for (int i = 0; i < originalMesh.vertexCount; i++)
                    {
                        if (oldToNewIndexMap.ContainsKey(i))
                        {
                            newDeltaVertices.Add(deltaVertices[i]);
                            newDeltaNormals.Add(deltaNormals[i]);
                            newDeltaTangents.Add(deltaTangents[i]);
                        }
                    }

                    newMesh.AddBlendShapeFrame(shapeName, frameWeight,
                        newDeltaVertices.ToArray(),
                        newDeltaNormals.ToArray(),
                        newDeltaTangents.ToArray());
                }
            }
        }
    }

}
#endif