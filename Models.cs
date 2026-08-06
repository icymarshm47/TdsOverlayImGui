using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TdsOverlayImGui
{
    public static class Loc
    {
        private const string LocalesFolder = "locales";
        private static readonly Dictionary<string, Dictionary<string, string>> Languages = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase);

        public static string CurrentLanguage = "en";

        public static void LoadLanguages()
        {
            Languages.Clear();
            DisplayNames.Clear();

            if (!Directory.Exists(LocalesFolder))
            {
                Directory.CreateDirectory(LocalesFolder);
            }

            string[] files = Directory.GetFiles(LocalesFolder, "*.json");

            foreach (var file in files)
            {
                try
                {
                    string langCode = Path.GetFileNameWithoutExtension(file).ToLower();
                    string json = File.ReadAllText(file);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (dict != null)
                    {
                        Languages[langCode] = dict;
                        string name = dict.TryGetValue("LanguageName", out var langName) ? langName : langCode.ToUpper();
                        DisplayNames[langCode] = name;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading locale file {file}: {ex.Message}");
                }
            }

            // Запасной вариант если папка была пуста
            if (Languages.Count == 0)
            {
                Languages["en"] = new Dictionary<string, string>();
                DisplayNames["en"] = "English";
            }
        }

        public static List<string> GetAvailableLanguages()
        {
            return new List<string>(Languages.Keys);
        }

        public static string GetLanguageDisplayName(string langCode)
        {
            if (DisplayNames.TryGetValue(langCode, out var name))
                return name;
            return langCode.ToUpper();
        }

        public static string Tr(string key)
        {
            if (Languages.TryGetValue(CurrentLanguage, out var dict))
            {
                if (dict.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val))
                    return val;
            }

            // Фолбэк на английский
            if (Languages.TryGetValue("en", out var enDict))
            {
                if (enDict.TryGetValue(key, out var enVal) && !string.IsNullOrEmpty(enVal))
                    return enVal;
            }

            return key;
        }
    }

    public class StrategyStep
    {
        public int StartWave { get; set; } = 1;
        public int EndWave { get; set; } = 30;
        public string Instruction { get; set; } = "";
        public List<string> ImagePaths { get; set; } = new();

        [JsonPropertyName("ImagePath")]
        public string? LegacyImagePath
        {
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && ImagePaths.Count == 0)
                    ImagePaths.Add(value);
            }
        }
    }

    public class MapStrategy
    {
        public string MapName { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string StrategyName { get; set; } = "Main";
        public string GeneralInfo { get; set; } = "";
        public List<string> ImagePaths { get; set; } = new();

        public List<StrategyStep> Steps { get; set; } = new();

        [JsonIgnore]
        public string? FilePath { get; set; }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(StrategyName)
            ? $"{MapName} [{Difficulty}]"
            : $"{MapName} [{Difficulty}] - {StrategyName}";
    }
}