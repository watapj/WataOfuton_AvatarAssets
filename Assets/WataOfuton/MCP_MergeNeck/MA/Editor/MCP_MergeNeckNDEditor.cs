using UnityEditor;
using UnityEngine;
#if AVATAR_OPTIMIZER
using Anatawa12.AvatarOptimizer.API;
#endif

namespace WataOfuton.Tools.MCP_MergeNeck.Editor
{
    [CustomEditor(typeof(MCP_MergeNeckND))]
    [CanEditMultipleObjects]
    public class MCP_MergeNeckNDEditor : UnityEditor.Editor
    {
        MCP_MergeNeckND comp;
        SerializedProperty _targetFaceRenderer;
        SerializedProperty _targetBodyRenderer;
        SerializedProperty _triangleDiffDataAll;

        void OnEnable()
        {
            comp = target as MCP_MergeNeckND;
            _targetFaceRenderer = serializedObject.FindProperty(nameof(MCP_MergeNeckND._targetFaceRenderer));
            _targetBodyRenderer = serializedObject.FindProperty(nameof(MCP_MergeNeckND._targetBodyRenderer));
            _triangleDiffDataAll = serializedObject.FindProperty(nameof(MCP_MergeNeckND._triangleDiffDataAll));
        }

        public override void OnInspectorGUI()
        {
            // EditorGUI.BeginDisabledGroup(true);
            // EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            // EditorGUI.EndDisabledGroup();

            serializedObject.Update();

            GUILayout.Label("ますきゃの首の繋ぎ目を整えるツール (NDMF版)", EditorStyles.boldLabel);

            if (ApplyTriangleDiffDataAllWindow.isShowUpdateMessage)
            {
                using (new EditorGUILayout.VerticalScope("HelpBox", GUILayout.ExpandWidth(true)))
                {
                    EditorGUILayout.HelpBox(
                                    $"新しいバージョン {ApplyTriangleDiffDataAllWindow.versionInfo.version} が利用可能です！ 詳細は Booth をご確認ください.",
                                    MessageType.Info);

                    if (GUILayout.Button("Open Booth"))
                    {
                        Application.OpenURL(ApplyTriangleDiffDataAllWindow.versionInfo.releaseURL);
                    }
                }
                EditorGUILayout.Space();
            }

            EditorGUILayout.HelpBox(
                "差分データを適用したい顔と体のオブジェクト及び TriangleDiffDataAll.asset を設定してください."
                ,
                MessageType.Info);

            EditorGUILayout.PropertyField(_targetFaceRenderer, new GUIContent("Target Face Renderer"));
            EditorGUILayout.PropertyField(_targetBodyRenderer, new GUIContent("Target Body Renderer"));
            EditorGUILayout.PropertyField(_triangleDiffDataAll, new GUIContent("Triangle Diff Data All"));

            // if (GUILayout.Button("Preview Merge Neck"))
            // {
            //     comp.TryApplyDiffDataNDMF();
            // }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(comp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    // AAO に登録だけして特に何もしない.
    // https://vpm.anatawa12.com/avatar-optimizer/ja/docs/developers/make-your-components-compatible-with-aao/
#if AVATAR_OPTIMIZER && UNITY_EDITOR

    [ComponentInformation(typeof(MCP_MergeNeckND))]
    internal class MCP_MergeNeckNDInformation : ComponentInformation<MCP_MergeNeckND>
    {
        protected override void CollectMutations(MCP_MergeNeckND component, ComponentMutationsCollector collector)
        {
            // call methods on the collector to tell about the component
        }

        protected override void CollectDependency(MCP_MergeNeckND component, ComponentDependencyCollector collector)
        {
            // call methods on the collector to tell about the component
        }
    }

#endif
}
