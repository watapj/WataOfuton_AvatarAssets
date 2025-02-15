﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace WataOfuton.Tools
{
    public class MaterialPropertyBatchSetterWindow : EditorWindow
    {
        private const string MENU_ITEM_PATH = "Window/WataOfuton/Material Property Batch Setter";
        private Vector2 scrollPosition;

        private float[] forcedValues;
        private bool[] checkEnables;
        public static readonly string[,] targetParamNames = new string[,]{{"_LightMinLimit",     "明るさの下限"},
                                                                          {"_LightMaxLimit",      "明るさの上限"},
                                                                          {"_MonochromeLighting", "ライトのモノクロ化"},
                                                                          {"_ShadowEnvStrength",  "影色への環境光影響度"},
                                                                          {"_AsUnlit",            "Unlit化"},
                                                                          {"_MatCapEnableLighting",  "[MatCap]ライトの明るさを反映"},
                                                                          {"_MatCap2ndEnableLighting",  "[MatCap2nd]ライトの明るさを反映"},
                                                                          {"_RimEnableLighting",  "[リムライト]ライトの明るさを反映"},
                                                                          {"_GlitterEnableLighting",  "[ラメ]ライトの明るさを反映"},
                                                                          {"_OutLineEnableLighting",  "[アウトライン]ライトの明るさを反映"},
                                                                         };
        private static readonly float[,] sliderSet = new float[,]{{0f, 1f},
                                                                  {0f, 10f},
                                                                  {0f, 1f},
                                                                  {0f, 1f},
                                                                  {0f, 1f},
                                                                  {0f, 1f},
                                                                  {0f, 1f},
                                                                  {0f, 1f},
                                                                  {0f, 1f},
                                                                  {0f, 1f},
                                                                 };
        private static readonly float[] defaultValues = new float[] { 0.05f, 1f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 1f };


        [MenuItem(MENU_ITEM_PATH)]
        public static void ShowWindow()
        {
            GetWindow<MaterialPropertyBatchSetterWindow>("Material Property Batch Setter");
        }

        private void OnEnable()
        {
            checkEnables = new bool[defaultValues.Length];
            forcedValues = new float[defaultValues.Length];
            LoadForcedValues();
        }

        private void OnGUI()
        {
            // スクリプトの表示
            // MonoScript script = MonoScript.FromScriptableObject(this);
            // EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            GUILayout.Label("Material Property Batch Setter Settings", EditorStyles.boldLabel);
            string text = "lilToon を使用しているマテリアルのパラメータを一括して設定するエディタ拡張です。\n"
                        + "一括設定を行いたいパラメータ名の左にあるチェックボックスにチェックを入れ、それぞれの値を入力した後、最下部の [Save Settings] を押してください。\n"
                        + "マテリアルを選択した際に自動で設定が反映されます。"
                        ;
            EditorGUILayout.HelpBox(text, MessageType.Info);
            text = "フォルダを選択して右クリックメニューから、そのフォルダ下のすべてのマテリアルのパラメータを一括して設定することも可能です。";
            EditorGUILayout.HelpBox(text, MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(8);
            for (int i = 0; i < forcedValues.Length; i++)
            {
                WataOfutonEditorUtility.HorizontalFieldFloatSlider(targetParamNames[i, 1], ref forcedValues[i], ref checkEnables[i], sliderSet[i, 0], sliderSet[i, 1]);
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Save Setting."))
            {
                SaveForcedValues();
            }
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Initialize Settings"))
            {
                // 警告ダイアログを表示
                bool userClickedOK = EditorUtility.DisplayDialog(
                    "確認", // タイトル
                    "設定を初期化します。よろしいですか？", // メッセージ
                    "はい", // OK ボタンのテキスト
                    "いいえ" // キャンセルボタンのテキスト
                );
                if (userClickedOK)
                {
                    InitForcedValues();
                    LoadForcedValues();
                }
            }
            // デバッグ用
            // EditorGUILayout.Space(5);
            // if (GUILayout.Button("Clear Keys. (for Debug)"))
            // {
            //     ClearForcedValues();
            // }

            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        private void LoadForcedValues()
        {
            for (int i = 0; i < forcedValues.Length; i++)
            {
                if (EditorPrefs.HasKey("MaterialPropertyBatchSetter." + targetParamNames[i, 0]))
                {
                    forcedValues[i] = EditorPrefs.GetFloat("MaterialPropertyBatchSetter." + targetParamNames[i, 0]);
                }
                else
                {
                    forcedValues[i] = defaultValues[i];
                }
            }
            bool[] tmp = WataOfutonEditorUtility.LoadBoolArray("MaterialPropertyBatchSetter.CheckEnables", checkEnables.Length);
            if (tmp.Length == checkEnables.Length)
            {
                checkEnables = tmp;
            }
            else
            {
                int lengthToCopy = Math.Min(checkEnables.Length, tmp.Length);
                Array.Copy(tmp, checkEnables, lengthToCopy);
            }
            Debug.Log("[Material Property Batch Setter] Load Settings.");
        }

        private void SaveForcedValues()
        {
            for (int i = 0; i < forcedValues.Length; i++)
            {
                EditorPrefs.SetFloat("MaterialPropertyBatchSetter." + targetParamNames[i, 0], forcedValues[i]);
            }
            WataOfutonEditorUtility.SaveBoolArray("MaterialPropertyBatchSetter.CheckEnables", checkEnables);
            Debug.Log("[Material Property Batch Setter] Settings Saved.");
        }
        private void InitForcedValues()
        {
            for (int i = 0; i < forcedValues.Length; i++)
            {
                EditorPrefs.SetFloat("MaterialPropertyBatchSetter." + targetParamNames[i, 0], defaultValues[i]);
            }
            WataOfutonEditorUtility.SaveBoolArray("MaterialPropertyBatchSetter.CheckEnables", new bool[checkEnables.Length]);
            Debug.Log("[Material Property Batch Setter] Settings Initialized.");
        }

        private void ClearForcedValues()
        {
            for (int i = 0; i < forcedValues.Length; i++)
            {
                EditorPrefs.DeleteKey("MaterialPropertyBatchSetter." + targetParamNames[i, 0]);
            }
            EditorPrefs.DeleteKey("MaterialPropertyBatchSetter.CheckEnables");
            Debug.Log("[Material Property Batch Setter] Clear Keys. (for Debug)");
        }
    }


    public static class MaterialPropertyBatchSetterMenu
    {
        [MenuItem("Assets/WataOfuton/Apply to All Materials in Folder (MaterialPropertyBatchSetter)", false, 1200)]
        private static void ApplyToAllMaterialsInFolder()
        {
            // 選択されたフォルダのパスを取得
            var selectedFolderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(selectedFolderPath))
            {
                Debug.LogWarning("Selected item is not a folder.");
                return;
            }

            string[,] targetParamNames = MaterialPropertyBatchSetterWindow.targetParamNames;
            float[] forcedValues = new float[targetParamNames.Length / 2];
            for (int i = 0; i < forcedValues.Length; i++)
            {
                forcedValues[i] = EditorPrefs.GetFloat("MaterialPropertyBatchSetter." + targetParamNames[i, 0]);
            }

            // 指定されたフォルダ内のすべてのマテリアルに対して処理を実行
            string[] fileEntries = Directory.GetFiles(selectedFolderPath, "*.mat", SearchOption.AllDirectories);
            foreach (string filePath in fileEntries)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(filePath);
                if (mat != null)
                {
                    MaterialPropertyBatchSetter.ApplyPropertiesToMaterial(mat, targetParamNames, forcedValues);
                }
            }

            Debug.Log("[Material Property Batch Setter] Applied properties to all materials in folder: " + selectedFolderPath);
        }


        // メニューオプションの有効/無効を切り替え
        [MenuItem("Assets/MaterialPropertyBatchSetter/Apply to All Materials in Folder", true, 1200)]
        private static bool ApplyToAllMaterialsInFolderValidation()
        {
            // フォルダが選択されているかどうかをチェック
            return Selection.activeObject != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
        }
    }


    [InitializeOnLoad]
    public class MaterialPropertyBatchSetter
    {
        // 変更対象のシェーダ名
        public static readonly string targetShaderName = "lilToon";

        static MaterialPropertyBatchSetter()
        {
            Enable();
        }

        public static void Enable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        public static void Disable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        static void OnSelectionChanged()
        {
            string[,] targetParamNames = MaterialPropertyBatchSetterWindow.targetParamNames;

            float[] forcedValues = new float[targetParamNames.Length / 2];
            for (int i = 0; i < forcedValues.Length; i++)
            {
                forcedValues[i] = EditorPrefs.GetFloat("MaterialPropertyBatchSetter." + targetParamNames[i, 0]);
            }

            foreach (var obj in Selection.objects)
            {
                var mat = obj as Material;
                if (mat != null && mat.shader.name.Contains(targetShaderName))
                {
                    ApplyPropertiesToMaterial(mat, targetParamNames, forcedValues);
                }
            }
        }

        public static void ApplyPropertiesToMaterial(Material mat, string[,] targetParamNames, float[] forcedValues)
        {
            bool[] enables = WataOfutonEditorUtility.LoadBoolArray("MaterialPropertyBatchSetter.CheckEnables");

            for (int i = 0; i < targetParamNames.Length / 2; i++)
            {
                if (!enables[i]) continue;
                if (!mat.HasProperty(targetParamNames[i, 0])) continue;
                if (mat.GetFloat(targetParamNames[i, 0]) == forcedValues[i]) continue;

                mat.SetFloat(targetParamNames[i, 0], forcedValues[i]);
                EditorUtility.SetDirty(mat); // 変更を保存
                Debug.Log("[Material Property Batch Setter] Set " + targetParamNames[i, 1] + " of " + mat.name + " to " + forcedValues[i]);
            }
        }
    }

    public class MaterialPropertyBatchSetterImportProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // 必要なパラメータを取得
            string[,] targetParamNames = MaterialPropertyBatchSetterWindow.targetParamNames;
            float[] forcedValues = new float[targetParamNames.Length / 2];
            for (int i = 0; i < forcedValues.Length; i++)
            {
                forcedValues[i] = EditorPrefs.GetFloat("MaterialPropertyBatchSetter." + targetParamNames[i, 0]);
            }

            // インポートされたアセットをチェック
            foreach (string path in importedAssets)
            {
                // マテリアルかどうかを確認
                if (path.EndsWith(".mat"))
                {
                    // マテリアルをロード
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat != null && mat.shader.name.Contains(MaterialPropertyBatchSetter.targetShaderName))
                    {
                        MaterialPropertyBatchSetter.ApplyPropertiesToMaterial(mat, targetParamNames, forcedValues);
                    }
                }
            }
        }
    }
}