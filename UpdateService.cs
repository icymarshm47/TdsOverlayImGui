using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TdsOverlayImGui
{
    public static class UpdateService
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Проверяет наличие новых релизов через официальный GitHub REST API
        /// </summary>
        public static async Task<(bool isUpdateAvailable, string latestVersionTag, string releaseUrl, string errorMessage)> CheckForUpdatesAsync(
            string owner, string repo, string currentVersionStr)
        {
            try
            {
                string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.UserAgent.ParseAdd("TdsOverlayImGui");
                request.Headers.Accept.ParseAdd("application/vnd.github.v3+json");

                using var response = await HttpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, "", "", "Репозиторий не найден, приватный или не содержит релизов.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (false, "", "", $"Ошибка GitHub API: {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("tag_name", out var tagElem))
                {
                    return (false, "", "", "Тег релиза не найден в ответе GitHub API.");
                }

                string tagName = tagElem.GetString() ?? "";
                string releaseUrl = root.TryGetProperty("html_url", out var urlElem)
                    ? urlElem.GetString() ?? $"https://github.com/{owner}/{repo}/releases/tag/{tagName}"
                    : $"https://github.com/{owner}/{repo}/releases/tag/{tagName}";

                var latestVer = ParseCleanVersion(tagName);
                var currentVer = ParseCleanVersion(currentVersionStr);

                if (latestVer != null && currentVer != null)
                {
                    if (latestVer > currentVer)
                    {
                        return (true, tagName, releaseUrl, "");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, "", "", $"Ошибка подключения: {ex.Message}");
            }

            return (false, "", "", "");
        }

        private static Version? ParseCleanVersion(string versionStr)
        {
            if (string.IsNullOrWhiteSpace(versionStr)) return null;

            string v = versionStr.TrimStart('v', 'V').Trim();

            int dashIdx = v.IndexOf('-');
            if (dashIdx >= 0)
            {
                v = v.Substring(0, dashIdx);
            }

            string[] parts = v.Split('.');
            if (parts.Length == 1)
            {
                v += ".0";
            }

            if (Version.TryParse(v, out var result))
            {
                return result;
            }

            return null;
        }

        public static void OpenUrlInBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open URL: {ex.Message}");
            }
        }
    }
}