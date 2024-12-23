using UnityEditor;
using UnityEngine;
// using Anatawa12.AvatarOptimizer.API;

namespace WataOfuton.Tools.ClipCostumeSetting.Editor
{
    [CustomEditor(typeof(ClipCostumeSetting))]
    public class ClipCostumeSettingEditor : UnityEditor.Editor
    {
        ClipCostumeSetting comp;
        SerializedProperty _targets;
        SerializedProperty _modularAvatarMergeAnimator;
        // SerializedProperty _replaceShader;
        // SerializedProperty _EyeDist;
        // SerializedProperty _ClipMask;
        SerializedProperty _targetShaderName;
        private static bool isShowUpdateMessage;
        private static CheckForUpdate.VersionInfo versionInfo;

        void OnEnable()
        {
            comp = target as ClipCostumeSetting;

            // SerializedObjectのプロパティ取得
            _targets = serializedObject.FindProperty(nameof(comp._targets));
            _modularAvatarMergeAnimator = serializedObject.FindProperty(nameof(comp._modularAvatarMergeAnimator));
            // _replaceShader = serializedObject.FindProperty(nameof(comp._replaceShader));
            // _EyeDist = serializedObject.FindProperty(nameof(comp._EyeDist));
            // _ClipMask = serializedObject.FindProperty(nameof(comp._ClipMask));
            _targetShaderName = serializedObject.FindProperty(nameof(comp._targetShaderName));

            comp.GetMAMA();
        }

        public override void OnInspectorGUI()
        {
            // EditorGUI.BeginDisabledGroup(true);
            // EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            // EditorGUI.EndDisabledGroup();

            serializedObject.Update();

            if (isShowUpdateMessage)
            {
                using (new EditorGUILayout.VerticalScope("HelpBox", GUILayout.ExpandWidth(true)))
                {
                    EditorGUILayout.HelpBox(
                                    $"新しいバージョン {versionInfo.version} が利用可能です！ 詳細は Booth をご確認ください.",
                                    MessageType.Info);

                    if (GUILayout.Button("Open Booth"))
                    {
                        Application.OpenURL(versionInfo.releaseURL);
                    }
                }
                EditorGUILayout.Space();
            }
            // if (GUILayout.Button("Generate Animation Test"))
            // {
            //     comp.GenController();
            // }
            // EditorGUI.BeginDisabledGroup(true);
            // EditorGUILayout.ObjectField("clipOn", comp.clipOn, typeof(AnimationClip), true);
            // EditorGUILayout.ObjectField("clipOff", comp.clipOff, typeof(AnimationClip), true);
            // EditorGUI.EndDisabledGroup();
            // EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_targetShaderName, new GUIContent("Target Shader Name"), true);
            if (GUILayout.Button("Collect Renderers With CC Shader"))
            {
                comp.CollectRenderers();
            }
            EditorGUILayout.PropertyField(_targets, new GUIContent("Targets"), true);
            EditorGUILayout.PropertyField(_modularAvatarMergeAnimator, new GUIContent("Merge Animator"), true);
            if (_modularAvatarMergeAnimator.objectReferenceValue == null)
            {
                comp.GetMAMA();
            }
            // EditorGUILayout.PropertyField(_replaceShader, new GUIContent("Replace Shader"), true);
            // if (_replaceShader.boolValue)
            // {
            //     EditorGUILayout.PropertyField(_EyeDist, new GUIContent("_EyeDist"), true);
            //     EditorGUILayout.PropertyField(_ClipMask, new GUIContent("_ClipMask"), true);
            // }

            serializedObject.ApplyModifiedProperties();
        }

        public static void CheckForUpdate(CheckForUpdate.VersionInfo info, bool isShow)
        {
            isShowUpdateMessage = isShow;
            versionInfo = info;
        }

    }

    /*
    // AAO に登録だけして特に何もしない.
    // https://vpm.anatawa12.com/avatar-optimizer/ja/docs/developers/make-your-components-compatible-with-aao/
#if AVATAR_OPTIMIZER && UNITY_EDITOR
    [ComponentInformation(typeof(ClipCostumeSetting))]
    internal class ReverseMeshNDInformation : ComponentInformation<ClipCostumeSetting>
    {
        protected override void CollectMutations(ClipCostumeSetting component, ComponentMutationsCollector collector)
        {
            // call methods on the collector to tell about the component
        }

        protected override void CollectDependency(ClipCostumeSetting component, ComponentDependencyCollector collector)
        {
            // call methods on the collector to tell about the component
        }
    }
#endif
    */
}
