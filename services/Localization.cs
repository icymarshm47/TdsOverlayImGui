using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TdsOverlayImGui.Models;

namespace TdsOverlayImGui.Services
{
    public static class Loc
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Languages = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, string>? _activeLangDict;
        private static Dictionary<string, string>? _enLangDict;

        private static string _currentLanguage = "en";
        public static string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                UpdateCachedDictionaries();
            }
        }

        public static void LoadLanguages()
        {
            Languages.Clear();
            DisplayNames.Clear();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string localesPath = Path.Combine(baseDir, "locales");

            if (!Directory.Exists(localesPath))
            {
                localesPath = Path.GetFullPath("locales");
                if (!Directory.Exists(localesPath))
                {
                    Directory.CreateDirectory(localesPath);
                }
            }

            string[] files = Directory.GetFiles(localesPath, "*.json");

            foreach (var file in files)
            {
                try
                {
                    string langCode = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    string json = File.ReadAllText(file);
                    var dict = JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString);

                    if (dict != null)
                    {
                        Languages[langCode] = dict;
                        string name = dict.TryGetValue("LanguageName", out var langName) ? langName : langCode.ToUpperInvariant();
                        DisplayNames[langCode] = name;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading locale file {file}: {ex.Message}");
                }
            }

            if (Languages.Count == 0)
            {
                Languages["en"] = new Dictionary<string, string>();
                DisplayNames["en"] = "English";
            }

            UpdateCachedDictionaries();
        }

        private static void UpdateCachedDictionaries()
        {
            Languages.TryGetValue(_currentLanguage, out _activeLangDict);
            Languages.TryGetValue("en", out _enLangDict);
        }

        public static List<string> GetAvailableLanguages()
        {
            return new List<string>(Languages.Keys);
        }

        public static string GetLanguageDisplayName(string langCode)
        {
            if (DisplayNames.TryGetValue(langCode, out var name))
                return name;
            return langCode.ToUpperInvariant();
        }

        public static string Tr(string key)
        {
            if (_activeLangDict != null && _activeLangDict.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val))
                return val;

            if (_enLangDict != null && _enLangDict.TryGetValue(key, out var enVal) && !string.IsNullOrEmpty(enVal))
                return enVal;

            return key;
        }
    }
}
