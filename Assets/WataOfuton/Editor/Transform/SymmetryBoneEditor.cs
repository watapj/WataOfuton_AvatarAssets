/*
 * This code was generated with the help of ChatGPT, an AI language model developed by OpenAI.
 * Please save this script in a folder named "Editor".
 */

using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace WataOfuton.Tool
{
    [CustomEditor(typeof(Transform))]
    public class SymmetryBoneEditor : Editor
    {
        private Transform _transform;
        private Transform symmetricalBone;
        private bool _symmetryEnabled = false;
        private Vector3 _positionInvertFlags = new Vector3(-1, 1, 1);
        private Vector3 _rotationInvertFlags = new Vector3(1, -1, -1);
        private Vector3 _rotationMirrorFlags = new Vector3(1, 1, 1);
        private bool usedSuffix;
        private string suffix;
        private Vector3 _lastPosition, _lastRotation, _lastScale;
        private Editor symmetryBoneEditor;

        private void OnEnable()
        {
            _transform = (Transform)target;
            symmetryBoneEditor = CreateEditor(_transform, Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.TransformInspector", true));

            _lastPosition = _transform.localPosition;
            _lastRotation = _transform.localEulerAngles;
            _lastScale = _transform.localScale;

            EditorApplication.update += Update;
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;

            if (symmetryBoneEditor != null)
            {
                symmetricalBone = null;
                DestroyImmediate(symmetryBoneEditor);
            }
        }

        private void Update()
        {
            Update_Symmetry();
        }

        private void Update_Symmetry()
        {
            if (_symmetryEnabled)
            {
                if (_transform.localPosition != _lastPosition ||
                    _transform.localEulerAngles != _lastRotation ||
                    _transform.localScale != _lastScale)
                {
                    ApplySymmetry();
                    _lastPosition = _transform.localPosition;
                    _lastRotation = _transform.localEulerAngles;
                    _lastScale = _transform.localScale;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            symmetryBoneEditor.OnInspectorGUI();

            if (EditorPrefs.GetBool(SymmetryBoneEditorWindow.PREF_KEY_ENABLE_AUTO_CHANGE, false))
            {
                symmetryBoneEditorGUICore();
            }
        }

        private void symmetryBoneEditorGUICore()
        {
            EditorGUILayout.Space(20);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Symmetry Bone Editor", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUIUtility.labelWidth = 15;
            _symmetryEnabled = EditorGUILayout.Toggle(_symmetryEnabled);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            if (_symmetryEnabled)
            {
                if (symmetricalBone == null)
                {
                    usedSuffix = false;
                    string symmetricalBoneName = GetSymmetricalBoneName(_transform.name);
                    if (usedSuffix) symmetricalBoneName += suffix;

                    if (symmetricalBoneName == null)
                    {
                        EditorGUILayout.HelpBox("Symmetry bone not found.", MessageType.Info);
                        return;
                    }

                    symmetricalBone = FindTransformRecursively(_transform.parent, symmetricalBoneName);
                    if (symmetricalBone == null)
                    {
                        EditorGUILayout.HelpBox("Symmetry bone not found.", MessageType.Info);
                        return;
                    }
                }

                // Display symmetricalBone in Transform component
                EditorGUILayout.HelpBox("Symmetry is enabled. Changes to this transform will automatically affect its symmetrical counterpart.", MessageType.Info);
                EditorGUILayout.ObjectField("Symmetrical Bone", symmetricalBone, typeof(GameObject), true);
                // EditorGUILayout.TextField("Suffix", suffix);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Invert Position Axes", GUILayout.Width(150));
                EditorGUIUtility.labelWidth = 15;
                _positionInvertFlags.x = EditorGUILayout.Toggle("X", _positionInvertFlags.x == -1) ? -1 : 1;
                _positionInvertFlags.y = EditorGUILayout.Toggle("Y", _positionInvertFlags.y == -1) ? -1 : 1;
                _positionInvertFlags.z = EditorGUILayout.Toggle("Z", _positionInvertFlags.z == -1) ? -1 : 1;
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Invert Rotation Axes", GUILayout.Width(150));
                EditorGUIUtility.labelWidth = 15;
                _rotationInvertFlags.x = EditorGUILayout.Toggle("X", _rotationInvertFlags.x == -1) ? -1 : 1;
                if (_rotationInvertFlags.x == -1) _rotationMirrorFlags.x = 1;
                _rotationInvertFlags.y = EditorGUILayout.Toggle("Y", _rotationInvertFlags.y == -1) ? -1 : 1;
                if (_rotationInvertFlags.y == -1) _rotationMirrorFlags.y = 1;
                _rotationInvertFlags.z = EditorGUILayout.Toggle("Z", _rotationInvertFlags.z == -1) ? -1 : 1;
                if (_rotationInvertFlags.z == -1) _rotationMirrorFlags.z = 1;
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mirror Rotation Mode", GUILayout.Width(150));
                EditorGUIUtility.labelWidth = 15;
                _rotationMirrorFlags.x = EditorGUILayout.Toggle("X", _rotationMirrorFlags.x == -1) ? -1 : 1;
                if (_rotationMirrorFlags.x == -1) _rotationInvertFlags.x = 1;
                _rotationMirrorFlags.y = EditorGUILayout.Toggle("Y", _rotationMirrorFlags.y == -1) ? -1 : 1;
                if (_rotationMirrorFlags.y == -1) _rotationInvertFlags.y = 1;
                _rotationMirrorFlags.z = EditorGUILayout.Toggle("Z", _rotationMirrorFlags.z == -1) ? -1 : 1;
                if (_rotationMirrorFlags.z == -1) _rotationInvertFlags.z = 1;
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck()) ApplySymmetry();
            }
        }

        private void ApplySymmetry()
        {
            if (symmetricalBone == null) return;
            if (_transform == null) return;

            Undo.RecordObject(_transform, "Transform Change Symmetry");
            Undo.RecordObject(symmetricalBone.transform, "Transform Change Symmetry");

            symmetricalBone.localPosition = Vector3.Scale(_transform.localPosition, _positionInvertFlags);
            symmetricalBone.localEulerAngles = Vector3.Scale(_transform.localEulerAngles, _rotationInvertFlags);
            symmetricalBone.localScale = _transform.localScale;

            var r = symmetricalBone.localEulerAngles;
            if (_rotationMirrorFlags.x == -1) r.x = mirrorRot(_transform.localEulerAngles.x);
            if (_rotationMirrorFlags.y == -1) r.y = mirrorRot(_transform.localEulerAngles.y);
            if (_rotationMirrorFlags.z == -1) r.z = mirrorRot(_transform.localEulerAngles.z);
            symmetricalBone.localEulerAngles = r;
        }

        private float mirrorRot(float rot)
        {
            float[] angles = { 90, 270 };
            var min = angles.Min(value => Mathf.Abs(value - rot));
            float closest = angles.First(value => Mathf.Approximately(Mathf.Abs(value - rot), min));

            float diff = Mathf.Abs(closest - rot);
            if (rot < closest)
            {
                return closest + diff;
            }
            else
            {
                return closest - diff;
            }
        }

        private string GetSymmetricalBoneName(string boneName)
        {
            // 数値接尾辞を抽出するための正規表現
            var regex = new System.Text.RegularExpressions.Regex(@"(\.\d+)$");
            var match = regex.Match(boneName);
            string numberSuffix = match.Success ? match.Groups[1].Value : "";

            // 数値接尾辞を除いたベース名を取得
            string baseName = regex.Replace(boneName, "");

            // 対称性を確認するパターン
            string[] patterns = { "_L", "_R", ".L", ".R", " L", " R", "_l", "_r", ".l", ".r", " l", " r" };
            foreach (string pattern in patterns)
            {
                if (baseName.EndsWith(pattern, System.StringComparison.OrdinalIgnoreCase))
                {
                    string oppositePattern = pattern.ToLower().Contains("l") ? pattern.Replace("L", "R").Replace("l", "r") : pattern.Replace("R", "L").Replace("r", "l");
                    return baseName.Substring(0, baseName.Length - pattern.Length) + oppositePattern + numberSuffix;
                }
            }

            if (boneName.Contains("Left"))
            {
                return boneName.Replace("Left", "Right");
            }
            else if (boneName.Contains("Right"))
            {
                return boneName.Replace("Right", "Left");
            }
            else if (boneName.Contains("left"))
            {
                return boneName.Replace("left", "right");
            }
            else if (boneName.Contains("right"))
            {
                return boneName.Replace("right", "left");
            }

            // Suffix を確認するパターン
            if (!usedSuffix)
            {
                suffix = findSuffix(boneName, boneName, patterns);
                if (suffix != null)
                {
                    usedSuffix = true;
                    return GetSymmetricalBoneName(boneName.Substring(0, boneName.Length - suffix.Length));
                }
            }

            return null;
        }

        private string findSuffix(string origname, string baseName, string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                if (baseName.EndsWith(pattern, System.StringComparison.OrdinalIgnoreCase))
                {
                    return origname.Substring(baseName.Length, origname.Length - baseName.Length);
                }
            }

            if (baseName.Length > 1)
            {
                return findSuffix(origname, baseName.Substring(0, baseName.Length - 1), patterns);
            }

            return null;
        }

        private Transform FindTransformRecursively(Transform startTransform, string name, List<Transform> checkedTransforms = null)
        {
            if (checkedTransforms == null)
            {
                checkedTransforms = new List<Transform>();
            }

            if (checkedTransforms.Contains(startTransform))
            {
                return null;
            }

            checkedTransforms.Add(startTransform);

            // Check the current transform for the target name
            foreach (Transform child in startTransform)
            {
                if (child.name.Equals(name))
                {
                    return child;
                }
            }

            // Recursively check the children
            foreach (Transform child in startTransform)
            {
                Transform result = FindTransformRecursively(child, name, checkedTransforms);

                if (result != null)
                {
                    return result;
                }
            }

            // If the target transform was not found in the current transform's hierarchy, go one level up (if possible)
            if (startTransform.parent != null)
            {
                return FindTransformRecursively(startTransform.parent, name, checkedTransforms);
            }

            // If the target transform was not found and there are no more parents to check, return null
            return null;
        }
    }

    public class SymmetryBoneEditorWindow : EditorWindow
    {
        private const string MENU_ITEM_PATH = "Window/WataOfuton/SymmetryBoneEditor";
        public const string PREF_KEY_ENABLE_AUTO_CHANGE = "SymmetryBoneEditorWindow.EnableEditor";

        // 現在の設定をMenuItemに反映
        [MenuItem(MENU_ITEM_PATH, true)]
        public static bool ToggleActionValidation()
        {
            // メニューがチェックされているかどうかを示す
            Menu.SetChecked(MENU_ITEM_PATH, EditorPrefs.GetBool(PREF_KEY_ENABLE_AUTO_CHANGE, true));
            return true;
        }

        // メニューアイテムが選択されたときの挙動を設定
        [MenuItem(MENU_ITEM_PATH, false)]
        public static void ToggleAction()
        {
            // 現在のチェック状態を取得し反転させる
            bool current = EditorPrefs.GetBool(PREF_KEY_ENABLE_AUTO_CHANGE, true);
            EditorPrefs.SetBool(PREF_KEY_ENABLE_AUTO_CHANGE, !current);
        }
    }

}
