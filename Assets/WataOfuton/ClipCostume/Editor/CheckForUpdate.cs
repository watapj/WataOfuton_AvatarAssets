using UnityEditor;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;

namespace WataOfuton.Tools.ClipCostumeSetting.Editor
{
    public static class CheckForUpdate
    {
        [System.Serializable]
        public class VersionInfo
        {
            public string version;
            public string releaseURL;
        }

        private const string VersionJsonUrl = "https://raw.githubusercontent.com/watapj/WataOfuton_AvatarAssets/main/Assets/WataOfuton/ClipCostume/version.json"; // GitHubのversion.jsonのURL
        private static string currentVersion = "2024.12.23"; // 現在のローカルバージョン


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
                    ClipCostumeSettingEditor.CheckForUpdate(remoteVersion, true);
                }
                else
                {
                    ClipCostumeSettingEditor.CheckForUpdate(remoteVersion, false);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Clip Costume] Failed to check for updates: {ex.Message}");
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