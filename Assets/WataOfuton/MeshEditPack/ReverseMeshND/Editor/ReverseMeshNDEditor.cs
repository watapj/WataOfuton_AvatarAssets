using UnityEditor;
using UnityEngine;
#if AVATAR_OPTIMIZER
using Anatawa12.AvatarOptimizer.API;
#endif

namespace WataOfuton.Tools.ReverseMeshND.Editor
{
    [CustomEditor(typeof(ReverseMeshND))]
    [CanEditMultipleObjects]
    public class ReverseMeshNDEditor : UnityEditor.Editor
    {
        ReverseMeshND comp;
        SerializedProperty _isReversed;
        SerializedProperty _origMesh;

        void OnEnable()
        {
            comp = target as ReverseMeshND;
            _isReversed = serializedObject.FindProperty(nameof(ReverseMeshND._isReversed));
            _origMesh = serializedObject.FindProperty(nameof(ReverseMeshND._origMesh));
        }

        public override void OnInspectorGUI()
        {
            // EditorGUI.BeginDisabledGroup(true);
            // EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            // EditorGUI.EndDisabledGroup();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_isReversed, new GUIContent("Reversed."));
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Preview Reverse Mesh"))
            {
                comp.TryReverseMeshND();
            }

            var text = "プレビュー後は 'Ctrl + Z '(Undo) で元に戻せます. \n"
                     + "NDMF を使って Build 時に動作します.";
            EditorGUILayout.HelpBox(text, MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("Save Mesh"))
            {
                comp.SaveMesh();
            }

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
