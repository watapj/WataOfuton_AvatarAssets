#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using UnityEngine.Rendering;
using System.Linq;
using System.IO;

namespace WataOfuton.Tools.ReverseMeshND
{
    public class ReverseMeshND : MonoBehaviour, IEditorOnly
    {
        [SerializeField] public bool _isReversed;
        [SerializeField] public Mesh[] _origMesh;

        public void GetMesh()
        {
            var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(false);
            _origMesh = new Mesh[smrs.Length];
            for (int i = 0; i < smrs.Length; i++)
            {
                _origMesh[i] = smrs[i].sharedMesh;
            }
        }

        public void TryReverseMeshND()
        {
            Undo.RegisterCompleteObjectUndo(this, "Remove Mesh");
            var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(false);
            foreach (var smr in smrs)
                Undo.RegisterCompleteObjectUndo(smr, "Remove Mesh");

            if (_origMesh == null || _origMesh.Length == 0)
                GetMesh();

            FlipAllChildSMR();

            _isReversed = true;
        }

        void FlipAllChildSMR()
        {
            var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(false);
            if (smrs.Length == 0)
            {
                Debug.LogWarning("SkinnedMeshRendererが子階層に見つかりませんでした。");
                return;
            }

            Vector3 originalScale = transform.localScale;
            // 実行前にマイナススケールにしている場合、元に戻す
            if (originalScale.x < 0)
            {
                var s = originalScale;
                s.x *= -1;
                originalScale = s;
            }

            // 一時的にスケール反転
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);

            // ワールド回転情報の記録（ボーン用）
            Dictionary<Transform, Quaternion> boneWorldRotations = new Dictionary<Transform, Quaternion>();
            // 反転状態での SkinnedMeshRenderer 回転情報
            Dictionary<SkinnedMeshRenderer, Quaternion> smrWorldRotations = new Dictionary<SkinnedMeshRenderer, Quaternion>();
            Dictionary<SkinnedMeshRenderer, Vector3> smrWorldPositions = new Dictionary<SkinnedMeshRenderer, Vector3>();

            foreach (var smr in smrs)
            {
                if (!smrWorldRotations.ContainsKey(smr))
                {
                    smrWorldRotations[smr] = smr.transform.rotation;
                }
                if (!smrWorldPositions.ContainsKey(smr))
                {
                    smrWorldPositions[smr] = smr.transform.position;
                }

                foreach (var bone in smr.bones)
                {
                    if (bone != null && !boneWorldRotations.ContainsKey(bone))
                    {
                        boneWorldRotations[bone] = bone.rotation;
                    }
                }
            }

            // 反転状態でメッシュ頂点をワールド座標で取得
            Dictionary<SkinnedMeshRenderer, (Vector3[], Vector3[], Vector4[], int[][])> smrMeshData
                = new Dictionary<SkinnedMeshRenderer, (Vector3[], Vector3[], Vector4[], int[][])>();

            foreach (var smr in smrs)
            {
                var originalMesh = smr.sharedMesh;
                if (originalMesh == null) continue;

                // メッシュ情報取得
                var verts = originalMesh.vertices;
                var norms = originalMesh.normals;
                var tans = originalMesh.tangents;
                int subMeshCount = originalMesh.subMeshCount;

                var localToWorld = smr.transform.localToWorldMatrix;

                // ワールド座標へ変換（反転状態）
                Vector3[] wVertices = new Vector3[verts.Length];
                Vector3[] wNormals = new Vector3[norms.Length];
                Vector4[] wTangents = new Vector4[tans.Length];
                int[][] subMeshIndices = new int[subMeshCount][];

                for (int i = 0; i < verts.Length; i++)
                {
                    wVertices[i] = localToWorld.MultiplyPoint3x4(verts[i]);
                    wNormals[i] = localToWorld.MultiplyVector(norms[i]);
                    var t = tans[i];
                    // Tangentは方向ベクトルなのでVectorとして扱う
                    Vector3 tangentDir = new Vector3(t.x, t.y, t.z);
                    tangentDir = localToWorld.MultiplyVector(tangentDir);
                    wTangents[i] = new Vector4(tangentDir.x, tangentDir.y, tangentDir.z, t.w);
                }

                // インデックスを取得（反転後は裏表が逆になるため後で再反転する）
                for (int si = 0; si < subMeshCount; si++)
                {
                    subMeshIndices[si] = originalMesh.GetTriangles(si);
                }

                smrMeshData[smr] = (wVertices, wNormals, wTangents, subMeshIndices);
            }

            // スケールを元に戻す
            transform.localScale = originalScale;

            // ここから、取得したワールド座標の頂点/法線等を、「通常状態での」ローカル座標に変換してメッシュへ適用
            // 通常状態のtransformでlocalToWorldMatrixが異なるため、その逆行列を使ってローカル座標へ戻す
            HashSet<Transform> processedBones = new HashSet<Transform>();

            foreach (var smr in smrs)
            {
                var originalMesh = smr.sharedMesh;
                if (originalMesh == null) continue;

                // BlendShape の weight 一時保存とリセット
                int blendCount = originalMesh.blendShapeCount;
                float[] weights = new float[blendCount];
                for (int i = 0; i < blendCount; i++)
                {
                    weights[i] = smr.GetBlendShapeWeight(i);
                    smr.SetBlendShapeWeight(i, 0f); // 全て0にリセット
                }

                Mesh workingMesh = Instantiate(originalMesh);
                var (wVertices, wNormals, wTangents, subMeshIndices) = smrMeshData[smr];

                var worldMatrix = smr.transform.localToWorldMatrix;
                var invWorld = worldMatrix.inverse;

                // ワールド→ローカル変換（元スケール状態）
                for (int i = 0; i < wVertices.Length; i++)
                {
                    // 頂点をローカルへ戻す
                    var localPos = invWorld.MultiplyPoint3x4(wVertices[i]);
                    var localNormal = invWorld.MultiplyVector(wNormals[i]);
                    var tangentDir = new Vector3(wTangents[i].x, wTangents[i].y, wTangents[i].z);
                    tangentDir = invWorld.MultiplyVector(tangentDir);

                    wVertices[i] = localPos;
                    wNormals[i] = localNormal;
                    wTangents[i] = new Vector4(tangentDir.x, tangentDir.y, tangentDir.z, wTangents[i].w);
                }

                // ここでX軸反転済みになっている状態をメッシュに焼き込むため、インデックスを再度反転
                // （scaleを戻した段階で表裏が逆になっている可能性があるため、頂点は左右反転された状態を保持するが、
                //  面方向(インデックス順)も裏返す）
                for (int si = 0; si < workingMesh.subMeshCount; si++)
                {
                    var indices = subMeshIndices[si];
                    System.Array.Reverse(indices);
                    workingMesh.SetTriangles(indices, si);
                }

                workingMesh.vertices = wVertices;
                workingMesh.normals = wNormals;
                workingMesh.tangents = wTangents;

                // ボーン反転処理
                foreach (var bone in smr.bones)
                {
                    if (bone != null && !processedBones.Contains(bone))
                    {
                        FlipTransform(bone);
                        processedBones.Add(bone);
                    }
                }

                // バインドポーズ再計算
                var bones = smr.bones;
                var bindposes = new Matrix4x4[bones.Length];
                var rootToWorld = smr.transform.localToWorldMatrix;
                for (int i = 0; i < bones.Length; ++i)
                {
                    Transform bone = bones[i];
                    // bone が null の場合は元の bindpose を使うか、単に identity を入れる
                    bindposes[i] = (bone ? bone.worldToLocalMatrix : Matrix4x4.identity) * rootToWorld;
                }
                workingMesh.bindposes = bindposes;

                // BlendShape 以外をコピーする.
                Mesh newMesh = Instantiate(workingMesh);
                newMesh.ClearBlendShapes();

                // BlendShapeを反転
                for (int shapeIndex = 0; shapeIndex < workingMesh.blendShapeCount; shapeIndex++)
                {
                    string shapeName = workingMesh.GetBlendShapeName(shapeIndex);
                    int frameCount = workingMesh.GetBlendShapeFrameCount(shapeIndex);
                    for (int frame = 0; frame < frameCount; frame++)
                    {
                        float weight = workingMesh.GetBlendShapeFrameWeight(shapeIndex, frame);
                        Vector3[] deltaVertices = new Vector3[workingMesh.vertexCount];
                        Vector3[] deltaNormals = new Vector3[workingMesh.vertexCount];
                        Vector3[] deltaTangents = new Vector3[workingMesh.vertexCount];
                        workingMesh.GetBlendShapeFrameVertices(shapeIndex, frame, deltaVertices, deltaNormals, deltaTangents);

                        // X軸反転
                        for (int i = 0; i < deltaVertices.Length; i++)
                        {
                            deltaVertices[i].x = -deltaVertices[i].x;
                            deltaNormals[i].x = -deltaNormals[i].x;
                            deltaTangents[i].x = -deltaTangents[i].x;
                        }

                        newMesh.AddBlendShapeFrame(shapeName, weight, deltaVertices, deltaNormals, deltaTangents);
                    }
                }

                // メッシュ適用
                smr.sharedMesh = newMesh;

                // BlendShapeのweightを元に戻す
                for (int i = 0; i < weights.Length; i++)
                {
                    smr.SetBlendShapeWeight(i, weights[i]);
                }

                // Bounds も左右反転
                var bounds = smr.localBounds;
                var b = bounds.center;
                b.x = -b.x;
                bounds.center = b;
            }

            // 記録したワールド回転を再適用
            foreach (var kvp in boneWorldRotations)
            {
                kvp.Key.rotation = kvp.Value;
            }
            foreach (var kvp in smrWorldPositions)
            {
                kvp.Key.transform.position = kvp.Value;
            }
            foreach (var kvp in smrWorldRotations)
            {
                kvp.Key.transform.rotation = kvp.Value;
            }
        }

        static void FlipTransform(Transform t)
        {
            Vector3 localPos = t.localPosition;
            localPos.x = -localPos.x;
            t.localPosition = localPos;

            Vector3 localEuler = t.localEulerAngles;
            localEuler.y = -localEuler.y;
            localEuler.z = -localEuler.z;
            t.localEulerAngles = localEuler;
        }

        public void SaveMesh()
        {
            var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(false);

            for (int i = 0; i < _origMesh.Length; i++)
            {
                mySaveAssets(_origMesh[i], smrs[i], "ReverseMeshND");
            }
        }


        public static void mySaveAssets(Mesh originMesh, SkinnedMeshRenderer smr, string directoryName)
        {
            string fileName = smr.transform.name + ".asset";
            // FBXが配置されているフォルダのパスを取得
            string fbxFolderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(originMesh));

            // "directoryName" ディレクトリパスを作成
            string bodyMorphFolderPath = Path.Combine(fbxFolderPath, directoryName);

            // フォルダの親が "directoryName" でない場合のみ、"directoryName" ディレクトリを作成
            if (!fbxFolderPath.EndsWith(directoryName))
            {
                if (!AssetDatabase.IsValidFolder(bodyMorphFolderPath))
                {
                    AssetDatabase.CreateFolder(fbxFolderPath, directoryName);
                }
                string filePath = Path.Combine(bodyMorphFolderPath, fileName);
                AssetDatabase.CreateAsset(smr.sharedMesh, filePath);
            }
            else
            {
                // Meshを保存
                string filePath = Path.Combine(fbxFolderPath, fileName);
                AssetDatabase.CreateAsset(smr.sharedMesh, filePath);
            }
        }
    }

}
#endif