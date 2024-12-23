#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using nadena.dev.modular_avatar.core;

namespace WataOfuton.Tools.ClipCostumeSetting
{
    public class ClipCostumeSetting : MonoBehaviour, IEditorOnly
    {
        [SerializeField] public List<Renderer> _targets;
        [SerializeField] public ModularAvatarMergeAnimator _modularAvatarMergeAnimator;
        private static AnimationClip clipOn, clipOff;
        private AnimatorController animator;
        // [SerializeField] public bool _replaceShader;
        // [SerializeField] public float _EyeDist;
        // [SerializeField] public Texture2D _ClipMask;
        [SerializeField] public string _targetShaderName = "lilToon_ClipCostume";

        public void GenController()
        {
            if (_targets == null || _targets.Count == 0) return;

            animator = Instantiate(_modularAvatarMergeAnimator.animator) as AnimatorController;
            GenAnimations(animator, this.transform.parent.gameObject, _targets);

            _modularAvatarMergeAnimator.animator = animator;
        }

        public void GenAnimations(AnimatorController controller, GameObject avatarRoot, List<Renderer> targets)
        {
            clipOn = FindClipByName(controller, "clipOn");
            clipOff = FindClipByName(controller, "clipOff");

            if (clipOn == null || clipOff == null)
            {
                Debug.LogError("[CC] clipOn or clipOff is not found in the controller.");
                return;
            }

            // 既に_ClipOnカーブが存在するかチェック
            bool clipOnAlreadyModified = IsAlreadyModified(clipOn);
            bool clipOffAlreadyModified = IsAlreadyModified(clipOff);

            if (!clipOnAlreadyModified && !clipOffAlreadyModified)
            {
                // クリップからキーをすべて削除
                ClearAllCurves(clipOn);
                ClearAllCurves(clipOff);
            }

            // 再度カーブ設定
            foreach (var renderer in _targets)
            {
                if (renderer == null) continue;
                string path = GetHierarchyPath(avatarRoot.transform, renderer.transform);
                if (!string.IsNullOrEmpty(path))
                {
                    AddRendererToggleCurve(clipOn, path, true);
                    AddRendererToggleCurve(clipOff, path, false);
                }
            }
        }

        private AnimationClip FindClipByName(AnimatorController ac, string clipName)
        {
            foreach (var clip in ac.animationClips)
            {
                if (clip.name == clipName)
                {
                    return clip;
                }
            }
            return null;
        }

        private static void ClearAllCurves(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                clip.SetCurve(binding.path, binding.type, binding.propertyName, null);
            }
        }

        private void AddRendererToggleCurve(AnimationClip clip, string path, bool isOn)
        {
            // _ClipOnを0/1でトグルするカーブ
            var curve = new AnimationCurve();
            float value = isOn ? 1f : 0f;
            curve.AddKey(new Keyframe(0f, value));
            curve.AddKey(new Keyframe(1f / clip.frameRate, value));

            // material._ClipOn というプロパティ名でアニメーションを設定
            clip.SetCurve(path, typeof(Renderer), "material._ClipOn", curve);
        }

        private string GetHierarchyPath(Transform root, Transform target)
        {
            if (target == root) return "";

            List<string> pathSegments = new List<string>();
            Transform current = target;

            // ルートまでのパスを再構築
            while (current != null && current != root)
            {
                pathSegments.Insert(0, current.name);
                current = current.parent;
            }

            // ルートに到達しなければnullを返す
            if (current != root) return null;

            return string.Join("/", pathSegments);
        }

        private bool IsAlreadyModified(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                // すでにmaterial._ClipOnプロパティが設定されていれば、変更済みと判断
                if (binding.propertyName == "material._ClipOn")
                {
                    return true;
                }
            }
            return false;
        }

        public static void ClearAllCurvesOnQuit()
        {
            if (clipOn == null || clipOff == null)
            {
                Debug.LogError("[CC] clipOn or clipOff is not found in the controller.");
                return;
            }

            ClearAllCurves(clipOn);
            ClearAllCurves(clipOff);
        }



        public void CollectRenderers()
        {
            _targets = CollectRenderersWithShader();
        }
        private List<Renderer> CollectRenderersWithShader()
        {
            List<Renderer> result = new List<Renderer>();

            // 親をたどって VRCAvatarDescriptor を持つオブジェクトを探す
            GameObject root = FindRootWithVRCAvatarDescriptor();
            if (root == null)
            {
                Debug.LogError("VRCAvatarDescriptor not found in parent hierarchy.");
                return result;
            }

            // ルートの子階層以下を探索
            CollectFromHierarchy(root.transform, result);

            return result;
        }
        private GameObject FindRootWithVRCAvatarDescriptor()
        {
            Transform current = transform;

            while (current != null)
            {
                if (current.GetComponent<VRCAvatarDescriptor>() != null)
                {
                    return current.gameObject;
                }
                current = current.parent;
            }

            return null;
        }
        private void CollectFromHierarchy(Transform current, List<Renderer> result)
        {
            // SkinnedMeshRendererがあるかチェック
            var renderer = current.GetComponent<Renderer>();
            if (renderer != null && HasTargetShader(renderer))
            {
                result.Add(renderer);
            }

            // 子階層を再帰的に処理
            foreach (Transform child in current)
            {
                CollectFromHierarchy(child, result);
            }
        }

        private bool HasTargetShader(Renderer renderer)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (material != null && material.shader.name.Contains(_targetShaderName))
                {
                    return true;
                }
            }
            return false;
        }

        public void GetMAMA()
        {
            _modularAvatarMergeAnimator = this.GetComponent<ModularAvatarMergeAnimator>();
        }
    }


    // https://kan-kikuchi.hatenablog.com/entry/playModeStateChanged
    [InitializeOnLoad]
    public static class PlayModeStateChangedExample
    {
        static PlayModeStateChangedExample()
        {
            EditorApplication.playModeStateChanged += OnChangedPlayMode;
        }
        private static void OnChangedPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Debug.Log("[CC] 停止状態になった！");
                ClipCostumeSetting.ClearAllCurvesOnQuit();
            }
        }
    }
}
#endif