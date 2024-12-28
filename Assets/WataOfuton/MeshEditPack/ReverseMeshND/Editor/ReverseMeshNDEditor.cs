using UnityEditor;
using UnityEngine;
using Anatawa12.AvatarOptimizer.API;

namespace WataOfuton.Tools.ReverseMeshND.Editor
{
    [CustomEditor(typeof(ReverseMeshND))]
    public class ReverseMeshNDEditor : UnityEditor.Editor
    {
        ReverseMeshND comp;
        SerializedProperty _isReversed;

        void OnEnable()
        {
            comp = target as ReverseMeshND;
            _isReversed = serializedObject.FindProperty(nameof(ReverseMeshND._isReversed));
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_isReversed, new GUIContent("Reversed."));
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Preview Reverse Mesh"))
            {
                comp.TryReverseMeshND();
            }

            var text = "プレビュー後は 'Ctrl + Z' で元に戻してください！ \n"
                     + "NDMF を使って Build 時に動作します.";
            EditorGUILayout.HelpBox(text, MessageType.Info);
            EditorGUILayout.Space();

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

    [ComponentInformation(typeof(ReverseMeshND))]
    internal class ReverseMeshNDInformation : ComponentInformation<ReverseMeshND>
    {
        protected override void CollectMutations(ReverseMeshND component, ComponentMutationsCollector collector)
        {
            // call methods on the collector to tell about the component
        }

        protected override void CollectDependency(ReverseMeshND component, ComponentDependencyCollector collector)
        {
            // call methods on the collector to tell about the component
        }
    }

#endif
}
