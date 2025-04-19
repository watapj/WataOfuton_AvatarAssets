using UnityEngine;
using UnityEditor;
using System.IO;

namespace WataOfuton.Tools.MCP_MergeNeck
{
    /// <summary>
    /// 顔・体のメッシュに差分を適用し、「Apply Triangle Diff Data All」ボタン押下時に
    /// 保存先フォルダをユーザーに選んでもらい、
    /// 適用後の顔メッシュと体メッシュをそこに .asset で保存するエディタ拡張ウィンドウ。
    /// </summary>
    public class ApplyTriangleDiffDataAllWindow : EditorWindow
    {
        public SkinnedMeshRenderer targetFaceRenderer; // 差分を適用する顔メッシュ
        public SkinnedMeshRenderer targetBodyRenderer; // 差分を適用する体メッシュ
        public TriangleDiffDataAll triangleDiffDataAll; // 差分データ(ScriptableObject)

        public static bool isShowUpdateMessage;
        public static CheckForUpdate.VersionInfo versionInfo;


        [MenuItem("Window/WataOfuton/MCP_MergeNeck [Apply]")]
        private static void OpenWindow()
        {
            var window = GetWindow<ApplyTriangleDiffDataAllWindow>();
            window.titleContent = new GUIContent("MCP_MergeNeck [Apply]");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("ますきゃの首の繋ぎ目を整えるツール", EditorStyles.boldLabel);

            if (isShowUpdateMessage)
            {
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

            EditorGUILayout.HelpBox(
                "差分データを適用したい顔と体のオブジェクト及び TriangleDiffDataAll.asset を設定してください."
                ,
                MessageType.Info);

            targetFaceRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Target Face Renderer", targetFaceRenderer, typeof(SkinnedMeshRenderer), true);
            targetBodyRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Target Body Renderer", targetBodyRenderer, typeof(SkinnedMeshRenderer), true);
            EditorGUILayout.Space(5);

            triangleDiffDataAll = (TriangleDiffDataAll)EditorGUILayout.ObjectField("Triangle Diff Data All", triangleDiffDataAll, typeof(TriangleDiffDataAll), false);
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Apply Triangle Diff Data All"))
            {
                ApplyDiffData();
            }
            EditorGUILayout.Space(5);
        }

        public static void CheckForUpdate(CheckForUpdate.VersionInfo info, bool isShow)
        {
            isShowUpdateMessage = isShow;
            versionInfo = info;
        }

        /// <summary>
        /// 差分データを適用して、対象メッシュの頂点位置を更新する。
        /// 差分は、ユーザーメッシュの各頂点UVをもとに、
        /// 差分データ内の三角形でバリセントリック補間を行い、加算される。
        /// </summary>
        private void ApplyDiffData()
        {
            if (triangleDiffDataAll == null)
            {
                Debug.LogError("[ApplyTriangleDiffData] 差分データが設定されていません。");
                return;
            }
            if (targetFaceRenderer == null || targetBodyRenderer == null)
            {
                Debug.LogError("[ApplyTriangleDiffData] 対象のRendererが設定されていません。");
                return;
            }

            Undo.RecordObject(targetFaceRenderer, "Apply Triangle Diff Data (Face+Body)");
            Undo.RecordObject(targetBodyRenderer, "Apply Triangle Diff Data (Face+Body)");

            // (1) まず保存先フォルダを選ばせる(Assets以下)
            //     EditorUtility.SaveFilePanelInProject を使い、ユーザーに「ファイル名」指定を促す
            //     ただ実質フォルダのみを選んでもらう想定なので、案内文を工夫する
            string dummyFileName = "SelectFolder.asset"; // 形式上ファイル名は必須だが、後で削除
            string path = EditorUtility.SaveFilePanelInProject(
                "Select Folder to Save Final Meshes",
                dummyFileName,
                "asset",
                "保存先フォルダを選んでください(ファイル名は無視されます)",
                "Assets/WataOfuton/MCP_MergeNeck" // 初期フォルダ 配布時はコメントアウトする
            );

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[ApplyTriangleDiffData] 保存先フォルダ選択がキャンセルされました。");
                return;
            }

            string folder = Path.GetDirectoryName(path);
            // Debug.Log($"ユーザーが選択したフォルダ: {folder}");

            // (2) 差分を適用
            // 顔に差分適用
            Mesh faceFinalMesh = MCP_MergeNeckCore.ApplyTriangleDiffData(targetFaceRenderer, triangleDiffDataAll.faceTriangles);
            // 体に差分適用
            Mesh bodyFinalMesh = MCP_MergeNeckCore.ApplyTriangleDiffData(targetBodyRenderer, triangleDiffDataAll.bodyTriangles);

            // (3) 仕上がったメッシュをフォルダに .asset で保存
            SaveMeshToFolder(faceFinalMesh, folder);
            SaveMeshToFolder(bodyFinalMesh, folder);

            Debug.Log("[ApplyTriangleDiffData] 差分の適用が完了しました。");
        }


        /// <summary>
        /// メッシュを指定フォルダ内に .asset ファイルとして保存する。
        /// </summary>
        private void SaveMeshToFolder(Mesh mesh, string folderPath)
        {
            if (mesh == null) return; // 差分適用失敗など
            if (string.IsNullOrEmpty(folderPath)) return;

            // ファイル名
            string fileName = mesh.name;
            fileName = fileName.Substring(0, fileName.Length - 7); // _(Clone)
            fileName = fileName + "_SeamNormals.asset";

            // フォルダとファイル名を結合
            string savePath = Path.Combine(folderPath, fileName);

            // 「Assets/」から始まるパスに統一 (パス区切りを正規化)
            savePath = savePath.Replace("\\", "/");

            // 既存アセットがある場合は上書き
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(savePath);
            }

            AssetDatabase.CreateAsset(mesh, savePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ApplyTriangleDiffData] Meshを保存しました: {savePath}");
        }

    }
}