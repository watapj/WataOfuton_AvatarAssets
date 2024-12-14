using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;

namespace WataOfuton.Tools.MMDSetup.Editor
{
    [CustomEditor(typeof(MMDSetup))]
    public class MMDSetupEditor : UnityEditor.Editor
    {
        MMDSetup MMDSetup;
        SerializedProperty faceMesh;
        SerializedProperty bodyMeshes;
#if UNITY_2022_3_OR_NEWER
        SerializedProperty enableGenerateBS;
        SerializedProperty blendShapeIndices1;
        SerializedProperty blendShapePowers1;
        SerializedProperty enableBlendBS;
        SerializedProperty blendShapeIndices2;
        SerializedProperty blendShapePowers2;
        SerializedProperty enableOverrideBS;
#endif

        void OnEnable()
        {
            MMDSetup = target as MMDSetup;
            faceMesh = serializedObject.FindProperty(nameof(MMDSetup.faceMesh));
            bodyMeshes = serializedObject.FindProperty(nameof(MMDSetup.bodyMeshes));
#if UNITY_2022_3_OR_NEWER
            enableGenerateBS = serializedObject.FindProperty(nameof(MMDSetup.enableGenerateBS));
            blendShapeIndices1 = serializedObject.FindProperty(nameof(MMDSetup.blendShapeIndices1));
            blendShapePowers1 = serializedObject.FindProperty(nameof(MMDSetup.blendShapePowers1));
            enableBlendBS = serializedObject.FindProperty(nameof(MMDSetup.enableBlendBS));
            blendShapeIndices2 = serializedObject.FindProperty(nameof(MMDSetup.blendShapeIndices2));
            blendShapePowers2 = serializedObject.FindProperty(nameof(MMDSetup.blendShapePowers2));
            enableOverrideBS = serializedObject.FindProperty(nameof(MMDSetup.enableOverrideBS));
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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

                List<Transform> bodies = FindDeepChildren(AvatarRoot, "Face");
                bodies.AddRange(FindDeepChildren(AvatarRoot, "Body"));

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

#if UNITY_2022_3_OR_NEWER

            var faceT = (Transform)faceMesh.objectReferenceValue;
            if (faceT != null)
            {
                var faceSMR = faceT.GetComponent<SkinnedMeshRenderer>();
                if (faceSMR != null)
                {
                    BlendShapeMappings(faceSMR);
                }
            }
#else
            var text2 = "MMD用BlendShape生成機能は、Unity2022でのみ動作します.";
            EditorGUILayout.HelpBox(text2, MessageType.Info);
#endif

            serializedObject.ApplyModifiedProperties();
        }

        private static List<Transform> FindDeepChildren(Transform parent, string name)
        {
            if (parent == null)
            {
                return new List<Transform>();
            }
            List<Transform> foundChildren = new List<Transform>();
            foreach (Transform child in parent)
            {
                if (child.gameObject.tag == "EditorOnly") continue;
                if (child.gameObject.activeInHierarchy == false) continue;

                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundChildren.Add(child);
                }
                foundChildren.AddRange(FindDeepChildren(child, name));
            }
            return foundChildren;
        }

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

#if UNITY_2022_3_OR_NEWER
        private void BlendShapeMappings(SkinnedMeshRenderer face)
        {
            SerializedProperty boolProperty = enableGenerateBS;
            string[] mappinglist = MMDSetupPlugin.blendShapeMappings4MMD;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            boolProperty.boolValue = EditorGUILayout.Toggle("", boolProperty.boolValue, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            EditorGUILayout.LabelField("Generate BlendShape for MMD from Original BlendShape");
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                if (boolProperty.boolValue)
                {
                    blendShapeIndices1.arraySize = mappinglist.Length;
                    blendShapePowers1.arraySize = mappinglist.Length;
                    blendShapeIndices2.arraySize = mappinglist.Length;
                    blendShapePowers2.arraySize = mappinglist.Length;
                    enableBlendBS.arraySize = mappinglist.Length;
                    enableOverrideBS.arraySize = mappinglist.Length;
                }
            }

            EditorGUILayout.Space(5);
            if (boolProperty.boolValue)
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

                string[] blendShapeList = new string[blendShapeCount + 1];
                blendShapeList[0] = "----";
                for (int i = 0; i < blendShapeCount; i++)
                {
                    blendShapeList[i + 1] = mesh.GetBlendShapeName(i);
                }

                for (int i = 0; i < mappinglist.Length; i++)
                {
                    SerializedProperty isoverrideArrayProperty = enableOverrideBS.GetArrayElementAtIndex(i);
                    if (MMDSetupPlugin.BlendShapeExists(mesh, mappinglist[i], true) && !isoverrideArrayProperty.boolValue)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(mappinglist[i], GUILayout.Width(100));
                        EditorGUILayout.LabelField("This BlendShape already exists.   Override ? ->", GUILayout.Width(270));
                        SerializedProperty indexProperty = blendShapeIndices1.GetArrayElementAtIndex(i);
                        indexProperty.intValue = -1;
                        GUILayout.FlexibleSpace();
                        SerializedProperty boolArrayProperty = enableOverrideBS.GetArrayElementAtIndex(i);
                        boolArrayProperty.boolValue = EditorGUILayout.Toggle(boolArrayProperty.boolValue, GUILayout.Width(20));
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
                        indexProperty.intValue = EditorGUILayout.Popup(mappinglist[i], indexProperty.intValue, blendShapeList, GUILayout.Width(200));
                        SerializedProperty powerProperty = blendShapePowers1.GetArrayElementAtIndex(i);
                        powerProperty.floatValue = EditorGUILayout.Slider(powerProperty.floatValue, -100, 100);
                        EditorGUIUtility.labelWidth = 0;
                        if (isoverrideArrayProperty.boolValue)
                        {
                            isoverrideArrayProperty.boolValue = EditorGUILayout.Toggle(isoverrideArrayProperty.boolValue, GUILayout.Width(20));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("", GUILayout.Width(20));
                        }
                        EditorGUILayout.EndHorizontal();

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
                            indexProperty2.intValue = EditorGUILayout.Popup(" ", indexProperty2.intValue, blendShapeList, GUILayout.Width(164));
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
#endif
    }
}
