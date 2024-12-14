using UnityEngine;
using UnityEditor;


namespace WataOfuton.Tools
{
    public static class WataOfutonEditorUtility
    {
        public static void HorizontalFieldBool(string labelText, ref bool parametor, ref bool checkEnable)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            checkEnable = EditorGUILayout.Toggle("", checkEnable, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            parametor = EditorGUILayout.Toggle(labelText, parametor);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }

        public static void HorizontalFieldPopup(string labelText, ref int index, string[] settingText, ref bool checkEnable)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            checkEnable = EditorGUILayout.Toggle("", checkEnable, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            index = EditorGUILayout.Popup(labelText, index, settingText);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }

        public static void HorizontalFieldFloat(string labelText, ref float parametor, ref bool checkEnable)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            checkEnable = EditorGUILayout.Toggle("", checkEnable, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            parametor = EditorGUILayout.FloatField(labelText, parametor);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }

        public static void HorizontalFieldFloatSlider(string labelText, ref float parametor, ref bool checkEnable, float left, float right)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            checkEnable = EditorGUILayout.Toggle("", checkEnable, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            parametor = EditorGUILayout.Slider(labelText, parametor, left, right);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }
        public static void HorizontalFieldIntSlider(string labelText, ref int parametor, ref bool checkEnable, int left, int right)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            checkEnable = EditorGUILayout.Toggle("", checkEnable, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            parametor = EditorGUILayout.IntSlider(labelText, parametor, left, right);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }

        public static void SaveBoolArray(string key, bool[] array)
        {
            string json = JsonUtility.ToJson(new BoolArrayWrapper { Array = array });
            EditorPrefs.SetString(key, json);
        }
        public static bool[] LoadBoolArray(string key, int defaultSize = 0)
        {
            if (EditorPrefs.HasKey(key))
            {
                string json = EditorPrefs.GetString(key);
                BoolArrayWrapper wrapper = JsonUtility.FromJson<BoolArrayWrapper>(json);
                return wrapper.Array;
            }
            return new bool[defaultSize];
        }
        [System.Serializable]
        private class BoolArrayWrapper
        {
            public bool[] Array;
        }
    }
}