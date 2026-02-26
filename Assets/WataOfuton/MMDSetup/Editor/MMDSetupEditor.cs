using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
#if AVATAR_OPTIMIZER
using Anatawa12.AvatarOptimizer.API;
#endif

namespace WataOfuton.Tools.MMDSetup.Editor
{
    [CustomEditor(typeof(MMDSetup))]
    public class MMDSetupEditor : UnityEditor.Editor
    {
        MMDSetup MMDSetup;
        SerializedProperty faceMesh;
        SerializedProperty bodyMeshes;
        SerializedProperty enableGenerateBS;
        SerializedProperty blendShapeIndices1;
        SerializedProperty blendShapeNames1;
        SerializedProperty blendShapePowers1;
        SerializedProperty enableBlendBS;
        SerializedProperty blendShapeIndices2;
        SerializedProperty blendShapeNames2;
        SerializedProperty blendShapePowers2;
        SerializedProperty enableOverrideBS;
        private static bool isShowUpdateMessage;
        private static CheckForUpdate.VersionInfo versionInfo;

        void OnEnable()
        {
            MMDSetup = target as MMDSetup;
            faceMesh = serializedObject.FindProperty(nameof(MMDSetup.faceMesh));
            bodyMeshes = serializedObject.FindProperty(nameof(MMDSetup.bodyMeshes));
            enableGenerateBS = serializedObject.FindProperty(nameof(MMDSetup.enableGenerateBS));
            blendShapeIndices1 = serializedObject.FindProperty(nameof(MMDSetup.blendShapeIndices1));
            blendShapeNames1 = serializedObject.FindProperty(nameof(MMDSetup.blendShapeNames1));
            blendShapePowers1 = serializedObject.FindProperty(nameof(MMDSetup.blendShapePowers1));
            enableBlendBS = serializedObject.FindProperty(nameof(MMDSetup.enableBlendBS));
            blendShapeIndices2 = serializedObject.FindProperty(nameof(MMDSetup.blendShapeIndices2));
            blendShapeNames2 = serializedObject.FindProperty(nameof(MMDSetup.blendShapeNames2));
            blendShapePowers2 = serializedObject.FindProperty(nameof(MMDSetup.blendShapePowers2));
            enableOverrideBS = serializedObject.FindProperty(nameof(MMDSetup.enableOverrideBS));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (isShowUpdateMessage)
            {
                using (new EditorGUILayout.VerticalScope("HelpBox", GUILayout.ExpandWidth(true)))
                {
                    EditorGUILayout.HelpBox($"新しいバージョン {versionInfo.version} が利用可能です！ 詳細は Booth をご確認ください.", MessageType.Info);

                    if (GUILayout.Button("Open Booth"))
                    {
                        Application.OpenURL(versionInfo.releaseURL);
                    }
                }
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Re Setting."))
            {
                faceMesh.objectReferenceValue = null;
            }
            EditorGUILayout.PropertyField(faceMesh);
            EditorGUILayout.PropertyField(bodyMeshes);

            if (faceMesh.objectReferenceValue == null)
            {
                faceMesh.objectReferenceValue = null;
                bodyMeshes.arraySize = 0;

                Transform AvatarRoot = FindRootWithDescriptor(MMDSetup.transform);

                List<Transform> bodies = MMDSetupPlugin.FindDeepChildren(AvatarRoot, "Face");
                bodies.AddRange(MMDSetupPlugin.FindDeepChildren(AvatarRoot, "Body"));

                bodyMeshes.arraySize = bodies.Count;

                if (bodies.Count > 0)
                {
                    bool isGetFace = false;
                    var faceBSCheckList = MMDSetupPlugin.blendShapeMappingsFace;
                    for (int i = 0; i < bodies.Count; i++)
                    {
                        SerializedProperty bodyProperty = bodyMeshes.GetArrayElementAtIndex(i);
                        bodyProperty.objectReferenceValue = bodies[i];

                        var smr = bodies[i].GetComponent<SkinnedMeshRenderer>();
                        if (smr != null)
                        {
                            Mesh mesh = smr.sharedMesh;
                            for (int j = 0; j < faceBSCheckList.Length; j++)
                            {
                                if (string.Equals(bodies[i].name, "Face", System.StringComparison.OrdinalIgnoreCase)
                                    || MMDSetupPlugin.BlendShapeExists(mesh, faceBSCheckList[j], false))
                                {
                                    // 頭メッシュと判断
                                    faceMesh.objectReferenceValue = bodies[i];
                                    isGetFace = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (isGetFace == false)
                    {
                        var text = "顔メッシュを自動検索できませんでした.\n"
                                 + "手動で顔メッシュをアタッチしてください.";
                        EditorGUILayout.HelpBox(text, MessageType.Warning);
                    }
                }
                else
                {
                    var text = "顔メッシュを自動検索できませんでした.\n"
                             + "手動で顔メッシュをアタッチしてください.";
                    EditorGUILayout.HelpBox(text, MessageType.Warning);
                }
            }

            var faceT = (Transform)faceMesh.objectReferenceValue;
            if (faceT != null)
            {
                var faceSMR = faceT.GetComponent<SkinnedMeshRenderer>();
                if (faceSMR != null)
                {
                    BlendShapeMapping(faceSMR);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }


        /// <summary>
        /// 親方向へ辿り、VRCAvatarDescriptorを持つTransformをアバタールートとして返します。
        /// </summary>
        Transform FindRootWithDescriptor(Transform current)
        {
            while (current.parent != null)
            {
                if (current.parent.GetComponent<VRCAvatarDescriptor>() != null)
                {
                    return current.parent;
                }
                current = current.parent;
            }
            return null;
        }

        /// <summary>
        /// MMD用BlendShapeマッピングのUI（Popup/Slider/Override/Blend）を描画します。
        /// </summary>
        private void BlendShapeMapping(SkinnedMeshRenderer face)
        {
            string[] mappinglist = BlendShapeMappings.blendShapeMappings4MMD;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            enableGenerateBS.boolValue = EditorGUILayout.Toggle("", enableGenerateBS.boolValue, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            EditorGUILayout.LabelField("Generate BlendShape for MMD from Original BlendShape");
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck() || (blendShapeIndices1.arraySize != mappinglist.Length))
            {
                if (enableGenerateBS.boolValue)
                {
                    SetArrays(mappinglist.Length);
                }
            }

            EditorGUILayout.Space(5);
            if (enableGenerateBS.boolValue)
            {
                Mesh mesh = face.sharedMesh;
                if (mesh == null) return;

                int blendShapeCount = mesh.blendShapeCount;

                if (blendShapeCount == 0)
                {
                    var text = "This mesh does not contain any BlendShapes.";
                    EditorGUILayout.HelpBox(text, MessageType.Info);
                    return;
                }

                // 先頭に「----」を入れる
                string[] blendShapeList = new string[blendShapeCount + 1];
                blendShapeList[0] = "----";
                for (int i = 0; i < blendShapeCount; i++)
                {
                    blendShapeList[i + 1] = mesh.GetBlendShapeName(i);
                }

                for (int i = 0; i < mappinglist.Length; i++)
                {
                    SerializedProperty isoverrideArrayProperty = enableOverrideBS.GetArrayElementAtIndex(i);

                    // 既に同名シェイプキーがある場合
                    if (MMDSetupPlugin.BlendShapeExists(mesh, mappinglist[i], true) && !isoverrideArrayProperty.boolValue)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(mappinglist[i], GUILayout.Width(100));
                        EditorGUILayout.LabelField("This BlendShape already exists.   Override ? ->", GUILayout.Width(270));
                        // 生成用の index は 0 にしてスキップ扱い（ユーザーがoverrideしない限り）
                        blendShapeIndices1.GetArrayElementAtIndex(i).intValue = 0;
                        GUILayout.FlexibleSpace();
                        isoverrideArrayProperty.boolValue = EditorGUILayout.Toggle(isoverrideArrayProperty.boolValue, GUILayout.Width(20));
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUIUtility.labelWidth = 100;
                        SerializedProperty indexProperty = blendShapeIndices1.GetArrayElementAtIndex(i);
                        if (indexProperty.intValue < 0)
                        {
                            indexProperty.intValue = 0;
                        }

                        // 保存しているBlendShape名があれば、現在のメッシュからpopup indexを逆算して追従する
                        SyncPopupIndexFromStoredName(mesh, blendShapeNames1, indexProperty, i);

                        indexProperty.intValue = EditorGUILayout.Popup(mappinglist[i], indexProperty.intValue, blendShapeList, GUILayout.Width(200));

                        // popup選択に合わせてBlendShape名を保存する
                        SyncStoredNameFromPopupSelection(mesh, blendShapeNames1, indexProperty.intValue, i);

                        SerializedProperty powerProperty = blendShapePowers1.GetArrayElementAtIndex(i);
                        powerProperty.floatValue = EditorGUILayout.Slider(powerProperty.floatValue, -100, 100);
                        EditorGUIUtility.labelWidth = 0;

                        // override フラグ表示
                        if (isoverrideArrayProperty.boolValue)
                        {
                            isoverrideArrayProperty.boolValue = EditorGUILayout.Toggle(isoverrideArrayProperty.boolValue, GUILayout.Width(20));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("", GUILayout.Width(20));
                        }
                        EditorGUILayout.EndHorizontal();

                        // Blend のオンオフ
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(" ", GUILayout.Width(10));
                        SerializedProperty boolArrayProperty = enableBlendBS.GetArrayElementAtIndex(i);
                        boolArrayProperty.boolValue = EditorGUILayout.Toggle(boolArrayProperty.boolValue, GUILayout.Width(20));
                        if (boolArrayProperty.boolValue)
                        {
                            EditorGUIUtility.labelWidth = 64;
                            SerializedProperty indexProperty2 = blendShapeIndices2.GetArrayElementAtIndex(i);
                            if (indexProperty2.intValue < 0)
                            {
                                indexProperty2.intValue = 0;
                            }

                            // 2番目も保存名から追従
                            SyncPopupIndexFromStoredName(mesh, blendShapeNames2, indexProperty2, i);

                            indexProperty2.intValue = EditorGUILayout.Popup(" ", indexProperty2.intValue, blendShapeList, GUILayout.Width(164));

                            // 2番目もpopup選択に合わせて保存
                            SyncStoredNameFromPopupSelection(mesh, blendShapeNames2, indexProperty2.intValue, i);

                            SerializedProperty powerProperty2 = blendShapePowers2.GetArrayElementAtIndex(i);
                            powerProperty2.floatValue = EditorGUILayout.Slider(powerProperty2.floatValue, -100, 100);
                            EditorGUILayout.LabelField("", GUILayout.Width(20));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Blend Another BlendShapes.");
                        }
                        EditorGUIUtility.labelWidth = 0;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.Space(5);
                }
            }
        }

        /// <summary>
        /// 設定配列（選択/名前/ウェイト/フラグ）をマッピング定義の長さに合わせて確保します。
        /// </summary>
        private void SetArrays(int length)
        {
            blendShapeIndices1.arraySize = length;
            blendShapeNames1.arraySize = length;
            blendShapePowers1.arraySize = length;
            blendShapeIndices2.arraySize = length;
            blendShapeNames2.arraySize = length;
            blendShapePowers2.arraySize = length;
            enableBlendBS.arraySize = length;
            enableOverrideBS.arraySize = length;
        }


        /// <summary>
        /// 保存済みのBlendShape名から、現在メッシュ上のpopup index(1-based)へ追従させます。
        /// </summary>
        private static void SyncPopupIndexFromStoredName(Mesh mesh, SerializedProperty nameArrayProperty, SerializedProperty popupIndexProperty, int mappingIndex)
        {
            if (nameArrayProperty == null)
            {
                return;
            }

            if (mappingIndex < 0 || mappingIndex >= nameArrayProperty.arraySize)
            {
                return;
            }

            string storedName = nameArrayProperty.GetArrayElementAtIndex(mappingIndex).stringValue;
            if (string.IsNullOrEmpty(storedName))
            {
                return;
            }

            int realIndex = MMDSetupPlugin.FindBlendShapeIndexByName(mesh, storedName);
            int popupIndex = realIndex >= 0 ? realIndex + 1 : 0;
            popupIndexProperty.intValue = popupIndex;
        }

        /// <summary>
        /// popup選択結果から、対応するBlendShape名を保存します。
        /// </summary>
        private static void SyncStoredNameFromPopupSelection(Mesh mesh, SerializedProperty nameArrayProperty, int popupIndex, int mappingIndex)
        {
            if (nameArrayProperty == null)
            {
                return;
            }

            if (mappingIndex < 0 || mappingIndex >= nameArrayProperty.arraySize)
            {
                return;
            }

            // popupIndex == 0 は "----"
            if (popupIndex <= 0)
            {
                nameArrayProperty.GetArrayElementAtIndex(mappingIndex).stringValue = string.Empty;
                return;
            }

            int realIndex = popupIndex - 1;
            if (mesh == null || realIndex < 0 || realIndex >= mesh.blendShapeCount)
            {
                nameArrayProperty.GetArrayElementAtIndex(mappingIndex).stringValue = string.Empty;
                return;
            }

            nameArrayProperty.GetArrayElementAtIndex(mappingIndex).stringValue = mesh.GetBlendShapeName(realIndex);
        }

        /// <summary>
        /// 更新情報表示の状態をEditor側へ通知します。
        /// </summary>
        public static void CheckForUpdate(CheckForUpdate.VersionInfo info, bool isShow)
        {
            isShowUpdateMessage = isShow;
            versionInfo = info;
        }
    }

    // AAO に登録だけして特に何もしない.
    // https://vpm.anatawa12.com/avatar-optimizer/ja/docs/developers/make-your-components-compatible-with-aao/
#if AVATAR_OPTIMIZER && UNITY_EDITOR

    [ComponentInformation(typeof(MMDSetup))]
    internal class MMDSetupInformation : ComponentInformation<MMDSetup>
    {
        protected override void CollectMutations(MMDSetup component, ComponentMutationsCollector collector)
        {
            // call methods on the collector to tell about the component
        }

        protected override void CollectDependency(MMDSetup component, ComponentDependencyCollector collector)
        {
            // call methods on the collector to tell about the component
        }
    }
#endif
}
