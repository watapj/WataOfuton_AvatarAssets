using UnityEditor;
using UnityEngine;
using Anatawa12.AvatarOptimizer.API;

namespace WataOfuton.Tools.RemoveMeshByBlendshape.Editor
{
    [CustomEditor(typeof(RemoveMeshByBlendshape))]
    public class RemoveMeshByBlendshapeEditor : UnityEditor.Editor
    {
        RemoveMeshByBlendshape comp;
        SerializedProperty _target;
        SerializedProperty _weightThreshold;
        SerializedProperty _isSelected;
        private SkinnedMeshRenderer smr;
        private Mesh mesh;
        private bool isDisplayBlendShapes;


        void OnEnable()
        {
            comp = target as RemoveMeshByBlendshape;
            _target = serializedObject.FindProperty(nameof(RemoveMeshByBlendshape._target));
            _weightThreshold = serializedObject.FindProperty(nameof(RemoveMeshByBlendshape._weightThreshold));
            _isSelected = serializedObject.FindProperty(nameof(RemoveMeshByBlendshape._isSelected));
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            serializedObject.Update();

            // if (GUILayout.Button("Remove Mesh"))
            // {
            //     comp.TryRemoveMeshByBlendshape();
            // }
            // EditorGUILayout.Space();

            // EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_target);
            // if (EditorGUI.EndChangeCheck())
            // {
            comp.GetSMR();
            smr = (SkinnedMeshRenderer)_target.objectReferenceValue;
            mesh = smr.sharedMesh;
            _isSelected.arraySize = mesh.blendShapeCount;
            // }

            _weightThreshold.floatValue = EditorGUILayout.Slider("Weight Threshould", _weightThreshold.floatValue, 0f, 100f);

            EditorGUILayout.Space();

            isDisplayBlendShapes = EditorGUILayout.Foldout(isDisplayBlendShapes, "BlendShapes");
            if (isDisplayBlendShapes)
            {
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    var element = _isSelected.GetArrayElementAtIndex(i);
                    element.boolValue = EditorGUILayout.ToggleLeft(mesh.GetBlendShapeName(i), element.boolValue);
                }
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

    [ComponentInformation(typeof(RemoveMeshByBlendshape))]
    internal class RemoveMeshByBlendshapeInformation : ComponentInformation<RemoveMeshByBlendshape>
    {
        protected override void CollectMutations(RemoveMeshByBlendshape component, ComponentMutationsCollector collector)
        {
            // call methods on the collector to tell about the component
        }

        protected override void CollectDependency(RemoveMeshByBlendshape component, ComponentDependencyCollector collector)
        {
            // call methods on the collector to tell about the component
        }
    }

#endif
}
