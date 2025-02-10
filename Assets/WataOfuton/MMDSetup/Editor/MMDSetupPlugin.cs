using nadena.dev.ndmf;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

[assembly: ExportsPlugin(typeof(WataOfuton.Tools.MMDSetup.Editor.MMDSetupPlugin))]

namespace WataOfuton.Tools.MMDSetup.Editor
{
    public class MMDSetupPlugin : Plugin<MMDSetupPlugin>
    {
        public override string DisplayName => nameof(MMDSetup);
        private static string rename = "body_renamed";
        private static string replacePath;
        private static string origFaceName;
        private static string[] origBodiesName;
        private static Vector3[] reuseVerticesA, reuseNormalsA, reuseTangentsA;
        private static Vector3[] reuseVerticesB, reuseNormalsB, reuseTangentsB;

        protected override void Configure()
        {
            // 顔メッシュと素体メッシュのリネーム処理と
            // アニメーションのリネーム処理の実行場所を別にすることで
            // 他 NDMF ツールとの衝突を回避する試み
            InPhase(BuildPhase.Generating).AfterPlugin("nadena.dev.modular-avatar").Run(nameof(MMDSetup), ctx =>
            {
                var MMDSetup = ctx.AvatarRootObject.GetComponentInChildren<MMDSetup>();
                if (MMDSetup == null)
                {
                    Object.DestroyImmediate(MMDSetup);
                    return;
                }

                var face = MMDSetup.faceMesh;
                if (face == null)
                {
                    Debug.LogWarning("[MMDSetup] Face Mesh is Unassigned.");
                    Object.DestroyImmediate(MMDSetup);
                    return;
                }

                replacePath = GetPathToRoot(face, ctx.AvatarRootObject.transform);
                if (replacePath != face.name)
                {
                    face.SetParent(ctx.AvatarRootObject.transform);
                }
                var bodies = MMDSetup.bodyMeshes;
                origBodiesName = new string[bodies.Count];
                for (int i = 0; i < bodies.Count; i++)
                {
                    if (bodies[i] == null)
                    {
                        Debug.LogWarning("[MMDSetup] Body Meshes Missing.");
                        continue;
                    }
                    if (bodies[i].GetInstanceID() == face.GetInstanceID())
                    {
                        origFaceName = face.name;
                        face.name = "Body";
                    }
                    else
                    {
                        origBodiesName[i] = GetPathToRoot(bodies[i], ctx.AvatarRootObject.transform);
                        bodies[i].name = $"{rename}{i}";
                    }
                }
            });


            InPhase(BuildPhase.Transforming).AfterPlugin("nadena.dev.modular-avatar").Run(nameof(MMDSetup), ctx =>
            {
                var MMDSetup = ctx.AvatarRootObject.GetComponentInChildren<MMDSetup>();
                if (MMDSetup == null)
                {
                    Object.DestroyImmediate(MMDSetup);
                    return;
                }

                var face = MMDSetup.faceMesh;
                if (face == null)
                {
                    Debug.LogWarning("[MMDSetup] Face Mesh is Unassigned.");
                    Object.DestroyImmediate(MMDSetup);
                    return;
                }

                var bodies = MMDSetup.bodyMeshes;
                if ((origFaceName != "Body") || replacePath.Contains("/"))
                {
                    var descriptor = ctx.AvatarRootObject.GetComponentInChildren<VRCAvatarDescriptor>();
                    if (descriptor.baseAnimationLayers == null)
                    {
                        Debug.LogWarning("[MMDSetup] Playable Layers is Null.");
                    }
                    else
                    {
                        for (int i = 0; i < descriptor.baseAnimationLayers.Length; i++)
                        {
                            var animC = descriptor.baseAnimationLayers[i].animatorController;
                            if (animC == null) continue;

                            AnimatorController controller = animC as AnimatorController;
                            if (controller == null)
                            {
                                Debug.Log($"[MMDSetup] No {animC.name} AnimatorController found on the Animator.");
                            }
                            else
                            {
                                // 各アニメーションクリップのパスを置き換える
                                foreach (var layer in controller.layers)
                                {
                                    ProcessStateMachine(layer.stateMachine, bodies, face);
                                }
                            }
                        }
                    }
                }

                if (MMDSetup.enableGenerateBS)
                {
                    var smr = face.GetComponent<SkinnedMeshRenderer>();
                    Mesh mesh = Object.Instantiate(smr.sharedMesh);
                    if (mesh != null)
                    {
                        var blendShapeIndices1 = MMDSetup.blendShapeIndices1;
                        var blendShapePowers1 = MMDSetup.blendShapePowers1;
                        var blendShapeIndices2 = MMDSetup.blendShapeIndices2;
                        var blendShapePowers2 = MMDSetup.blendShapePowers2;
                        var enableBlendBS = MMDSetup.enableBlendBS;
                        var enableOverrideBS = MMDSetup.enableOverrideBS;

                        string[] mappinglist = BlendShapeMappings.blendShapeMappings4MMD;

                        if (enableOverrideBS.Contains(true))
                        {
                            // BlendShapeを全削除
                            mesh.ClearBlendShapes();
                            // BlendShapeの数を取得
                            int originalCount = smr.sharedMesh.blendShapeCount;

                            // MMD関係以外のBlendShapeを復元する
                            for (int i = 0; i < originalCount; i++)
                            {
                                string shapeName = smr.sharedMesh.GetBlendShapeName(i);
                                bool isInMMDList; int idxInMMDList;
                                (isInMMDList, idxInMMDList) = ContainsString(mappinglist, shapeName);
                                if (isInMMDList && enableOverrideBS[idxInMMDList]) continue;

                                int vertexCount = smr.sharedMesh.vertexCount;
                                Vector3[] vertices = new Vector3[vertexCount];
                                Vector3[] normals = new Vector3[vertexCount];
                                Vector3[] tangents = new Vector3[vertexCount];
                                smr.sharedMesh.GetBlendShapeFrameVertices(i, 0, vertices, normals, tangents);

                                mesh.AddBlendShapeFrame(shapeName, 100f, vertices, normals, tangents);
                            }
                        }

                        for (int i = 0; i < mappinglist.Length; i++)
                        {
                            string targetBSName = mappinglist[i];

                            int popupValue1 = blendShapeIndices1[i];
                            // popupValue1 == 0 -> “----” を選択中 -> スキップ
                            if (popupValue1 == 0)
                            {
                                // Debug.Log($"[MMDSetup] Skip {targetBSName}");
                                continue;
                            }

                            // 1->メッシュ上のインデックス0, 2->1, 3->2...
                            int realIndex1 = popupValue1 - 1;
                            if (realIndex1 < 0 || realIndex1 >= smr.sharedMesh.blendShapeCount)
                            {
                                // 範囲外 -> スキップ
                                continue;
                            }

                            // BlendモードがONなら blendShapeIndices2 も見る
                            if (enableBlendBS[i])
                            {
                                int popupValue2 = blendShapeIndices2[i];
                                int realIndex2 = popupValue2 - 1;

                                if (realIndex2 < 0 || realIndex2 >= smr.sharedMesh.blendShapeCount)
                                {
                                    // 2番目シェイプがスキップ扱い -> 単独生成
                                    AddBlendShape4MMD(mesh, targetBSName, realIndex1, blendShapePowers1[i] * 0.01f);
                                }
                                else
                                {
                                    // 2つのBlendShapeを合成
                                    BlendingBlendShape4MMD(mesh, targetBSName, realIndex1, blendShapePowers1[i] * 0.01f, realIndex2, blendShapePowers2[i] * 0.01f);
                                }
                            }
                            else
                            {
                                // Blendしない -> 単独生成
                                AddBlendShape4MMD(mesh, targetBSName, realIndex1, blendShapePowers1[i] * 0.01f);
                            }
                        }
                    }
                    smr.sharedMesh = mesh;
                }

                Object.DestroyImmediate(MMDSetup);
            });
        }

        private static void ProcessStateMachine(AnimatorStateMachine stateMachine, List<Transform> bodies, Transform face)
        {
            // 状態の処理
            foreach (var state in stateMachine.states)
            {
                AnimationClip clip = state.state.motion as AnimationClip;
                if (clip != null)
                {
                    for (int i = 0; i < bodies.Count; i++)
                    {
                        if (bodies[i].GetInstanceID() == face.GetInstanceID()) continue;
                        ReplacePathsInClip(clip, origBodiesName[i], $"{rename}{i}");
                    }
                    ReplacePathsInClip(clip, replacePath, "Body");
                }
            }

            // サブステートマシンの処理
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                ProcessStateMachine(subStateMachine.stateMachine, bodies, face);
            }
        }

        private static void ReplacePathsInClip(AnimationClip clip, string targetPath, string replaceName)
        {
            // アニメーションクリップ内の全バインディングを取得
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in bindings)
            {
                if (!binding.path.StartsWith(targetPath)) continue;

                string remainingPath = binding.path.Substring(targetPath.Length);
                string newPath = replaceName + remainingPath;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                AnimationUtility.SetEditorCurve(clip, binding, null); // 古いバインディングを削除
                EditorCurveBinding newBinding = new EditorCurveBinding
                {
                    path = newPath,
                    propertyName = binding.propertyName,
                    type = binding.type
                };
                AnimationUtility.SetEditorCurve(clip, newBinding, curve); // 新しいバインディングに追加
            }
        }

        private static string GetPathToRoot(Transform current, Transform root)
        {
            if (current == null) return "";
            string path = current.gameObject.name;
            while (current.parent != null && current.parent != root)
            {
                current = current.parent;
                path = current.gameObject.name + "/" + path;
            }
            return path;
        }

        private static (bool, int) ContainsString(string[] array, string target)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == target)
                {
                    return (true, i);
                }
            }
            return (false, -1);
        }

        private static void PrepareReuseArrays(int vertexCount)
        {
            // 初回または、vertexCount が変わった場合のみ再確保
            if (reuseVerticesA == null || reuseVerticesA.Length != vertexCount)
            {
                reuseVerticesA = new Vector3[vertexCount];
                reuseNormalsA = new Vector3[vertexCount];
                reuseTangentsA = new Vector3[vertexCount];
                reuseVerticesB = new Vector3[vertexCount];
                reuseNormalsB = new Vector3[vertexCount];
                reuseTangentsB = new Vector3[vertexCount];
            }
        }

        private static void AddBlendShape4MMD(Mesh mesh, string newBlendShapeName, int OrigBlendShapeIndex, float power)
        {
            if (power == 0f)
            {
                Debug.Log($"[MMDSetup] Skip Create BlendShape '{newBlendShapeName}'(Zero Wight).");
                return;
            }

            int vertexCount = mesh.vertexCount;
            PrepareReuseArrays(vertexCount);
            // reuseVerticesA / reuseNormalsA / reuseTangentsA にフレームを取得
            mesh.GetBlendShapeFrameVertices(OrigBlendShapeIndex, 0, reuseVerticesA, reuseNormalsA, reuseTangentsA);

            for (int i = 0; i < vertexCount; i++)
            {
                reuseVerticesA[i] = reuseVerticesA[i] * power; // power%のウェイトを適用
                reuseNormalsA[i] = reuseNormalsA[i] * power;
                reuseTangentsA[i] = reuseTangentsA[i] * power;
            }
            mesh.AddBlendShapeFrame(newBlendShapeName, 100f, reuseVerticesA, reuseNormalsA, reuseTangentsA);
        }

        private static void BlendingBlendShape4MMD(Mesh mesh, string newBlendShapeName, int OrigBlendShapeIndex1, float power1, int OrigBlendShapeIndex2, float power2)
        {
            if (power1 == 0f && power2 == 0f)
            {
                Debug.Log($"[MMDSetup] Skip Create BlendShape '{newBlendShapeName}'(Both BlendShapes have Zero Weight).");
                return;
            }
            if (power1 == 0f)
            {
                Debug.Log($"[MMDSetup] BlendShape1 has Zero Weight for '{newBlendShapeName}'. Using BlendShape2 only.");
            }
            if (power2 == 0f)
            {
                Debug.Log($"[MMDSetup] BlendShape2 has Zero Weight for '{newBlendShapeName}'. Using BlendShape1 only.");
            }

            int vertexCount = mesh.vertexCount;
            PrepareReuseArrays(vertexCount);
            mesh.GetBlendShapeFrameVertices(OrigBlendShapeIndex1, 0, reuseVerticesA, reuseNormalsA, reuseTangentsA);
            mesh.GetBlendShapeFrameVertices(OrigBlendShapeIndex2, 0, reuseVerticesB, reuseNormalsB, reuseTangentsB);

            // 加算合成
            for (int i = 0; i < vertexCount; i++)
            {
                reuseVerticesA[i] = reuseVerticesA[i] * power1 + reuseVerticesB[i] * power2;
                reuseNormalsA[i] = reuseNormalsA[i] * power1 + reuseNormalsB[i] * power2;
                reuseTangentsA[i] = reuseTangentsA[i] * power1 + reuseTangentsB[i] * power2;
            }
            mesh.AddBlendShapeFrame(newBlendShapeName, 100f, reuseVerticesA, reuseNormalsA, reuseTangentsA);
        }
    }
}
