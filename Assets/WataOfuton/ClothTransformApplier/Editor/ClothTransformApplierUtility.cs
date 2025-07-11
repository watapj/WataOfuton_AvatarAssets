#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using nadena.dev.modular_avatar.core.editor;
using UnityEditor.Presets;

namespace WataOfuton.Tools.ClothTransformApplier.Editor
{
    public class ClothTransformApplierUtility : EditorWindow
    {
        private int tab;
        private string[] tabText = new string[2] { "Export", "Convert" };
        private Vector2 scroll = Vector2.zero;
        private string text;
        private GameObject export;
        private GameObject preset;
        private GameObject target;
        private string newPresetName;
        private string[] axisOptions = { "X", "Y", "Z" };
        private int selectedAxisIndex = 1;
        private float multiplier = 1.01f;
        [SerializeField] public List<string> skipComponentList = new List<string>() { "physbone" };
        private bool enableButton;


        //メニューへの登録
        [MenuItem("Window/WataOfuton/ClothTransformApplierUtility")]
        public static void Create()
        {
            //ウインドウ作成
            GetWindow<ClothTransformApplierUtility>("ClothTransformApplierUtility");
        }

        //GUI
        private void OnGUI()
        {
            // スクリプトの表示
            // MonoScript script = MonoScript.FromScriptableObject(this);
            // EditorGUI.BeginDisabledGroup(true);
            // EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            // EditorGUI.EndDisabledGroup();
            GUILayout.Space(10);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            //編集ツール切り替え
            EditorGUI.BeginChangeCheck();
            tab = GUILayout.Toolbar(tab, tabText);

            switch (tab)
            {
                case 0:
                    OnGUI_Export();
                    break;
                case 1:
                    OnGUI_Convert();
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        void OnGUI_Export()
        {
            enableButton = true;
            text = "Preset から不要データを除去したデータを出力します. 他ユーザーと Preset を共有したい場合に使用してください.";
            EditorGUILayout.HelpBox(text, MessageType.Info);

            GUILayout.Space(5);
            export = (GameObject)EditorGUILayout.ObjectField("Export Preset", export, typeof(GameObject), true);
            if (export == null)
            {
                enableButton = false;
                text = "出力する Preset が設定されていません.";
                EditorGUILayout.HelpBox(text, MessageType.Warning);
            }

            GUILayout.Space(5);
            newPresetName = EditorGUILayout.TextField("New Preset Name", newPresetName);
            if (string.IsNullOrEmpty(newPresetName))
            {
                enableButton = false;
            }

            EditorGUILayout.Space(10);
            EditorGUI.BeginDisabledGroup(!enableButton);
            if (GUILayout.Button("Export"))
            {
                ExportPreset();
            }
            EditorGUI.EndDisabledGroup();
        }

        void OnGUI_Convert()
        {
            var so = new SerializedObject(this);
            so.Update();

            enableButton = true;
            text = "アバター A からアバター B へ変換する Preset の差分情報を基に、アバター B からアバター A へ変換する Preset を作成します.";
            text += "\n現在 β 版です.";
            EditorGUILayout.HelpBox(text, MessageType.Info);

            GUILayout.Space(5);
            preset = (GameObject)EditorGUILayout.ObjectField("Preset", preset, typeof(GameObject), true);
            if (preset == null)
            {
                enableButton = false;
                text = "Preset が設定されていません.";
                EditorGUILayout.HelpBox(text, MessageType.Warning);
            }
            else if (PrefabUtility.GetCorrespondingObjectFromSource(preset) == null)
            {
                enableButton = false;
                text = "Preset から差分情報を取得できません.";
                EditorGUILayout.HelpBox(text, MessageType.Warning);
            }

            GUILayout.Space(5);
            target = (GameObject)EditorGUILayout.ObjectField("Target", target, typeof(GameObject), true);
            if (target == null)
            {
                enableButton = false;
                text = "Target が設定されていません.";
                EditorGUILayout.HelpBox(text, MessageType.Warning);
            }

            GUILayout.Space(5);
            selectedAxisIndex = EditorGUILayout.Popup("Select Bone Axis", selectedAxisIndex, axisOptions);

            GUILayout.Space(5);
            multiplier = EditorGUILayout.FloatField("Multiplier", multiplier);

            GUILayout.Space(5);
            EditorGUILayout.PropertyField(so.FindProperty("skipComponentList"), true);

            GUILayout.Space(5);
            newPresetName = EditorGUILayout.TextField("New Preset Name", newPresetName);
            if (string.IsNullOrEmpty(newPresetName))
            {
                enableButton = false;
            }


            EditorGUILayout.Space(10);
            EditorGUI.BeginDisabledGroup(!enableButton); // trueにするとDisableになる。（falseにするといつも通り入力可能になる）
            if (GUILayout.Button("Convert"))
            {
                ConvertPreset();
            }
            EditorGUI.EndDisabledGroup();
            so.ApplyModifiedProperties();
        }

        void ConvertPreset()
        {
            var targetBoneList = ReferenceBoneList.TargetBoneList();
            var boneList = new List<ClothTransformApplier.BoneList>();
            ClothTransformApplier.CollectBones(preset.transform, boneList, targetBoneList, false, "", "");
            if (boneList == null || boneList.Count == 0)
            {
                ClothTransformApplier.CollectBones(preset.transform, boneList, targetBoneList, false, "bone_", "");
                if (boneList == null || boneList.Count == 0)
                {
                    Debug.LogWarning("[Cloth Transform Applier] BoneList is Empty.");
                    return;
                }
            }

            var targetI = Instantiate(target);

            targetI.transform.localPosition = -preset.transform.position;
            targetI.transform.localScale = ClothTransformApplier.DivideVector3(Vector3.one, preset.transform.localScale);

            GetBoneComponents(targetI.transform, boneList, targetBoneList, "bone_", "", preset.transform, targetI.transform);
            SavePrefabAssets(preset, targetI, newPresetName, "Presets");

            DestroyImmediate(targetI);
        }

        private void GetBoneComponents(Transform _parent, List<ClothTransformApplier.BoneList> _boneList, string[][] targetBoneList, string prefix, string suffix,
                                       Transform preset, Transform target)
        {
            if (_parent == null) return;

            string boneName = ClothTransformApplier.TrimAffix(_parent.name, prefix, suffix);

            // 自分自身をチェック
            foreach (var bone in _boneList)
            {
                if (ReferenceBoneList.IsMatchBoneName(boneName, targetBoneList[bone.num]))
                {
                    foreach (var component in bone.components)
                    {
                        CopyComponent(component, _parent, skipComponentList, selectedAxisIndex, multiplier, preset, target);
                    }
                }
            }

            if (_parent.childCount == 0) return;

            // 子オブジェクトを再帰的に処理
            foreach (Transform child in _parent)
            {
                GetBoneComponents(child, _boneList, targetBoneList, prefix, suffix, preset, target);
            }
        }

        /**************************************************************************************************************/
        void ExportPreset()
        {
            var exportI = Instantiate(export);

            var targetBoneList = ReferenceBoneList.TargetBoneListALL();
            // ボーン以外のオブジェクトを削除
            DeleteNonBoneObjects(exportI.transform, targetBoneList, "bone_", "");

            SavePrefabAssets(export, exportI, newPresetName, "Export");
            DestroyImmediate(exportI);
        }

        // ボーン以外のオブジェクトを再帰的に削除するメソッド
        private void DeleteNonBoneObjects(Transform parent, string[][] targetBoneList, string prefix, string suffix)
        {
            if (parent == null) return;
            if (parent.gameObject.CompareTag("EditorOnly")) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                string boneName = ClothTransformApplier.TrimAffix(child.name, prefix, suffix);

                // 子オブジェクトの名前が保持するべきボーン名に含まれていない場合は削除
                int num = ReferenceBoneList.IsMatchBoneNameID(boneName, targetBoneList);
                if (num >= 0)
                {
                    // 含まれている場合はさらに下の階層も探索
                    DeleteNonBoneObjects(child, targetBoneList, prefix, suffix);
                }
                else
                {
                    DeleteNonBoneObjects(child, targetBoneList, prefix, suffix);
                    // Debug.Log($"[Cloth Transform Applier] Delete {child.name}.");
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        /**************************************************************************************************************/
        public static void SavePrefabAssets(GameObject preset, GameObject target, string newPresetName, string dirName)
        {
            string fileName = newPresetName + ".prefab";

            // Preset が配置されているフォルダのパスを取得
            string presetDirPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(preset));

            // ディレクトリパスを作成
            string dirPath = Path.Combine(presetDirPath, dirName);

            string filePath = "";

            // Preset が入っているフォルダが "dirName" でない場合のみ、"dirName" ディレクトリを作成
            if (!presetDirPath.EndsWith(dirName))
            {
                if (!AssetDatabase.IsValidFolder(dirPath))
                {
                    AssetDatabase.CreateFolder(presetDirPath, dirName);
                }
                filePath = Path.Combine(dirPath, fileName);
            }
            else
            {
                // Preset を上書き保存
                filePath = Path.Combine(presetDirPath, fileName);
            }
            PrefabUtility.SaveAsPrefabAsset(target, filePath);
            AssetDatabase.SaveAssets();

            // 生成した Prefab に フォーカスする
            Object newPrefab = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            Selection.activeObject = newPrefab;
            EditorGUIUtility.PingObject(newPrefab);
        }


        private static void CopyComponent(Component source, Transform target, List<string> skipList, int selectedAxisIndex, float multiplier,
                                          Transform sourceRoot, Transform targetRoot)
        {
            // Transform をコピー
            if (source is Transform sourceTransform)
            {
                Transform targetTransform = target.transform;

                // https://light11.hatenadiary.com/entry/2019/04/18/202742
                var origTransform = PrefabUtility.GetCorrespondingObjectFromSource(sourceTransform);
                ClothTransformApplier.TransformDiff diff = new ClothTransformApplier.TransformDiff
                {
                    pos = sourceTransform.localPosition - origTransform.localPosition,
                    rot = sourceTransform.rotation * Quaternion.Inverse(origTransform.rotation),
                };

                // 差分を逆に適用
                targetTransform.localPosition -= diff.pos;
                targetTransform.rotation = targetTransform.rotation * diff.rot;
                targetTransform.localScale = ClothTransformApplier.DivideVector3(targetTransform.localScale, sourceTransform.localScale);

                // 大体が Preset よりちょっと小さくなるので、試しにこれで拡大してみる.
                var scale = targetTransform.localScale;
                switch (selectedAxisIndex)
                {
                    case 0: // X を選択
                        scale.y *= multiplier;
                        scale.z *= multiplier;
                        break;
                    case 1: // Y を選択
                        scale.x *= multiplier;
                        scale.z *= multiplier;
                        break;
                    case 2: // Z を選択
                        scale.x *= multiplier;
                        scale.y *= multiplier;
                        break;
                }
                targetTransform.localScale = scale;

                return;
            }

            // Skip List をスキップ
            var type = source.GetType();
            foreach (var skip in skipList)
            {
                if (type.Name.IndexOf(skip, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Debug.Log($"[Cloth Transform Applier] Skip Copy '{type.Name}' in '{target.name}'.");
                    return;
                }
            }

            if (source is ModularAvatarScaleAdjuster scaleAdjuster)
            {
                ModularAvatarScaleAdjuster targetComponent = target.GetComponent<ModularAvatarScaleAdjuster>();

                if (targetComponent != null) // すでにコンポーネントが存在する場合
                {
                    // targetのScaleにsourceのScaleを除算
                    targetComponent.Scale = ClothTransformApplier.DivideVector3(targetComponent.Scale, scaleAdjuster.Scale);
                }
                else
                {
                    // コンポーネントがない場合、新規に追加してから値をコピー
                    targetComponent = Undo.AddComponent<ModularAvatarScaleAdjuster>(target.gameObject);
                    targetComponent.Scale = scaleAdjuster.Scale;
                }
                return;
            }

            // Transform 以外のComponent をコピー
            if (target.GetComponent(type) == null)
            {
                Undo.AddComponent(target.gameObject, type); // AddComponentの代わりにUndo.AddComponentを使用
            }
            Component copy = target.GetComponent(type);

            //シリアライズ済みフィールドを丸ごとコピー
            EditorUtility.CopySerialized(source, copy);

            // Object 参照をターゲット階層に置き換え
            var so = new SerializedObject(copy);
            var prop = so.GetIterator();

            while (prop.Next(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;

                var obj = prop.objectReferenceValue;
                if (obj == null) continue;

                var newObj = ClothTransformApplier.RemapSingleReference(obj, sourceRoot, targetRoot);
                if (newObj != obj)
                    prop.objectReferenceValue = newObj;
            }
            so.ApplyModifiedProperties();
        }
    }
}
#endif