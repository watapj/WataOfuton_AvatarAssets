using UnityEditor;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;

namespace WataOfuton.Tools.ClothTransformApplier.Editor
{
    public static class CheckForUpdate
    {
        [System.Serializable]
        public class VersionInfo
        {
            public string version;
            public string releaseURL;
        }

        private const string VersionJsonUrl = "https://raw.githubusercontent.com/watapj/WataOfuton_AvatarAssets/main/Assets/WataOfuton/ClothTransformApplier/version.json"; // GitHubのversion.jsonのURL
        private static string currentVersion = "2025.07.12"; // 現在のローカルバージョン


        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            CheckForUpdateAsync();
        }

        private static async void CheckForUpdateAsync()
        {
            try
            {
                string json = await GetVersionJsonAsync(VersionJsonUrl);
                // Debug.Log($"json : {json}");
                var remoteVersion = JsonUtility.FromJson<VersionInfo>(json);

                // ローカルバージョンとリモートバージョンを比較
                if (remoteVersion.version != currentVersion)
                {
                    currentVersion = remoteVersion.version;
                    ClothTransformApplierEditor.CheckForUpdate(remoteVersion, true);
                }
                else
                {
                    ClothTransformApplierEditor.CheckForUpdate(remoteVersion, false);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Cloth Transform Applier] Failed to check for updates: {ex.Message}");
            }
        }

        private static async Task<string> GetVersionJsonAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(url);
            }
        }
    }
}