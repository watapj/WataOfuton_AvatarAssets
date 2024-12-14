using nadena.dev.ndmf;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using UnityEngine.TextCore;
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
                        // Debug.LogWarning("[MMDSetup] Body Meshes Missing.");
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

#if UNITY_2022_3_OR_NEWER
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

                        if (enableOverrideBS.Contains(true))
                        {
                            // BlendShapeを全削除
                            mesh.ClearBlendShapes();
                            // BlendShapeの数を取得
                            int blendShapeCount = smr.sharedMesh.blendShapeCount;

                            // MMD関係以外のBlendShapeを復元する
                            for (int i = 0; i < blendShapeCount; i++)
                            {
                                string blendShapeName = smr.sharedMesh.GetBlendShapeName(i);
                                bool isContain; int n;
                                (isContain, n) = ContainsString(blendShapeMappings4MMD, blendShapeName);
                                if (isContain && enableOverrideBS[n]) continue;

                                int vertexCount = smr.sharedMesh.vertexCount;
                                Vector3[] vertices = new Vector3[vertexCount];
                                Vector3[] normals = new Vector3[vertexCount];
                                Vector3[] tangents = new Vector3[vertexCount];
                                smr.sharedMesh.GetBlendShapeFrameVertices(i, 0, vertices, normals, tangents);

                                mesh.AddBlendShapeFrame(blendShapeName, 100f, vertices, normals, tangents);
                            }
                        }

                        for (int i = 0; i < blendShapeMappings4MMD.Length; i++)
                        {
                            blendShapeIndices1[i] += -1; // 0 番目には"----"が入っているので1つずらす
                            if (blendShapeIndices1[i] == -1)
                            {
                                // Debug.Log($"[MMDSetup] Skip Create BlendShape '{blendShapeMappings4MMD[i]}'.");
                                continue;
                            }
                            if (blendShapeIndices1[i] == -2)
                            {
                                // Debug.Log($"[MMDSetup] BlendShape '{blendShapeMappings4MMD[i]}' already exists.");
                                continue;
                            }
                            if (blendShapeIndices1[i] <= -3)
                            {
                                Debug.Log($"[MMDSetup] Something wrong '{blendShapeMappings4MMD[i]}'.");
                                continue;
                            }
                            if (blendShapePowers1[i] == 0f)
                            {
                                Debug.Log($"[MMDSetup] Skip Create BlendShape '{blendShapeMappings4MMD[i]}'(Zero Wight).");
                                continue;
                            }

                            if (enableBlendBS[i])
                            {
                                blendShapeIndices2[i] -= 1;
                                BlendingBlendShape4MMD(mesh, blendShapeMappings4MMD[i], blendShapeIndices1[i], blendShapePowers1[i] * 0.01f, blendShapeIndices2[i], blendShapePowers2[i] * 0.01f);
                                Debug.Log($"[MMDSetup] Blend BlendShape '{blendShapeMappings4MMD[i]}'.");
                            }
                            else
                            {
                                AddBlendShape4MMD(mesh, blendShapeMappings4MMD[i], blendShapeIndices1[i], blendShapePowers1[i] * 0.01f);
                                Debug.Log($"[MMDSetup] Generate BlendShape '{blendShapeMappings4MMD[i]}'.");
                            }
                        }
                    }
                    smr.sharedMesh = mesh;
                }
#endif

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

        public static bool BlendShapeExists(Mesh mesh, string name, bool isCheckOrdinal)
        {
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (isCheckOrdinal)
                {
                    if (mesh.GetBlendShapeName(i) == name)
                        return true;
                }
                else
                {
                    if (string.Equals(mesh.GetBlendShapeName(i), name, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }
        public static string[] blendShapeMappingsFace = new string[]
        {
            "vrc.v_aa",
            "vrc_v_aa",
            "vrc_v.aa",
            "lip_aa",
            "lip.aa",
            "mouse_a",
            "mouse.a",
        };


#if UNITY_2022_3_OR_NEWER
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

        private static void AddBlendShape4MMD(Mesh mesh, string newBlendShapeName, int OrigBlendShapeIndex, float power)
        {
            Vector3[] deltaVertices = new Vector3[mesh.vertexCount];
            Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
            Vector3[] deltaTangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(OrigBlendShapeIndex, 0, deltaVertices, deltaNormals, deltaTangents);

            for (int i = 0; i < deltaVertices.Length; i++)
            {
                deltaVertices[i] *= power; // power%のウェイトを適用
                deltaNormals[i] *= power;
                deltaTangents[i] *= power;
            }
            mesh.AddBlendShapeFrame(newBlendShapeName, 100, deltaVertices, deltaNormals, deltaTangents);
        }


        private static void BlendingBlendShape4MMD(Mesh mesh, string newBlendShapeName, int OrigBlendShapeIndex1, float power1, int OrigBlendShapeIndex2, float power2)
        {
            Vector3[] deltaVerticesA = new Vector3[mesh.vertexCount];
            Vector3[] deltaNormalsA = new Vector3[mesh.vertexCount];
            Vector3[] deltaTangentsA = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(OrigBlendShapeIndex1, 0, deltaVerticesA, deltaNormalsA, deltaTangentsA);
            Vector3[] deltaVerticesB = new Vector3[mesh.vertexCount];
            Vector3[] deltaNormalsB = new Vector3[mesh.vertexCount];
            Vector3[] deltaTangentsB = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(OrigBlendShapeIndex2, 0, deltaVerticesB, deltaNormalsB, deltaTangentsB);

            for (int i = 0; i < deltaVerticesA.Length; i++)
            {
                deltaVerticesA[i] = deltaVerticesA[i] * power1 + deltaVerticesB[i] * power2;
                deltaNormalsA[i] = deltaNormalsA[i] * power1 + deltaNormalsB[i] * power2;
                deltaTangentsA[i] = deltaTangentsA[i] * power1 + deltaTangentsB[i] * power2;
            }
            mesh.AddBlendShapeFrame(newBlendShapeName, 100f, deltaVerticesA, deltaNormalsA, deltaTangentsA);
        }


        public static string[] blendShapeMappings4MMD = new string[]
        {
            // https://images-wixmp-ed30a86b8c4ca887773594c2.wixmp.com/i/0b7b5e4b-c62e-41f7-8ced-1f3e58c4f5bf/d5nbmvp-5779f5ac-d476-426c-8ee6-2111eff8e76c.png
            "まばたき",
            "笑い",
            "ウィンク",
            "ウィンク右",
            "ウィンク２",
            // "ウィンク２右",
            "ｳｨﾝｸ２右",
            "なごみ",
            "はぅ",
            "びっくり",
            "じと目",
            "ｷﾘｯ",
            "なぬ！",
            "白目", // "はちゅ目",
            "星目",
            "はぁと",
            "瞳大",
            "瞳小",
            "恐ろしい子！",
            "ハイライト消し",

            "あ",
            "い",
            "う",
            "え",
            "お",
            // "あ２",
            "ん",
            "▲",
            "∧",
            "ワ",
            "□",
            "ω",
            "ω□",
            "えー",
            "はんっ！",
            "にやり",
            "にやり２",
            "にっこり",
            "ぺろっ",
            "てへぺろ",
            "てへぺろ２",

            "真面目",
            "困る",
            "にこり",
            "怒り",
            "上",
            "下",
            "照れ",
            "涙",
        };
#endif
    }
}
