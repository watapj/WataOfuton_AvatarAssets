#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace WataOfuton.Tools.ClothTransformApplier.Editor
{
    [CustomEditor(typeof(ClothTransformApplier))]
    public class ClothTransformApplierEditor : UnityEditor.Editor
    {
        ClothTransformApplier comp;
        SerializedProperty _transformGroups;
        SerializedProperty _skipComponentList;
        SerializedProperty _isUseMergeArmature;
        SerializedProperty _isRemoveMeshSettings;
        SerializedProperty _isUseCopyScaleAdjuster;
        SerializedProperty _isRenameBreast;
        SerializedProperty _breastLeft;
        SerializedProperty _breastRight;
        SerializedProperty _isFixScaleAdjuster;
        private ReorderableList reorderableList;
        private bool isDisplaySettings;
        private static bool isShowUpdateMessage;
        private static CheckForUpdate.VersionInfo versionInfo;


        /// <summary>
        /// 外部スクリプトから呼び出す共通 API。
        /// <summary>
        public static void DrawPresetWithAlterith(ClothTransformApplier applier, ref int targetIndex)
        {
            GUILayout.BeginVertical("ClothTransformApplier", "window");

            string error = "";
            if (applier == null)
            {
                error = "[ClothTransformApplier] ClothTransformApplier not found";
            }
            else if (applier.transformGroups == null || applier.transformGroups.Count == 0)
            {
                error = "[ClothTransformApplier] PresetGroups is Empty";
            }

            if (error == "")
            {
                DrawGroupSelector(applier, ref targetIndex);
            }
            else
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            GUILayout.EndVertical();
        }

        private static void DrawGroupSelector(ClothTransformApplier applier, ref int targetIndex)
        {
            SerializedObject so = new SerializedObject(applier);
            SerializedProperty groupsProp = so.FindProperty(nameof(applier.transformGroups));

            int count = groupsProp.arraySize;

            // インデックスをレンジ内に拘束
            targetIndex = Mathf.Clamp(targetIndex, 0, count - 1);

            EditorGUILayout.Space(2);

            //-------------------- 見出し + ▲▼ボタン --------------------
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"Select Preset. ({targetIndex + 1}/{count})  (Read Only)", GUILayout.ExpandWidth(true));

                if (GUILayout.Button("▲", GUILayout.Width(34)))
                    targetIndex = (targetIndex - 1 + count) % count;

                if (GUILayout.Button("▼", GUILayout.Width(34)))
                    targetIndex = (targetIndex + 1) % count;
            }

            //-------------------- 選択中の TransformGroup を 1 件だけ表示 --------------------
            var element = groupsProp.GetArrayElementAtIndex(targetIndex);
            var nameProp = element.FindPropertyRelative("Name");
            var presetPrefabProp = element.FindPropertyRelative("presetPrefab");
            var isFoldoutOpenProp = element.FindPropertyRelative("isFoldoutOpen");
            var blendShapeSetProp = element.FindPropertyRelative("blendShapeSet");
            var isDiffModeProp = element.FindPropertyRelative("isDiffMode");

            // PresetPrefab のチェック　上下矢印は出したい
            if (presetPrefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("[ClothTransformApplier] PresetPrefab is Empty", MessageType.Error);
                return;
            }

            // EditorGUI.BeginDisabledGroup(true);
            using (new EditorGUILayout.VerticalScope("HelpBox", GUILayout.ExpandWidth(true)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Nameフィールド
                    EditorGUILayout.LabelField("Name", GUILayout.Width(60));
                    EditorGUILayout.TextField(nameProp.stringValue, GUILayout.Width(120));
                    // presetPrefab フィールド
                    EditorGUILayout.ObjectField(presetPrefabProp.objectReferenceValue, typeof(GameObject), true);
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.Toggle("Difference Mode", isDiffModeProp.boolValue);

                EditorGUILayout.Foldout(isFoldoutOpenProp.boolValue, "BlendShapeSet");
                if (isFoldoutOpenProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    // BlendShapeSetリストを描画
                    for (int i = 0; i < blendShapeSetProp.arraySize; i++)
                    {
                        var blendShapeElement = blendShapeSetProp.GetArrayElementAtIndex(i);
                        var namePropInBlendShape = blendShapeElement.FindPropertyRelative("name");
                        var valuePropInBlendShape = blendShapeElement.FindPropertyRelative("value");

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            // Nameフィールド
                            EditorGUILayout.LabelField("Name" + (i + 1).ToString(), GUILayout.Width(100));

                            // NameとValueを描画
                            EditorGUILayout.PropertyField(namePropInBlendShape, GUIContent.none);
                            EditorGUILayout.DelayedFloatField(valuePropInBlendShape, GUIContent.none);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            // EditorGUI.EndDisabledGroup();

            return;
        }


        void OnEnable()
        {
            comp = target as ClothTransformApplier;

            // SerializedObjectのプロパティ取得
            _transformGroups = serializedObject.FindProperty(nameof(comp.transformGroups));
            _skipComponentList = serializedObject.FindProperty(nameof(comp.skipComponentList));
            _isUseMergeArmature = serializedObject.FindProperty(nameof(comp.isUseMergeArmature));
            _isRemoveMeshSettings = serializedObject.FindProperty(nameof(comp.isRemoveMeshSettings));
            _isUseCopyScaleAdjuster = serializedObject.FindProperty(nameof(comp.isUseCopyScaleAdjuster));
            _isRenameBreast = serializedObject.FindProperty(nameof(comp.isRenameBreast));
            _isFixScaleAdjuster = serializedObject.FindProperty(nameof(comp.isFixScaleAdjuster));
            _breastLeft = serializedObject.FindProperty(nameof(comp.breastLeft));
            _breastRight = serializedObject.FindProperty(nameof(comp.breastRight));

            // ReorderableListの初期化（SerializedPropertyを使う）
            reorderableList = new ReorderableList(serializedObject, _transformGroups, true, true, true, true);

            var lineCount = 0;

            // ヘッダーコールバック
            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Preset Groups");
            };

            // 要素描画コールバック
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _transformGroups.GetArrayElementAtIndex(index); // 各要素を取得
                var nameProp = element.FindPropertyRelative("Name");
                var presetPrefabProp = element.FindPropertyRelative("presetPrefab");
                var isFoldoutOpenProp = element.FindPropertyRelative("isFoldoutOpen");
                var blendShapeSetProp = element.FindPropertyRelative("blendShapeSet");
                var isDiffModeProp = element.FindPropertyRelative("isDiffMode");

                rect.y += 2;

                // Nameフィールド
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 40, EditorGUIUtility.singleLineHeight), "Name");
                nameProp.stringValue = EditorGUI.TextField(new Rect(rect.x + 45, rect.y, 120, EditorGUIUtility.singleLineHeight), nameProp.stringValue);

                // presetPrefab フィールド
                presetPrefabProp.objectReferenceValue = (GameObject)EditorGUI.ObjectField(new Rect(rect.x + 170, rect.y, rect.width - 230, EditorGUIUtility.singleLineHeight), presetPrefabProp.objectReferenceValue, typeof(GameObject), true);

                // Apply ボタン
                if (GUI.Button(new Rect(rect.x + rect.width - 55, rect.y, 55, EditorGUIUtility.singleLineHeight), "Apply"))
                {
                    ClothTransformApplier.TransformGroup group = new ClothTransformApplier.TransformGroup
                    {
                        Name = element.FindPropertyRelative("Name").stringValue,
                        presetPrefab = (GameObject)element.FindPropertyRelative("presetPrefab").objectReferenceValue,
                        isFoldoutOpen = element.FindPropertyRelative("isFoldoutOpen").boolValue,
                        blendShapeSet = new List<ClothTransformApplier.BlendShapeSet>(),
                        isDiffMode = element.FindPropertyRelative("isDiffMode").boolValue,
                        lineCount = element.FindPropertyRelative("lineCount").intValue,
                    };
                    // blendShapeSetの各要素をリストに追加
                    for (int i = 0; i < blendShapeSetProp.arraySize; i++)
                    {
                        var blendShapeElement = blendShapeSetProp.GetArrayElementAtIndex(i);
                        string blendShapeName = blendShapeElement.FindPropertyRelative("name").stringValue;
                        float blendShapeValue = blendShapeElement.FindPropertyRelative("value").floatValue;
                        group.blendShapeSet.Add(new ClothTransformApplier.BlendShapeSet(blendShapeName, blendShapeValue));
                    }

                    comp.ApplyClothSettings(group, null);
                }

                // 差分モード
                EditorGUI.indentLevel++;
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                isDiffModeProp.boolValue = EditorGUI.Toggle(new Rect(rect.x, rect.y, 400, EditorGUIUtility.singleLineHeight), "Difference Mode", isDiffModeProp.boolValue);

                // BlendShapeSetのFoldout
                rect.y += EditorGUIUtility.singleLineHeight + 1;
                isFoldoutOpenProp.boolValue = EditorGUI.Foldout(new Rect(rect.x, rect.y, 40, EditorGUIUtility.singleLineHeight), isFoldoutOpenProp.boolValue, "BlendShapeSet");
                if (isFoldoutOpenProp.boolValue)
                {
                    rect.y += EditorGUIUtility.singleLineHeight + 1;

                    if (blendShapeSetProp.arraySize == 0)
                    {
                        blendShapeSetProp.InsertArrayElementAtIndex(0);
                    }

                    // BlendShapeSetリストを描画
                    for (int i = 0; i < blendShapeSetProp.arraySize; i++)
                    {
                        var blendShapeElement = blendShapeSetProp.GetArrayElementAtIndex(i);
                        var namePropInBlendShape = blendShapeElement.FindPropertyRelative("name");
                        var valuePropInBlendShape = blendShapeElement.FindPropertyRelative("value");

                        // Nameフィールド
                        EditorGUI.LabelField(new Rect(rect.x, rect.y, 70, EditorGUIUtility.singleLineHeight), "Name" + (i + 1).ToString());

                        // NameとValueを描画
                        EditorGUI.PropertyField(new Rect(rect.x + 60, rect.y, 140, EditorGUIUtility.singleLineHeight), namePropInBlendShape, GUIContent.none);

                        float val = valuePropInBlendShape.floatValue;
                        if (val >= 0f && val <= 100f)
                        {
                            val = CustomSlider(new Rect(rect.x + 205, rect.y, rect.width - 265, EditorGUIUtility.singleLineHeight), val, 0f, 100f);
                            valuePropInBlendShape.floatValue = val;
                        }
                        else
                        {
                            EditorGUI.DelayedFloatField(new Rect(rect.x + 190, rect.y, rect.width - 250, EditorGUIUtility.singleLineHeight), valuePropInBlendShape, GUIContent.none);
                        }

                        // "+" ボタンで要素を追加
                        if (GUI.Button(new Rect(rect.x + rect.width - 55, rect.y, 24, EditorGUIUtility.singleLineHeight), "+"))
                        {
                            blendShapeSetProp.InsertArrayElementAtIndex(i + 1);
                        }
                        // "-" ボタンで要素を削除
                        if (GUI.Button(new Rect(rect.x + rect.width - 25, rect.y, 24, EditorGUIUtility.singleLineHeight), "-"))
                        {
                            blendShapeSetProp.DeleteArrayElementAtIndex(i);
                        }

                        rect.y += EditorGUIUtility.singleLineHeight;
                    }
                    lineCount = blendShapeSetProp.arraySize + 2;
                }
                else
                {
                    lineCount = 2;
                }
                EditorGUI.indentLevel--;
            };

            // Addボタンが押されたときの処理
            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                _transformGroups.arraySize++;
                var newElement = _transformGroups.GetArrayElementAtIndex(_transformGroups.arraySize - 1);

                // 各フィールドを初期化
                newElement.FindPropertyRelative("Name").stringValue = "New Preset";
                newElement.FindPropertyRelative("presetPrefab").objectReferenceValue = null;
                newElement.FindPropertyRelative("isFoldoutOpen").boolValue = false;
                newElement.FindPropertyRelative("blendShapeSet").arraySize = 0;
                // プロパティの変更を適用
                serializedObject.ApplyModifiedProperties();
            };

            // 要素の高さコールバック
            reorderableList.elementHeightCallback = (index) =>
            {
                var element = _transformGroups.GetArrayElementAtIndex(index);
                var isFoldoutOpenProp = element.FindPropertyRelative("isFoldoutOpen");
                var blendShapeSetProp = element.FindPropertyRelative("blendShapeSet");
                if (isFoldoutOpenProp.boolValue)
                    lineCount = blendShapeSetProp.arraySize + 2;
                else
                    lineCount = 2;
                return EditorGUIUtility.singleLineHeight * lineCount + 24;
            };

            EditorApplication.update += OnEditorUpdate;
        }
        void OnDisable()
        {
            comp = null;
            EditorApplication.update -= OnEditorUpdate;
        }
        void OnEditorUpdate()
        {
            if (comp == null) return;
            // このオブジェクトはアバターの配下において Transform がデフォルトである前提で動作するためそれを強制する.
            if (comp.transform.parent != null)
            {
                comp.transform.localPosition = Vector3.zero;
                comp.transform.localRotation = Quaternion.identity;
                comp.transform.localScale = Vector3.one;
            }
        }


        public override void OnInspectorGUI()
        {
            // EditorGUI.BeginDisabledGroup(true);
            // EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            // EditorGUI.EndDisabledGroup();

            serializedObject.Update();

            DrawUpdateMessage();
            DrawBaseSettings();

            // ReorderableListの描画
            reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBaseSettings()
        {
            isDisplaySettings = EditorGUILayout.Foldout(isDisplaySettings, "基本設定を表示する");
            if (!isDisplaySettings) return;

            using (new EditorGUILayout.VerticalScope("HelpBox", GUILayout.ExpandWidth(true)))
            {
                // Skip Components
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_skipComponentList, new GUIContent("Skip Copy Component Name"), true);
                EditorGUI.indentLevel--;
                EditorGUILayout.HelpBox("このリストに記載した Component はコピーしません（検索には部分一致を含みます）", MessageType.Info);

                EditorGUILayout.Space(5);

                // Merge Armature / Mesh Settings / CopyScaleAdjuster
                _isUseMergeArmature.boolValue = EditorGUILayout.Toggle("MA Setup Outfit", _isUseMergeArmature.boolValue);
                EditorGUILayout.HelpBox("Modular Avatar の 'Setup Outfit' を実行します.", MessageType.Info);

                if (_isUseMergeArmature.boolValue)
                {
                    _isRemoveMeshSettings.boolValue = EditorGUILayout.Toggle("Remove MA Mesh Settings", _isRemoveMeshSettings.boolValue);
                    EditorGUILayout.HelpBox("衣装に自動で生成される Modular Avatar の 'Mesh Settings' を削除します.", MessageType.Info);

                    _isUseCopyScaleAdjuster.boolValue = EditorGUILayout.Toggle("Copy Scale Adjuster", _isUseCopyScaleAdjuster.boolValue);
                    EditorGUILayout.HelpBox("Numeira.CopyScaleAdjuster を実行します（事前にインポートされている場合のみ動作します）.", MessageType.Info);
                }

                EditorGUILayout.Space(5);

                // Fix Scale Adjuster
                _isFixScaleAdjuster.boolValue = EditorGUILayout.Toggle("Fix Scale Adjuster", _isFixScaleAdjuster.boolValue);
                EditorGUILayout.HelpBox("Preset と衣装の両方に 'Scale Adjuster' がある場合、Scale の値を自動調整します.", MessageType.Info);

                EditorGUILayout.Space(5);

                // Rename Breast
                _isRenameBreast.boolValue = EditorGUILayout.Toggle("Rename Breast Bone", _isRenameBreast.boolValue);
                if (_isRenameBreast.boolValue)
                {
                    EditorGUI.indentLevel++;
                    _breastLeft.objectReferenceValue = EditorGUILayout.ObjectField("素体左胸ボーン", _breastLeft.objectReferenceValue, typeof(GameObject), true);
                    _breastRight.objectReferenceValue = EditorGUILayout.ObjectField("素体右胸ボーン", _breastRight.objectReferenceValue, typeof(GameObject), true);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.HelpBox(
                    "実験的機能です. 衣装の胸ボーン名を素体と一致させるか指定します. 素体や衣装のボーン構成により意図した動作をしない場合があります.",
                    MessageType.Warning);

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Open Utility Window"))
                    ClothTransformApplierUtility.Create();
            }
            EditorGUILayout.Space(10);
        }

        private void DrawUpdateMessage()
        {
            if (!isShowUpdateMessage) return;

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
        public static void CheckForUpdate(CheckForUpdate.VersionInfo info, bool isShow)
        {
            isShowUpdateMessage = isShow;
            versionInfo = info;
        }

        // Reference : https://karanokan.info/2020/06/07/post-4953/#outline__2
        private static float CustomSlider(Rect rect, float val, float left_val, float right_val)
        {
            int control_id = GUIUtility.GetControlID(FocusType.Passive);
            Event ev = Event.current; // 現在のイベントを取得

            // スライダーと数値フィールドのサイズ計算
            int float_field_width = 65; // 数値フィールドの幅
            int slider_width = (int)(rect.width - float_field_width - 4); // スライダーの幅（数値フィールドの分を差し引く）

            // スライダーと数値フィールドの位置を計算
            Rect rect_slider = new Rect(rect.x, rect.y, slider_width + 12, rect.height);
            Rect rect_float = new Rect(rect.x + slider_width + 4, rect.y, float_field_width, rect.height);

            // スライダー背景とつまみの描画調整
            GUIStyle style_slider = GUI.skin.horizontalSlider;
            GUIStyle style_slider_thumb = GUI.skin.horizontalSliderThumb;

            // スライダーのリマップ（値から位置への変換）
            float remap_val = (val - left_val) / (right_val - left_val);
            int slider_thumb_size = 12;
            RectOffset slider_thumb_space = new RectOffset(0, 2, 1, 1);
            int slider_thumb_half_size = slider_thumb_size / 2;

            int rect_slide_area_width = slider_width - slider_thumb_half_size * 2 + slider_thumb_space.left + slider_thumb_space.right + 12;
            Rect rect_slide_area = new Rect(rect_slider.x + slider_thumb_half_size - slider_thumb_space.left, rect_slider.y, rect_slide_area_width, rect_slider.height);

            float slider_pos = Mathf.Lerp(rect_slide_area.x, rect_slide_area.x + rect_slide_area.width, remap_val) - slider_thumb_half_size;
            int slider_thumb_y_pos = (int)(rect.y + (rect.height - slider_thumb_size + 2) / 2);
            Rect rect_slider_thumb_draw = new Rect(slider_pos, slider_thumb_y_pos, slider_thumb_size, slider_thumb_size);

            // マウス操作処理
            Vector2 mouse_pos = ev.mousePosition;
            if (ev.button == 0)
            {
                switch (ev.type)
                {
                    case EventType.MouseDown:
                        if (rect_slide_area.Contains(mouse_pos))
                        {
                            float re_scale = (mouse_pos.x - rect_slide_area.x) / (rect_slide_area.width);
                            val = Mathf.Lerp(left_val, right_val, re_scale);
                            val = Mathf.Clamp(val, left_val, right_val);
                            GUIUtility.hotControl = control_id;
                            ev.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == control_id)
                        {
                            float re_scale = (mouse_pos.x - rect_slide_area.x) / (rect_slide_area.width);
                            val = Mathf.Lerp(left_val, right_val, re_scale);
                            val = Mathf.Clamp(val, left_val, right_val);
                            ev.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == control_id)
                        {
                            GUIUtility.hotControl = 0;
                            ev.Use();
                        }
                        break;
                }
            }

            // スライダーとつまみの描画
            if (ev.type == EventType.Repaint)
            {
                style_slider.Draw(rect_slider, GUIContent.none, control_id);
                style_slider_thumb.Draw(rect_slider_thumb_draw, GUIContent.none, control_id);
            }
            val = EditorGUI.DelayedFloatField(rect_float, val);

            return val;
        }
    }
}
#endif