using nadena.dev.ndmf;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;

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
                    Mesh sourceMesh = smr.sharedMesh;
                    var originalBlendShapeWeightsByName = CaptureBlendShapeWeightsByName(smr, sourceMesh);

                    Mesh mesh = Object.Instantiate(sourceMesh);
                    if (mesh != null)
                    {
                        var blendShapeIndices1 = MMDSetup.blendShapeIndices1;
                        var blendShapeNames1 = MMDSetup.blendShapeNames1;
                        var blendShapePowers1 = MMDSetup.blendShapePowers1;
                        var blendShapeIndices2 = MMDSetup.blendShapeIndices2;
                        var blendShapeNames2 = MMDSetup.blendShapeNames2;
                        var blendShapePowers2 = MMDSetup.blendShapePowers2;
                        var enableBlendBS = MMDSetup.enableBlendBS;
                        var enableOverrideBS = MMDSetup.enableOverrideBS;

                        string[] mappinglist = BlendShapeMappings.blendShapeMappings4MMD;

                        if (HasAnyTrue(enableOverrideBS))
                        {
                            // Override対象は「overrideがON」だけでなく「このビルドで実際に再生成するもの」だけに限定する。
                            // そうしないと、同名BlendShapeが存在するケースで意図せず復元漏れが発生する。
                            var overrideTargetNames = new HashSet<string>();
                            for (int i = 0; i < mappinglist.Length; i++)
                            {
                                if (!enableOverrideBS[i])
                                {
                                    continue;
                                }

                                // 再生成しない（未選択）の場合は、既存を復元する
                                int resolvedIndex = ResolveBlendShapeIndex(sourceMesh, blendShapeNames1, blendShapeIndices1, i);
                                if (resolvedIndex < 0)
                                {
                                    continue;
                                }

                                if (i < blendShapePowers1.Length && blendShapePowers1[i] == 0f)
                                {
                                    // 再生成してもゼロウェイトなら実質無効なので、既存を復元する
                                    continue;
                                }

                                overrideTargetNames.Add(mappinglist[i]);
                            }

                            // BlendShapeを全削除
                            mesh.ClearBlendShapes();
                            // BlendShapeの数を取得
                            int originalCount = sourceMesh.blendShapeCount;

                            // MMD関係以外のBlendShapeを復元する
                            for (int i = 0; i < originalCount; i++)
                            {
                                string shapeName = sourceMesh.GetBlendShapeName(i);
                                // 再生成対象に含まれる名前は復元しない（後続で作り直す）
                                if (overrideTargetNames.Contains(shapeName))
                                {
                                    continue;
                                }

                                int vertexCount = sourceMesh.vertexCount;
                                Vector3[] vertices = new Vector3[vertexCount];
                                Vector3[] normals = new Vector3[vertexCount];
                                Vector3[] tangents = new Vector3[vertexCount];
                                sourceMesh.GetBlendShapeFrameVertices(i, 0, vertices, normals, tangents);

                                mesh.AddBlendShapeFrame(shapeName, 100f, vertices, normals, tangents);
                            }
                        }

                        for (int i = 0; i < mappinglist.Length; i++)
                        {
                            string targetBSName = mappinglist[i];

                            // indexではなく名前優先で参照（削除/並べ替えに強くする）
                            int realIndex1 = ResolveBlendShapeIndex(sourceMesh, blendShapeNames1, blendShapeIndices1, i);
                            if (realIndex1 < 0 || realIndex1 >= sourceMesh.blendShapeCount)
                            {
                                // 未選択 or 範囲外 -> スキップ
                                continue;
                            }

                            // BlendモードがONなら blendShapeIndices2 も見る
                            if (enableBlendBS[i])
                            {
                                int realIndex2 = ResolveBlendShapeIndex(sourceMesh, blendShapeNames2, blendShapeIndices2, i);
                                if (realIndex2 < 0 || realIndex2 >= sourceMesh.blendShapeCount)
                                {
                                    // 2番目シェイプがスキップ扱い -> 単独生成
                                    AddBlendShape4MMD(mesh, sourceMesh, targetBSName, realIndex1, blendShapePowers1[i] * 0.01f);
                                }
                                else
                                {
                                    // 2つのBlendShapeを合成
                                    BlendingBlendShape4MMD(mesh, sourceMesh, targetBSName, realIndex1, blendShapePowers1[i] * 0.01f, realIndex2, blendShapePowers2[i] * 0.01f);
                                }
                            }
                            else
                            {
                                // Blendしない -> 単独生成
                                AddBlendShape4MMD(mesh, sourceMesh, targetBSName, realIndex1, blendShapePowers1[i] * 0.01f);
                            }
                        }
                    }
                    smr.sharedMesh = mesh;
                    RestoreBlendShapeWeightsByName(smr, mesh, originalBlendShapeWeightsByName);
                }

                Object.DestroyImmediate(MMDSetup);
            });
        }

        /// <summary>
        /// AnimatorStateMachine配下のステートを走査し、アニメーションクリップ内のパス置換を行います。
        /// サブステートマシンも再帰的に処理します。
        /// </summary>
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

        /// <summary>
        /// AnimationClip内のカーブバインディングのパスを置換します。
        /// 例: "Armature/Body" を "Body" に置き換える等。
        /// </summary>
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

        /// <summary>
        /// ルート(AvatarRoot)から見たTransformの相対パスを作成します。
        /// </summary>
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

        /// <summary>
        /// bool配列にtrueが含まれているか判定します。
        /// </summary>
        private static bool HasAnyTrue(bool[] values)
        {
            if (values == null)
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// BlendShape名からインデックスを解決します（完全一致）。
        /// </summary>
        private static int FindBlendShapeIndexByName(Mesh mesh, string targetName)
        {
            if (mesh == null || string.IsNullOrEmpty(targetName))
            {
                return -1;
            }

            int blendShapeCount = mesh.blendShapeCount;
            for (int i = 0; i < blendShapeCount; i++)
            {
                if (mesh.GetBlendShapeName(i) == targetName)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// BlendShapeの参照インデックスを解決します。
        /// 名前が保存されていれば名前優先、無ければpopup index(1-based)から解決します。
        /// 未選択または解決不能な場合は-1を返します。
        /// </summary>
        private static int ResolveBlendShapeIndex(Mesh mesh, string[] blendShapeNames, int[] blendShapePopupIndices, int mappingIndex)
        {
            if (mesh == null)
            {
                return -1;
            }

            // 名前優先
            if (blendShapeNames != null && mappingIndex >= 0 && mappingIndex < blendShapeNames.Length)
            {
                string storedName = blendShapeNames[mappingIndex];
                if (!string.IsNullOrEmpty(storedName))
                {
                    int resolvedIndex = FindBlendShapeIndexByName(mesh, storedName);
                    return resolvedIndex;
                }
            }

            // 互換: popup index(1-based, 0=="----")
            if (blendShapePopupIndices == null || mappingIndex < 0 || mappingIndex >= blendShapePopupIndices.Length)
            {
                return -1;
            }

            int popupValue = blendShapePopupIndices[mappingIndex];
            if (popupValue == 0)
            {
                return -1;
            }

            return popupValue - 1;
        }

        /// <summary>
        /// SkinnedMeshRendererに保存されているBlendShapeのウェイトを、BlendShape名で退避します。
        /// BlendShapeのウェイトはindex依存のため、メッシュのBlendShape順序が変わるとズレる問題の対策です。
        /// </summary>
        private static Dictionary<string, float> CaptureBlendShapeWeightsByName(SkinnedMeshRenderer skinnedMeshRenderer, Mesh sourceMesh)
        {
            if (skinnedMeshRenderer == null || sourceMesh == null)
            {
                return new Dictionary<string, float>(0, System.StringComparer.Ordinal);
            }

            int blendShapeCount = sourceMesh.blendShapeCount;
            var weightsByName = new Dictionary<string, float>(blendShapeCount, System.StringComparer.Ordinal);
            for (int i = 0; i < blendShapeCount; i++)
            {
                string shapeName = sourceMesh.GetBlendShapeName(i);
                if (string.IsNullOrEmpty(shapeName))
                {
                    continue;
                }
                weightsByName[shapeName] = skinnedMeshRenderer.GetBlendShapeWeight(i);
            }
            return weightsByName;
        }

        /// <summary>
        /// 新しいメッシュ適用後、退避していた「名前→ウェイト」をSkinnedMeshRendererへ復元します。
        /// </summary>
        private static void RestoreBlendShapeWeightsByName(SkinnedMeshRenderer skinnedMeshRenderer, Mesh targetMesh, Dictionary<string, float> weightsByName)
        {
            if (skinnedMeshRenderer == null || targetMesh == null || weightsByName == null)
            {
                return;
            }

            int blendShapeCount = targetMesh.blendShapeCount;
            for (int i = 0; i < blendShapeCount; i++)
            {
                string shapeName = targetMesh.GetBlendShapeName(i);
                if (!string.IsNullOrEmpty(shapeName) && weightsByName.TryGetValue(shapeName, out float weight))
                {
                    skinnedMeshRenderer.SetBlendShapeWeight(i, weight);
                }
                else
                {
                    // 旧indexの残留ウェイトが別名へ誤適用されるのを防ぐ
                    skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
                }
            }
        }

        /// <summary>
        /// 元メッシュのBlendShapeフレームを参照し、生成先メッシュへ新規BlendShapeとして追加します。
        /// Override等で生成先メッシュのBlendShape順序が変わっても、参照元(index)がズレないようにするため
        /// フレーム取得は常に参照元メッシュ(sourceMesh)から行います。
        /// </summary>
        private static void AddBlendShape4MMD(Mesh targetMesh, Mesh sourceMesh, string newBlendShapeName, int origBlendShapeIndex, float power)
        {
            if (power == 0f)
            {
                Debug.Log($"[MMDSetup] Skip Create BlendShape '{newBlendShapeName}'(Zero Wight).");
                return;
            }

            if (targetMesh == null || sourceMesh == null)
            {
                Debug.LogWarning($"[MMDSetup] Skip Create BlendShape '{newBlendShapeName}'(Mesh is Null).");
                return;
            }

            if (targetMesh.vertexCount != sourceMesh.vertexCount)
            {
                Debug.LogWarning($"[MMDSetup] Skip Create BlendShape '{newBlendShapeName}'(Vertex Count Mismatch).");
                return;
            }

            int vertexCount = targetMesh.vertexCount;

            // 注意: AddBlendShapeFrame に渡す配列は毎回新規にする。
            // Unity内部実装の差異により、使い回し配列だと後続処理で内容が上書きされ
            // 生成済みBlendShapeが汚染される可能性がある。
            Vector3[] deltaVertices = new Vector3[vertexCount];
            Vector3[] deltaNormals = new Vector3[vertexCount];
            Vector3[] deltaTangents = new Vector3[vertexCount];
            sourceMesh.GetBlendShapeFrameVertices(origBlendShapeIndex, 0, deltaVertices, deltaNormals, deltaTangents);

            for (int i = 0; i < vertexCount; i++)
            {
                deltaVertices[i] *= power; // power%のウェイトを適用
                deltaNormals[i] *= power;
                deltaTangents[i] *= power;
            }
            targetMesh.AddBlendShapeFrame(newBlendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
        }

        /// <summary>
        /// 元メッシュの2つのBlendShapeフレームを参照し、指定ウェイトで加算合成した結果を生成先メッシュへ追加します。
        /// </summary>
        private static void BlendingBlendShape4MMD(Mesh targetMesh, Mesh sourceMesh, string newBlendShapeName, int origBlendShapeIndex1, float power1, int origBlendShapeIndex2, float power2)
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

            if (targetMesh == null || sourceMesh == null)
            {
                Debug.LogWarning($"[MMDSetup] Skip Create BlendShape '{newBlendShapeName}'(Mesh is Null).");
                return;
            }

            if (targetMesh.vertexCount != sourceMesh.vertexCount)
            {
                Debug.LogWarning($"[MMDSetup] Skip Create BlendShape '{newBlendShapeName}'(Vertex Count Mismatch).");
                return;
            }

            int vertexCount = targetMesh.vertexCount;

            Vector3[] deltaVerticesA = new Vector3[vertexCount];
            Vector3[] deltaNormalsA = new Vector3[vertexCount];
            Vector3[] deltaTangentsA = new Vector3[vertexCount];
            sourceMesh.GetBlendShapeFrameVertices(origBlendShapeIndex1, 0, deltaVerticesA, deltaNormalsA, deltaTangentsA);

            Vector3[] deltaVerticesB = new Vector3[vertexCount];
            Vector3[] deltaNormalsB = new Vector3[vertexCount];
            Vector3[] deltaTangentsB = new Vector3[vertexCount];
            sourceMesh.GetBlendShapeFrameVertices(origBlendShapeIndex2, 0, deltaVerticesB, deltaNormalsB, deltaTangentsB);

            // 加算合成
            for (int i = 0; i < vertexCount; i++)
            {
                deltaVerticesA[i] = deltaVerticesA[i] * power1 + deltaVerticesB[i] * power2;
                deltaNormalsA[i] = deltaNormalsA[i] * power1 + deltaNormalsB[i] * power2;
                deltaTangentsA[i] = deltaTangentsA[i] * power1 + deltaTangentsB[i] * power2;
            }
            targetMesh.AddBlendShapeFrame(newBlendShapeName, 100f, deltaVerticesA, deltaNormalsA, deltaTangentsA);
        }
    }
}
