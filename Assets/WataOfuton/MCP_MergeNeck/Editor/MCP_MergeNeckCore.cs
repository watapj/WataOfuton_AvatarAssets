using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WataOfuton.Tools.MCP_MergeNeck
{
    public class MCP_MergeNeckCore : MonoBehaviour
    {
        private static float baryEpsilon = 1e-4f; // バリセントリック計算時の誤差許容値

        public static void ApplyDiffDataNDMF(SkinnedMeshRenderer targetFaceRenderer, SkinnedMeshRenderer targetBodyRenderer, TriangleDiffDataAll triangleDiffDataAll)
        {
            if (triangleDiffDataAll == null)
            {
                Debug.LogError("[ApplyTriangleDiffData] 差分データが設定されていません。");
                return;
            }
            if (targetFaceRenderer == null || targetBodyRenderer == null)
            {
                Debug.LogError("[ApplyTriangleDiffData] 対象のRendererが設定されていません。");
                return;
            }

            // 顔に差分適用
            Mesh faceFinalMesh = ApplyTriangleDiffData(targetFaceRenderer, triangleDiffDataAll.faceTriangles);
            // 体に差分適用
            Mesh bodyFinalMesh = ApplyTriangleDiffData(targetBodyRenderer, triangleDiffDataAll.bodyTriangles);

            Debug.Log("[ApplyTriangleDiffData] 差分の適用が完了しました。");
        }

        public static Mesh ApplyTriangleDiffData(SkinnedMeshRenderer skinned, TriangleDiff[] triDiffs)
        {
            Mesh userMesh = skinned.sharedMesh;
            if (userMesh == null)
            {
                Debug.LogError($"[ApplyTriangleDiffData] Renderer {skinned.name} のメッシュが存在しません。");
                return null;
            }

            // 頂点配列を取得(コピーしてから操作)
            Mesh newMesh = Instantiate(userMesh);
            Vector3[] origVerts = newMesh.vertices;
            Vector3[] origNormals = newMesh.normals;
            Vector4[] origTangents = newMesh.tangents;
            Vector2[] origUV = userMesh.uv;
            int vCount = origVerts.Length;
            Vector3[] newVerts = new Vector3[vCount];
            Vector3[] newNormals = new Vector3[vCount];
            Vector4[] newTangents = new Vector4[vCount];

            // メッシュの BoneWeight 配列を取得
            BoneWeight[] boneWeights = newMesh.boneWeights;

            Transform skinnedTransform = skinned.transform;

            // // 初期は元の頂点をコピー
            // for (int i = 0; i < vCount; i++)
            // {
            //     newVerts[i] = origVerts[i];
            //     newNormals[i] = origNormals[i];
            //     newTangents[i] = origTangents[i];
            // }

            // 各ユーザーメッシュの頂点について、差分を適用する
            for (int i = 0; i < vCount; i++)
            {
                newVerts[i] = origVerts[i];
                newNormals[i] = origNormals[i];
                newTangents[i] = origTangents[i];

                Vector2 uv = origUV[i];
                Vector3 interpolatedDiff = Vector3.zero;
                Vector3 interpolatedNormalDiff = Vector3.zero;
                Vector3 interpolatedTangentDiff = Vector3.zero;
                BoneWeight newBoneWeight = boneWeights[i]; // 初期値（補間できなかった場合は元データを維持）

                bool found = false;

                // すべての三角形データをチェック
                foreach (var tri in triDiffs)
                {
                    if (ComputeBarycentric2D(uv, tri.uv0, tri.uv1, tri.uv2, out float w0, out float w1, out float w2))
                    {
                        // 補間して差分を算出
                        interpolatedDiff = tri.posDelta0 * w0 + tri.posDelta1 * w1 + tri.posDelta2 * w2;
                        interpolatedNormalDiff = tri.normalDelta0 * w0 + tri.normalDelta1 * w1 + tri.normalDelta2 * w2;
                        interpolatedTangentDiff = tri.tangentDelta0 * w0 + tri.tangentDelta1 * w1 + tri.tangentDelta2 * w2;
                        // 骨ウェイトも補間
                        newBoneWeight = InterpolateBoneWeight(tri.boneWeight0, tri.boneWeight1, tri.boneWeight2, w0, w1, w2);
                        found = true;
                        break;
                    }
                }

                // 差分が見つかった場合は加算 (オリジナル + 差分)
                if (found)
                {
                    // 頂点座標の適用 (ワールド座標 → ローカル座標)
                    Vector3 wpos = skinnedTransform.localToWorldMatrix.MultiplyPoint3x4(origVerts[i]) + interpolatedDiff;
                    newVerts[i] = skinnedTransform.worldToLocalMatrix.MultiplyPoint3x4(wpos);

                    // 法線の適用 (ワールド座標 → ローカル座標)
                    Vector3 wNormal = skinnedTransform.TransformDirection(origNormals[i]) + interpolatedNormalDiff;
                    newNormals[i] = skinnedTransform.InverseTransformDirection(wNormal).normalized;

                    // 接線の適用 (xyz成分のみ適用, w成分は元データを維持)
                    Vector3 wTangent = skinnedTransform.TransformDirection(new Vector3(origTangents[i].x, origTangents[i].y, origTangents[i].z)) + interpolatedTangentDiff;
                    Vector3 localTangent = skinnedTransform.InverseTransformDirection(wTangent).normalized;
                    newTangents[i] = new Vector4(localTangent.x, localTangent.y, localTangent.z, origTangents[i].w);

                    // 補間した骨ウェイトの適用
                    boneWeights[i] = newBoneWeight;
                }
            }

            newMesh.vertices = newVerts;
            newMesh.normals = newNormals;
            newMesh.tangents = newTangents;
            newMesh.boneWeights = boneWeights;

            // 新MeshをRendererに適用
            skinned.sharedMesh = newMesh;
            Debug.Log($"[ApplyTriangleDiffData] {skinned.name} に差分を適用しました。");

            return newMesh;
        }

        /// <summary>
        /// 2Dバリセントリック計算
        /// p が三角形(a, b, c) 内にあるなら、w0, w1, w2 を出力する。
        /// </summary>
        private static bool ComputeBarycentric2D(
            Vector2 p, Vector2 a, Vector2 b, Vector2 c,
            out float w0, out float w1, out float w2)
        {
            w0 = w1 = w2 = 0f;
            float den = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(den) < 1e-12f)
                return false;

            float w0r = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / den;
            float w1r = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / den;
            float w2r = 1f - w0r - w1r;

            // eps を使って許容誤差を考慮
            if (w0r < -baryEpsilon || w1r < -baryEpsilon || w2r < -baryEpsilon ||
                w0r > 1 + baryEpsilon || w1r > 1 + baryEpsilon || w2r > 1 + baryEpsilon)
                return false;

            w0 = w0r; w1 = w1r; w2 = w2r;
            return true;
        }

        /// <summary>
        /// 3 つの BoneWeight を、バリセントリック補間係数 w0, w1, w2 を用いて線形補間し、
        /// 上位 4 つのボーンに正規化した結果を返します。
        /// </summary>
        private static BoneWeight InterpolateBoneWeight(BoneWeight bw0, BoneWeight bw1, BoneWeight bw2, float w0, float w1, float w2)
        {
            // 各頂点の BoneWeight から、各ボーンの寄与値を合算するための辞書
            Dictionary<int, float> boneWeightDict = new Dictionary<int, float>();

            // bw0 の寄与を加算
            AddBoneWeightContribution(boneWeightDict, bw0.boneIndex0, bw0.weight0 * w0);
            AddBoneWeightContribution(boneWeightDict, bw0.boneIndex1, bw0.weight1 * w0);
            AddBoneWeightContribution(boneWeightDict, bw0.boneIndex2, bw0.weight2 * w0);
            AddBoneWeightContribution(boneWeightDict, bw0.boneIndex3, bw0.weight3 * w0);

            // bw1 の寄与を加算
            AddBoneWeightContribution(boneWeightDict, bw1.boneIndex0, bw1.weight0 * w1);
            AddBoneWeightContribution(boneWeightDict, bw1.boneIndex1, bw1.weight1 * w1);
            AddBoneWeightContribution(boneWeightDict, bw1.boneIndex2, bw1.weight2 * w1);
            AddBoneWeightContribution(boneWeightDict, bw1.boneIndex3, bw1.weight3 * w1);

            // bw2 の寄与を加算
            AddBoneWeightContribution(boneWeightDict, bw2.boneIndex0, bw2.weight0 * w2);
            AddBoneWeightContribution(boneWeightDict, bw2.boneIndex1, bw2.weight1 * w2);
            AddBoneWeightContribution(boneWeightDict, bw2.boneIndex2, bw2.weight2 * w2);
            AddBoneWeightContribution(boneWeightDict, bw2.boneIndex3, bw2.weight3 * w2);

            // 寄与値の大きい順に並べ、上位 4 つを採用
            var sorted = boneWeightDict.OrderByDescending(pair => pair.Value).Take(4).ToList();

            // 正規化
            float total = sorted.Sum(pair => pair.Value);
            BoneWeight result = new BoneWeight();
            if (total > 0)
            {
                for (int i = 0; i < sorted.Count; i++)
                {
                    float normalizedWeight = sorted[i].Value / total;
                    switch (i)
                    {
                        case 0:
                            result.boneIndex0 = sorted[i].Key;
                            result.weight0 = normalizedWeight;
                            break;
                        case 1:
                            result.boneIndex1 = sorted[i].Key;
                            result.weight1 = normalizedWeight;
                            break;
                        case 2:
                            result.boneIndex2 = sorted[i].Key;
                            result.weight2 = normalizedWeight;
                            break;
                        case 3:
                            result.boneIndex3 = sorted[i].Key;
                            result.weight3 = normalizedWeight;
                            break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 指定した boneIndex の寄与値を辞書に加算するヘルパー関数
        /// </summary>
        private static void AddBoneWeightContribution(Dictionary<int, float> dict, int boneIndex, float weight)
        {
            if (dict.ContainsKey(boneIndex))
                dict[boneIndex] += weight;
            else
                dict[boneIndex] = weight;
        }
    }
}