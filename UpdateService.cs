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
        private static readonly HttpClient HttpClient = new();

        /// <summary>
        /// Сканирует страницу релизов GitHub и находит самую верхнюю (самую новую) версию
        /// </summary>
        public static async Task<(bool isUpdateAvailable, string latestVersionTag, string releaseUrl, string errorMessage)> CheckForUpdatesAsync(
            string owner, string repo, string currentVersionStr)
        {
            try
            {
                // Страница со списком всех релизов
                string releasesPageUrl = $"https://github.com/{owner}/{repo}/releases";

                var request = new HttpRequestMessage(HttpMethod.Get, releasesPageUrl);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var response = await HttpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, "", "", "Репозиторий не найден или он приватный (Private)!");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (false, "", "", $"Ошибка доступа к GitHub: {response.StatusCode}");
                }

                string html = await response.Content.ReadAsStringAsync();

                // Ищем самый первый (самый свежий) тег релиза на странице через Regex
                var match = Regex.Match(html, $@"/{Regex.Escape(owner)}/{Regex.Escape(repo)}/releases/tag/([^""\s?#]+)", RegexOptions.IgnoreCase);

                if (!match.Success)
                {
                    return (false, "", "", "На странице не найдено опубликованных версий (проверьте, не сохранен ли релиз как 'Draft').");
                }

                string tagName = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
                string releaseUrl = $"https://github.com/{owner}/{repo}/releases/tag/{tagName}";

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