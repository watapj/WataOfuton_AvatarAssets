using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimationPathReplacer : EditorWindow
{
    private AnimatorController animController;
    private string targetPath = "Replaced Object Name";   // 置換対象のパス
    private string replacePath = "Original Object Name";      // 置換後のパス

    [MenuItem("Window/WataOfuton/Animation Path Replacer")]
    public static void ShowWindow()
    {
        GetWindow<AnimationPathReplacer>("Animation Path Replacer");
    }

    private void OnGUI()
    {
        // MonoScript script = MonoScript.FromScriptableObject(this);
        // EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        // EditorGUILayout.Space(10);

        GUILayout.Label("Animator Path Replacer", EditorStyles.boldLabel);

        animController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animController, typeof(AnimatorController), false);
        targetPath = EditorGUILayout.TextField("Target Name", targetPath);
        replacePath = EditorGUILayout.TextField("Replace Name", replacePath);

        if (GUILayout.Button("Replace Paths"))
        {
            if (animController != null)
            {
                ReplacePathsInAnimatorController(animController, targetPath, replacePath);

                Debug.Log("Path replacement complete.");
                EditorUtility.SetDirty(animController);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogError("Animator Controller not assigned.");
            }
        }
    }

    private void ReplacePathsInAnimatorController(AnimatorController controller, string targetPath, string replacePath)
    {
        foreach (var layer in controller.layers)
        {
            ProcessStateMachine(layer.stateMachine, targetPath, replacePath);
        }
    }

    private void ProcessStateMachine(AnimatorStateMachine stateMachine, string targetPath, string replacePath)
    {
        foreach (var state in stateMachine.states)
        {
            AnimationClip clip = state.state.motion as AnimationClip;
            if (clip != null)
            {
                ReplacePathsInClip(clip, targetPath, replacePath);
            }
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            ProcessStateMachine(subStateMachine.stateMachine, targetPath, replacePath);
        }
    }

    private void ReplacePathsInClip(AnimationClip clip, string targetPath, string replacePath)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

        foreach (var binding in bindings)
        {
            if (binding.path.Contains(targetPath)) // 部分一致を確認
            {
                string newPath = binding.path.Replace(targetPath, replacePath); // 部分置換を実行

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                AnimationUtility.SetEditorCurve(clip, binding, null); // 古いバインディングを削除

                EditorCurveBinding newBinding = new EditorCurveBinding
                {
                    path = newPath,
                    propertyName = binding.propertyName,
                    type = binding.type
                };

                AnimationUtility.SetEditorCurve(clip, newBinding, curve); // 新しいバインディングを追加
            }
        }
    }
}
