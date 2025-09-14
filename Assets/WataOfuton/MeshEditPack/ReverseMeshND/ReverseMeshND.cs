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

        SkinnedMeshRenderer[] GetTargetSMRs()
        {
            return GetComponentsInChildren<SkinnedMeshRenderer>(false);
        }

        public void GetMesh(SkinnedMeshRenderer[] smrs)
        {
            _origMesh = new Mesh[smrs.Length];
            for (int i = 0; i < smrs.Length; i++)
            {
                _origMesh[i] = smrs[i].sharedMesh;
            }
        }

        public void ExecuteReverseMeshND()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("ReverseMeshND");
            // Undoの対象を重複なく収集して一括登録（多重登録によるオーバーフロー回避）
            var smrs = GetTargetSMRs();
            var undoSet = new HashSet<Object> { this };
            foreach (var smr in smrs)
            {
                if (smr) undoSet.Add(smr);
            }
            foreach (var smr in smrs)
            {
                if (smr == null || smr.bones == null) continue;
                foreach (var bone in smr.bones)
                {
                    if (bone) undoSet.Add(bone);
                }
            }
            // メッシュアセット自体は編集しない（新規生成を割り当てる）ため、ここでは登録しない
            Undo.RegisterCompleteObjectUndo(undoSet.ToArray(), "ReverseMeshND Objects");

            if (_origMesh == null || _origMesh.Length == 0 || _origMesh.Length != smrs.Length)
                GetMesh(smrs);

            FlipAllChildSMR(smrs);

            _isReversed = true;

            Undo.CollapseUndoOperations(undoGroup);
        }

        // NOTE : 法線・接線の反転処理は不要なので実装しない
        void FlipAllChildSMR(SkinnedMeshRenderer[] smrs)
        {
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

            // 再計算フローに備えて、現在のシーンポーズ（SMRとボーンのワールドTRS）を保存
            Dictionary<SkinnedMeshRenderer, Quaternion> smrWorldRotations = new Dictionary<SkinnedMeshRenderer, Quaternion>();
            Dictionary<SkinnedMeshRenderer, Vector3> smrWorldPositions = new Dictionary<SkinnedMeshRenderer, Vector3>();
            foreach (var smr in smrs)
            {
                if (!smrWorldRotations.ContainsKey(smr)) smrWorldRotations[smr] = smr.transform.rotation;
                if (!smrWorldPositions.ContainsKey(smr)) smrWorldPositions[smr] = smr.transform.position;
            }

            // 一時的にスケール反転（メッシュ座標取得のため）
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);

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
            // まずは全SMRで共有される骨を一度だけ"rest"へ戻し、全体を反転する。
            // これをSMRごとに行うと、先に処理したSMRの骨反転が後のSMR処理で上書きされるため、順序依存の不具合が起きる。
            var boneRestInfo = new Dictionary<Transform, (Matrix4x4 bindpose, Matrix4x4 rootToWorld)>();
            foreach (var smr in smrs)
            {
                var mesh = smr.sharedMesh;
                var bones = smr.bones;
                if (mesh == null || bones == null) continue;
                var binds = mesh.bindposes;
                if (binds == null) continue;
                int n = Mathf.Min(bones.Length, binds.Length);
                var rootToWorld = smr.transform.localToWorldMatrix;
                for (int i = 0; i < n; ++i)
                {
                    var bone = bones[i];
                    if (bone == null) continue;
                    if (!boneRestInfo.ContainsKey(bone))
                    {
                        boneRestInfo[bone] = (binds[i], rootToWorld);
                    }
                }
            }

            // 骨をrestへ戻す（各骨につき一度だけ）
            foreach (var kv in boneRestInfo)
            {
                var bone = kv.Key;
                var bindpose = kv.Value.bindpose;
                var rootToWorldPre = kv.Value.rootToWorld;
                var invBind = bindpose.inverse; // = rootWorld^-1 * boneWorld
                Matrix4x4 targetWorld = rootToWorldPre * invBind; // バインド時のboneのworld行列
                Matrix4x4 parentWorld = bone.parent ? bone.parent.localToWorldMatrix : Matrix4x4.identity;
                Matrix4x4 targetLocal = parentWorld.inverse * targetWorld;
                if (DecomposeMatrix(targetLocal, out var lPos, out var lRot, out var lScale))
                {
                    bone.localPosition = lPos;
                    bone.localRotation = lRot;
                    bone.localScale = lScale;
                }
            }

            // 骨の反転も一度だけ
            foreach (var bone in boneRestInfo.Keys)
            {
                FlipTransform(bone);
            }

            foreach (var smr in smrs)
            {
                var originalMesh = smr.sharedMesh;
                if (originalMesh == null || !smrMeshData.ContainsKey(smr)) continue;

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

                // インデックス反転（三角形ごとに入れ替え）
                for (int si = 0; si < workingMesh.subMeshCount; si++)
                {
                    var indices = subMeshIndices[si];
                    if (indices == null || indices.Length % 3 != 0) continue;
                    for (int t = 0; t < indices.Length; t += 3)
                    {
                        // 0,1,2 -> 1,0,2
                        int tmp = indices[t];
                        indices[t] = indices[t + 1];
                        indices[t + 1] = tmp;
                    }
                    workingMesh.SetTriangles(indices, si);
                }

                workingMesh.vertices = wVertices;
                workingMesh.normals = wNormals;
                workingMesh.tangents = wTangents;

                // バインドポーズ再計算（null ボーンを考慮）: 骨はすでに一度だけ反転済み
                var bones = smr.bones;
                var bindposes = new Matrix4x4[bones.Length];
                var rootToWorld = smr.transform.localToWorldMatrix;
                for (int i = 0; i < bones.Length; ++i)
                {
                    Transform bone = bones[i];
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
                Undo.RegisterCreatedObjectUndo(newMesh, "ReverseMeshND Create Mesh");
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
                smr.localBounds = bounds;
            }

            // 先にSMRのワールド位置/回転を復元
            foreach (var kvp in smrWorldPositions)
            {
                kvp.Key.transform.position = kvp.Value;
            }
            foreach (var kvp in smrWorldRotations)
            {
                kvp.Key.transform.rotation = kvp.Value;
            }
        }

        // バインドポーズ復元用: 行列をTRSに分解
        static bool DecomposeMatrix(Matrix4x4 m, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = m.GetColumn(3);
            Vector3 x = new Vector3(m.m00, m.m10, m.m20);
            Vector3 y = new Vector3(m.m01, m.m11, m.m21);
            Vector3 z = new Vector3(m.m02, m.m12, m.m22);
            float sx = x.magnitude; float sy = y.magnitude; float sz = z.magnitude;
            if (sx < 1e-8f || sy < 1e-8f || sz < 1e-8f)
            {
                rotation = Quaternion.identity; scale = Vector3.one; return false;
            }
            // 正規化して回転行列を構築
            Vector3 xn = x / sx; Vector3 yn = y / sy; Vector3 zn = z / sz;
            // 右手系補正（手性が負ならZ軸を反転してスケール符号に反映）
            float handed = Vector3.Dot(Vector3.Cross(xn, yn), zn);
            if (handed < 0f)
            {
                sz = -sz; zn = -zn;
            }
            rotation = Quaternion.LookRotation(zn, yn);
            scale = new Vector3(sx, sy, sz);
            return true;
        }

        static void FlipTransform(Transform t)
        {
            Vector3 localPos = t.localPosition;
            localPos.x = -localPos.x;
            t.localPosition = localPos;
            Quaternion localRot = t.localRotation;
            localRot.y = -localRot.y;
            localRot.z = -localRot.z;
            t.localRotation = localRot;
        }

        public void SaveMesh()
        {
            var smrs = GetTargetSMRs();

            if (_origMesh == null || _origMesh.Length != smrs.Length)
            {
                // 対象のズレを避けるため再取得
                GetMesh(smrs);
            }

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