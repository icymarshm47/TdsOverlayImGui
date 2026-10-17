using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TdsOverlayImGui.Services
{

    public static class StrategyService
    {
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string FolderPath = Path.Combine(BaseDir, "strategies");
        private static readonly string ImagesFolder = Path.Combine(BaseDir, "images");
        private static readonly string SettingsFilePath = Path.Combine(BaseDir, "settings.json");
        private static readonly string TasksFilePath = Path.Combine(BaseDir, "completed_tasks.json");

        private static readonly Regex UncheckedBoxRegex = new Regex(@"^(\s*[\-*]\s*)\[\s*\]", RegexOptions.Compiled);
        private static readonly Regex CheckedBoxRegex = new Regex(@"^(\s*[\-*]\s*)\[[xX]\]", RegexOptions.Compiled);

        public static AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings) ?? new AppSettings();

                if (settings.Language == "0" || settings.Language.Equals("English", StringComparison.OrdinalIgnoreCase))
                    settings.Language = "en";
                else if (settings.Language == "1" || settings.Language.Equals("Russian", StringComparison.OrdinalIgnoreCase))
                    settings.Language = "ru";

                Loc.CurrentLanguage = settings.Language;
                return settings;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, AppJsonContext.Default.AppSettings);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public static HashSet<string> LoadCompletedTasks()
        {
            if (!File.Exists(TasksFilePath))
                return new HashSet<string>();

            try
            {
                string json = File.ReadAllText(TasksFilePath);
                var list = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListString);
                return list != null ? new HashSet<string>(list) : new HashSet<string>();
            }
            catch
            {
                return new HashSet<string>();
            }
        }

        public static void SaveCompletedTasks(HashSet<string> tasks)
        {
            try
            {
                var list = new List<string>(tasks);
                string json = JsonSerializer.Serialize(list, AppJsonContext.Default.ListString);
                File.WriteAllText(TasksFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving tasks: {ex.Message}");
            }
        }

        public static List<MapStrategy> LoadStrategies()
        {
            var list = new List<MapStrategy>();

            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
                var defaultStrat = GetDefaultStrategy();
                SaveStrategy(defaultStrat);
                list.Add(defaultStrat);
                return list;
            }

            string[] files = Directory.GetFiles(FolderPath, "*.json");

            if (files.Length == 0)
            {
                var defaultStrat = GetDefaultStrategy();
                SaveStrategy(defaultStrat);
                list.Add(defaultStrat);
                return list;
            }

            var strats = new MapStrategy?[files.Length];

            Parallel.For(0, files.Length, i =>
            {
                try
                {
                    string file = files[i];
                    string json = File.ReadAllText(file);
                    var strat = JsonSerializer.Deserialize(json, AppJsonContext.Default.MapStrategy);
                    if (strat != null)
                    {
                        strat.FilePath = file;
                        strat.Steps ??= new();
                        strat.ImagePaths ??= new();
                        strat.MapName ??= "";
                        strat.Difficulty ??= "";
                        strat.StrategyName ??= "Main";
                        strat.GeneralInfo ??= "";
                        strat.EnsureDefaultPlayer();
                        foreach (var p in strat.Players)
                        {
                            p.Steps ??= new();
                            p.ImagePaths ??= new();
                            p.GeneralInfo ??= "";
                            foreach (var s in p.Steps)
                            {
                                s.Instruction ??= "";
                                s.ImagePaths ??= new();
                            }
                        }
                        strats[i] = strat;
                    }
                }
                catch { }
            });

            for (int i = 0; i < strats.Length; i++)
            {
                if (strats[i] != null)
                {
                    list.Add(strats[i]!);
                }
            }

            if (list.Count == 0)
            {
                var defaultStrat = GetDefaultStrategy();
                SaveStrategy(defaultStrat);
                list.Add(defaultStrat);
            }

            return list;
        }

        public static void SaveStrategy(MapStrategy strategy)
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                    Directory.CreateDirectory(FolderPath);

                strategy.EnsureDefaultPlayer();
                foreach (var p in strategy.Players)
                {
                    for (int i = 0; i < p.ImagePaths.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(p.ImagePaths[i]))
                        {
                            p.ImagePaths[i] = CopyImageToImagesFolder(p.ImagePaths[i], strategy.MapName, i + 1);
                        }
                    }
                }

                strategy.GeneralInfo = strategy.CurrentPlayer.GeneralInfo;
                strategy.ImagePaths = new List<string>(strategy.CurrentPlayer.ImagePaths);
                strategy.Steps = new List<StrategyStep>(strategy.CurrentPlayer.Steps);

                string safeFileName = SanitizeFileName($"{strategy.MapName}_{strategy.Difficulty}_{strategy.StrategyName}.json");
                string targetPath = Path.Combine(FolderPath, safeFileName);

                if (!string.IsNullOrEmpty(strategy.FilePath) && strategy.FilePath != targetPath && File.Exists(strategy.FilePath))
                {
                    File.Delete(strategy.FilePath);
                }

                strategy.FilePath = targetPath;

                string json = JsonSerializer.Serialize(strategy, AppJsonContext.Default.MapStrategy);
                File.WriteAllText(targetPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving strategy: {ex.Message}");
            }
        }

        public static void DeleteStrategy(MapStrategy strategy)
        {
            try
            {
                if (!string.IsNullOrEmpty(strategy.FilePath) && File.Exists(strategy.FilePath))
                {
                    File.Delete(strategy.FilePath);
                }

                string fullImagesDir = Path.GetFullPath(ImagesFolder);
                foreach (var imgPath in strategy.ImagePaths)
                {
                    if (string.IsNullOrWhiteSpace(imgPath)) continue;
                    string fullPath = Path.GetFullPath(imgPath);
                    if (File.Exists(fullPath) && fullPath.StartsWith(fullImagesDir, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Delete(fullPath);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        public static MapStrategy PrepareStrategyForExport(MapStrategy source, HashSet<string> completedTasks)
        {
            source.EnsureDefaultPlayer();
            string mapId = source.TaskId;
            var exportMap = new MapStrategy
            {
                MapName = source.MapName,
                Difficulty = source.Difficulty,
                StrategyName = source.StrategyName,
                GeneralInfo = ApplyCheckboxStatesToText(source.CurrentPlayer.GeneralInfo, $"gen_{mapId}_{source.CurrentPlayer.PlayerName}", completedTasks),
                ImagePaths = new List<string>(source.CurrentPlayer.ImagePaths),
                Steps = new List<StrategyStep>(),
                Players = new List<PlayerStrategy>()
            };

            for (int i = 0; i < source.CurrentPlayer.Steps.Count; i++)
            {
                var step = source.CurrentPlayer.Steps[i];
                exportMap.Steps.Add(new StrategyStep
                {
                    StartWave = step.StartWave,
                    EndWave = step.EndWave,
                    Instruction = ApplyCheckboxStatesToText(step.Instruction, $"{mapId}_{source.CurrentPlayer.PlayerName}_{i}", completedTasks),
                    ImagePaths = new List<string>(step.ImagePaths)
                });
            }

            foreach (var p in source.Players)
            {
                var ep = new PlayerStrategy
                {
                    PlayerName = p.PlayerName,
                    GeneralInfo = ApplyCheckboxStatesToText(p.GeneralInfo, $"gen_{mapId}_{p.PlayerName}", completedTasks),
                    ImagePaths = new List<string>(p.ImagePaths),
                    Steps = new List<StrategyStep>()
                };
                for (int i = 0; i < p.Steps.Count; i++)
                {
                    var step = p.Steps[i];
                    ep.Steps.Add(new StrategyStep
                    {
                        StartWave = step.StartWave,
                        EndWave = step.EndWave,
                        Instruction = ApplyCheckboxStatesToText(step.Instruction, $"{mapId}_{p.PlayerName}_{i}", completedTasks),
                        ImagePaths = new List<string>(step.ImagePaths)
                    });
                }
                exportMap.Players.Add(ep);
            }

            return exportMap;
        }

        private static string ApplyCheckboxStatesToText(string text, string keyPrefix, HashSet<string> completedTasks)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                string line = lines[lineIdx];
                string taskKey = $"{keyPrefix}_{lineIdx}";

                bool isCompleted = completedTasks.Contains(taskKey);

                if (isCompleted)
                {
                    if (UncheckedBoxRegex.IsMatch(line))
                    {
                        lines[lineIdx] = UncheckedBoxRegex.Replace(line, "${1}[x]");
                    }
                }
                else
                {
                    if (CheckedBoxRegex.IsMatch(line))
                    {
                        lines[lineIdx] = CheckedBoxRegex.Replace(line, "${1}[ ]");
                    }
                }
            }

            return string.Join("\n", lines);
        }

        public static bool ExportStrategyToZip(MapStrategy strategy, HashSet<string> completedTasks, string zipPath)
        {
            try
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                var exportMap = PrepareStrategyForExport(strategy, completedTasks);
                exportMap.ImagePaths = new List<string>();

                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    int imgCount = 1;
                    foreach (var imgPath in strategy.ImagePaths)
                    {
                        if (string.IsNullOrWhiteSpace(imgPath)) continue;

                        string fullPath = Path.GetFullPath(imgPath);
                        if (!File.Exists(fullPath) && File.Exists(imgPath))
                            fullPath = imgPath;

                        if (File.Exists(fullPath))
                        {
                            string ext = Path.GetExtension(fullPath);
                            string zipRelativePath = $"images/img_{imgCount}{ext}";

                            archive.CreateEntryFromFile(fullPath, zipRelativePath);
                            exportMap.ImagePaths.Add(zipRelativePath);
                            imgCount++;
                        }
                    }

                    string json = JsonSerializer.Serialize(exportMap, AppJsonContext.Default.MapStrategy);
                    var jsonEntry = archive.CreateEntry("strategy.json");
                    using (var writer = new StreamWriter(jsonEntry.Open()))
                    {
                        writer.Write(json);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export error: {ex.Message}");
                return false;
            }
        }

        public static string ExportStrategyToClipboardBase64(MapStrategy strategy, HashSet<string> completedTasks)
        {
            var exportMap = PrepareStrategyForExport(strategy, completedTasks);
            exportMap.ImagePaths = new List<string>();

            string json = JsonSerializer.Serialize(exportMap, AppJsonContext.Default.MapStrategy);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }

        public static bool ImportStrategyFromClipboardBase64(string text, out string statusMessage)
        {
            statusMessage = "";
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    statusMessage = Loc.Tr("ClipboardImportError");
                    return false;
                }

                string trimmed = text.Trim();
                string jsonText = "";

                if (!trimmed.StartsWith("{"))
                {
                    try
                    {
                        byte[] bytes = Convert.FromBase64String(trimmed);
                        jsonText = Encoding.UTF8.GetString(bytes);
                    }
                    catch
                    {
                        jsonText = trimmed;
                    }
                }
                else
                {
                    jsonText = trimmed;
                }

                var strat = JsonSerializer.Deserialize(jsonText, AppJsonContext.Default.MapStrategy);

                if (strat == null || string.IsNullOrWhiteSpace(strat.MapName))
                {
                    statusMessage = Loc.Tr("ClipboardImportError");
                    return false;
                }

                strat.Steps ??= new();
                strat.ImagePaths = new List<string>();
                strat.MapName ??= "";
                strat.Difficulty ??= "";
                strat.StrategyName ??= "Main";
                strat.GeneralInfo ??= "";
                foreach (var s in strat.Steps)
                {
                    s.Instruction ??= "";
                    s.ImagePaths ??= new();
                }
                SaveStrategy(strat);

                statusMessage = $"{Loc.Tr("ClipboardImportSuccess")}\n({strat.DisplayName})";
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Import error: {ex.Message}";
                return false;
            }
        }

        public static bool ImportStrategy(string importPath, out string statusMessage)
        {
            statusMessage = "";
            try
            {
                if (!File.Exists(importPath))
                {
                    statusMessage = "File not found at specified path!";
                    return false;
                }

                string ext = Path.GetExtension(importPath).ToLower();

                if (ext == ".json")
                {
                    string jsonText = File.ReadAllText(importPath);
                    return ImportStrategyFromClipboardBase64(jsonText, out statusMessage);
                }
                else if (ext == ".zip")
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "tds_import_" + Guid.NewGuid().ToString("N"));
                    try
                    {
                        ZipFile.ExtractToDirectory(importPath, tempDir);

                        string jsonFile = Path.Combine(tempDir, "strategy.json");
                        if (!File.Exists(jsonFile))
                        {
                            var jsonFiles = Directory.GetFiles(tempDir, "*.json", SearchOption.AllDirectories);
                            if (jsonFiles.Length > 0) jsonFile = jsonFiles[0];
                        }

                        if (!File.Exists(jsonFile))
                        {
                            statusMessage = "strategy.json not found in ZIP archive!";
                            return false;
                        }

                        string jsonText = File.ReadAllText(jsonFile);
                        var strat = JsonSerializer.Deserialize(jsonText, AppJsonContext.Default.MapStrategy);

                        if (strat == null)
                        {
                            statusMessage = "Failed to read strategy format!";
                            return false;
                        }

                        strat.Steps ??= new();
                        strat.ImagePaths ??= new();
                        strat.MapName ??= "";
                        strat.Difficulty ??= "";
                        strat.StrategyName ??= "Main";
                        strat.GeneralInfo ??= "";
                        foreach (var s in strat.Steps)
                        {
                            s.Instruction ??= "";
                            s.ImagePaths ??= new();
                        }

                        string fullTempDir = Path.GetFullPath(tempDir);
                        var importedImages = new List<string>();
                        for (int i = 0; i < strat.ImagePaths.Count; i++)
                        {
                            string zipImgRelPath = strat.ImagePaths[i];
                            if (string.IsNullOrWhiteSpace(zipImgRelPath)) continue;

                            string tempImgFullPath = Path.GetFullPath(Path.Combine(tempDir, zipImgRelPath));
                            if (!tempImgFullPath.StartsWith(fullTempDir, StringComparison.OrdinalIgnoreCase)) continue;

                            if (File.Exists(tempImgFullPath))
                            {
                                if (!Directory.Exists(ImagesFolder)) Directory.CreateDirectory(ImagesFolder);

                                string imgExt = Path.GetExtension(tempImgFullPath);
                                string destImgName = $"{SanitizeFileName(strat.MapName)}_imported_{Guid.NewGuid().ToString("N")[..6]}{imgExt}";
                                string destImgPath = Path.Combine(ImagesFolder, destImgName);

                                File.Copy(tempImgFullPath, destImgPath, overwrite: true);
                                importedImages.Add(destImgPath);
                            }
                        }

                        strat.ImagePaths = importedImages;
                        SaveStrategy(strat);

                        statusMessage = $"Strategy '{strat.DisplayName}' successfully imported!";
                        return true;
                    }
                    finally
                    {
                        if (Directory.Exists(tempDir))
                        {
                            try { Directory.Delete(tempDir, true); } catch { }
                        }
                    }
                }
                else
                {
                    statusMessage = "Only .zip or .json files are supported!";
                    return false;
                }
            }
            catch (Exception ex)
            {
                statusMessage = $"Import error: {ex.Message}";
                return false;
            }
        }

        private static string CopyImageToImagesFolder(string sourcePath, string mapName, int imgNum)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return sourcePath;

            if (!Directory.Exists(ImagesFolder))
                Directory.CreateDirectory(ImagesFolder);

            string fullSource = Path.GetFullPath(sourcePath);
            string fullImagesDir = Path.GetFullPath(ImagesFolder);

            if (fullSource.StartsWith(fullImagesDir, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(".", sourcePath);
            }

            try
            {
                string ext = Path.GetExtension(sourcePath);
                string cleanMap = SanitizeFileName(mapName);
                string newFileName = $"{cleanMap}_img{imgNum}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
                string destPath = Path.Combine(ImagesFolder, newFileName);

                File.Copy(sourcePath, destPath, overwrite: true);
                return destPath;
            }
            catch
            {
                return sourcePath;
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        private static MapStrategy GetDefaultStrategy()
        {
            return new MapStrategy
            {
                MapName = "Fallen Ground",
                Difficulty = "Fallen",
                StrategyName = "Solo Accelerator",
                GeneralInfo = "Loadout: Scout, Farm, Commander, DJ, Accelerator.\nNote: Max farms to level 3 first.",
                ImagePaths = new List<string>(),
                Steps = new List<StrategyStep>
                {
                    new StrategyStep
                    {
                        StartWave = 1,
                        EndWave = 5,
                        Instruction = "- [ ] Place 2x Scouts at the first turn, upgrade to Lv 2."
                    },
                    new StrategyStep
                    {
                        StartWave = 6,
                        EndWave = 30,
                        Instruction = "- [ ] Place Lv 1 Farm in the corner, save money for Accelerator."
                    }
                }
            };
        }
    }
}
