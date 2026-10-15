using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TdsOverlayImGui.Services
{
    public static class UpdateService
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static async Task<(bool isUpdateAvailable, string latestVersionTag, string releaseUrl, string errorKey)> CheckForUpdatesAsync(
            string owner, string repo, string currentVersionStr)
        {
            try
            {
                Version? bestVersion = null;
                string bestTag = "";
                string bestUrl = "";
                bool anySuccess = false;

                string[] atomFeeds = new[]
                {
                    $"https://github.com/{owner}/{repo}/tags.atom",
                    $"https://github.com/{owner}/{repo}/releases.atom"
                };

                foreach (var feedUrl in atomFeeds)
                {
                    try
                    {
                        using var feedReq = new HttpRequestMessage(HttpMethod.Get, feedUrl);
                        feedReq.Headers.UserAgent.ParseAdd("TdsOverlayImGui");
                        using var feedResp = await HttpClient.SendAsync(feedReq);
                        if (feedResp.IsSuccessStatusCode)
                        {
                            anySuccess = true;
                            string xml = await feedResp.Content.ReadAsStringAsync();
                            var matches = Regex.Matches(xml, @"releases/tag/([^""'\s<]+)");
                            foreach (Match match in matches)
                            {
                                if (match.Groups.Count > 1)
                                {
                                    string tag = match.Groups[1].Value;
                                    var ver = ParseCleanVersion(tag);
                                    if (ver != null && (bestVersion == null || ver > bestVersion))
                                    {
                                        bestVersion = ver;
                                        bestTag = tag;
                                        bestUrl = $"https://github.com/{owner}/{repo}/releases/tag/{tag}";
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (bestVersion == null)
                {
                    string releasesUrl = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=15";
                    string tagsUrl = $"https://api.github.com/repos/{owner}/{repo}/tags?per_page=15";

                    try
                    {
                        using var relReq = new HttpRequestMessage(HttpMethod.Get, releasesUrl);
                        relReq.Headers.UserAgent.ParseAdd("TdsOverlayImGui");
                        relReq.Headers.Accept.ParseAdd("application/vnd.github.v3+json");

                        using var relResp = await HttpClient.SendAsync(relReq);
                        if (relResp.IsSuccessStatusCode)
                        {
                            anySuccess = true;
                            string json = await relResp.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var elem in doc.RootElement.EnumerateArray())
                                {
                                    if (elem.TryGetProperty("draft", out var draftElem) && draftElem.GetBoolean())
                                    {
                                        continue;
                                    }

                                    if (elem.TryGetProperty("tag_name", out var tagElem))
                                    {
                                        string tag = tagElem.GetString() ?? "";
                                        var ver = ParseCleanVersion(tag);
                                        if (ver != null && (bestVersion == null || ver > bestVersion))
                                        {
                                            bestVersion = ver;
                                            bestTag = tag;
                                            bestUrl = elem.TryGetProperty("html_url", out var uElem)
                                                ? uElem.GetString() ?? $"https://github.com/{owner}/{repo}/releases/tag/{tag}"
                                                : $"https://github.com/{owner}/{repo}/releases/tag/{tag}";
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        using var tagsReq = new HttpRequestMessage(HttpMethod.Get, tagsUrl);
                        tagsReq.Headers.UserAgent.ParseAdd("TdsOverlayImGui");
                        tagsReq.Headers.Accept.ParseAdd("application/vnd.github.v3+json");

                        using var tagsResp = await HttpClient.SendAsync(tagsReq);
                        if (tagsResp.IsSuccessStatusCode)
                        {
                            anySuccess = true;
                            string json = await tagsResp.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var elem in doc.RootElement.EnumerateArray())
                                {
                                    if (elem.TryGetProperty("name", out var nameElem))
                                    {
                                        string tag = nameElem.GetString() ?? "";
                                        var ver = ParseCleanVersion(tag);
                                        if (ver != null && (bestVersion == null || ver > bestVersion))
                                        {
                                            bestVersion = ver;
                                            bestTag = tag;
                                            bestUrl = $"https://github.com/{owner}/{repo}/releases/tag/{tag}";
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (!anySuccess && bestVersion == null)
                {
                    return (false, "", "", "CheckUpdatesError");
                }

                if (bestVersion == null)
                {
                    return (false, "", "", "");
                }

                var currentVer = ParseCleanVersion(currentVersionStr);
                if (currentVer != null && bestVersion > currentVer)
                {
                    return (true, bestTag, bestUrl, "");
                }

                return (false, bestTag, bestUrl, "");
            }
            catch
            {
                return (false, "", "", "CheckUpdatesError");
            }
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
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
    }
}