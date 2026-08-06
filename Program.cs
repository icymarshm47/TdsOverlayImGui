using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ClickableTransparentOverlay;
using ImGuiNET;

namespace TdsOverlayImGui
{
    public class TdsImGuiOverlay : Overlay
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        private const string GitHubOwner = "icymarshm47";
        private const string GitHubRepo = "TdsOverlayImGui";
        private const string CurrentAppVersion = "0.4";

        private List<MapStrategy> _strategies = new();
        private AppSettings _settings = new();

        private int _selectedMapIndex = -1;
        private int _currentStepIndex = 0;
        private int _currentImageIndex = 0;

        private bool _isOverlayOpen = true;
        private bool _isFirstFrame = true;

        private bool _showUpdateModal = false;
        private string _latestVersionTag = "";
        private string _releaseUrl = "";
        private string _manualCheckMessage = "";
        private float _manualCheckMessageTimer = 0.0f;

        private HashSet<string> _completedTasks = new();
        private int _currentWaveNumber = 1;
        private int _previousWaveNumber = 1;

        // DJ Toast & Audio Notifications
        private HashSet<string> _triggeredDjAlerts = new();
        private HashSet<string> _triggeredOcrSounds = new();
        private float _djToastTimer = 0.0f;
        private string _djToastMessage = "";
        private Vector4 _djToastColor = Vector4.One;

        // Regex for <ocr N color>
        private static readonly Regex OcrTagRegex = new Regex(@"<ocr\s+([\d\-]+)(?:\s+(red|green|purple))?\s*>(.*?)</ocr[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex InlineMarkdownRegex = new Regex(@"(\*\*.*?\*\*|\*.*?\*|~~.*?~~|`.*?`)", RegexOptions.Compiled);

        private float _toastTimer = 0.0f;

        private float _imageScale = 1.0f;
        private Vector2 _imageOffset = Vector2.Zero;

        private int? _detectedWaveNumber = null;
        private bool _isSelectingOcrRegion = false;
        private int _ocrSelectionState = 0;
        private Vector2 _ocrDragStart = Vector2.Zero;
        private DateTime _lastOcrScanTime = DateTime.MinValue;

        private bool _isEditing = false;
        private string _editMapName = "";
        private string _editDifficulty = "";
        private string _editStrategyName = "";
        private string _editGeneralInfo = "";

        private bool _showAddMapModal = false;
        private string _newMapName = "";
        private string _newMapDiff = "Fallen";
        private string _newMapStrat = "Solo";

        private bool _showImportExportModal = false;
        private string _importStatusMessage = "";
        private string _exportStatusMessage = "";

        private bool _showSettingsModal = false;
        private bool _showAboutModal = false;
        private bool _showHelpModal = false;
        private bool _showDeleteConfirmModal = false;

        public TdsImGuiOverlay() : base("TDS Strategy Overlay", true)
        {
            // Загружаем языковые файлы из папки locales/
            Loc.LoadLanguages();

            string[] fontCandidates = new[]
            {
                "Roboto-Regular.ttf",
                "roboto.ttf",
                Path.Combine("fonts", "Roboto-Regular.ttf"),
                @"C:\Windows\Fonts\Roboto-Regular.ttf",
                @"C:\Windows\Fonts\segoeui.ttf",
                @"C:\Windows\Fonts\arial.ttf"
            };

            string? selectedFont = null;
            foreach (var candidate in fontCandidates)
            {
                if (File.Exists(candidate))
                {
                    selectedFont = candidate;
                    break;
                }
            }

            if (selectedFont != null)
            {
                ReplaceFont(selectedFont, 18, FontGlyphRangeType.Cyrillic);
            }

            _settings = StrategyService.LoadSettings();
            _strategies = StrategyService.LoadStrategies();
            _completedTasks = StrategyService.LoadCompletedTasks();
            _selectedMapIndex = -1;

            CheckForUpdatesInBackground(silent: true);
        }

        public static void SetAlwaysOnTop(bool enable)
        {
            IntPtr hWnd = FindWindow(null, "TDS Strategy Overlay");
            if (hWnd != IntPtr.Zero)
            {
                IntPtr insertAfter = enable ? HWND_TOPMOST : HWND_NOTOPMOST;
                SetWindowPos(hWnd, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        private static void PlayOcrBeep()
        {
            Task.Run(() =>
            {
                try
                {
                    Console.Beep(880, 100);  // A5
                    Console.Beep(1175, 180); // D6
                }
                catch { }
            });
        }

        private void OnWaveChanged(int newWave)
        {
            if (newWave < _previousWaveNumber)
            {
                RemoveTriggersForWaveOrHigher(_triggeredOcrSounds, newWave);
                RemoveTriggersForWaveOrHigher(_triggeredDjAlerts, newWave);
            }
            _previousWaveNumber = newWave;
        }

        private static void RemoveTriggersForWaveOrHigher(HashSet<string> set, int targetWave)
        {
            set.RemoveWhere(key =>
            {
                var parts = key.Split('_');
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    if (int.TryParse(parts[i], out int w))
                    {
                        return w >= targetWave;
                    }
                }
                return false;
            });
        }

        private static void SafeTextColored(Vector4 color, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
        }

        private static void SafeTextWrapped(Vector4 color, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextWrapped(text);
            ImGui.PopStyleColor();
        }

        private void CheckForUpdatesInBackground(bool silent = true)
        {
            Task.Run(async () =>
            {
                var (available, latestTag, url, error) = await UpdateService.CheckForUpdatesAsync(
                    GitHubOwner, GitHubRepo, CurrentAppVersion);

                if (available)
                {
                    _latestVersionTag = latestTag;
                    _releaseUrl = url;
                    _showUpdateModal = true;
                }
                else if (!silent)
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        _manualCheckMessage = error;
                    }
                    else
                    {
                        _manualCheckMessage = Loc.Tr("NoUpdatesNotice");
                    }
                    _manualCheckMessageTimer = 5.0f;
                }
            });
        }

        protected override void Render()
        {
            if (_isFirstFrame)
            {
                SetAlwaysOnTop(true);
                _isFirstFrame = false;
            }

            if (!_isOverlayOpen)
            {
                Close();
                return;
            }

            SetupStyle();
            ProcessBackgroundOcr();

            ImGui.SetNextWindowSize(new Vector2(520, 600), ImGuiCond.FirstUseEver);

            ImGui.Begin("TDS Strategy Overlay (ImGui)", ref _isOverlayOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu(Loc.Tr("File")))
                {
                    if (ImGui.MenuItem(Loc.Tr("ImportExport")))
                    {
                        _showImportExportModal = true;
                        _importStatusMessage = "";
                        _exportStatusMessage = "";
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.Tr("Settings")))
                    {
                        _showSettingsModal = true;
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu(Loc.Tr("Other")))
                {
                    bool isCompact = _settings.CompactMode;
                    if (ImGui.MenuItem(Loc.Tr("CompactMode"), "", ref isCompact))
                    {
                        _settings.CompactMode = isCompact;
                        StrategyService.SaveSettings(_settings);
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.Tr("Help")))
                    {
                        _showHelpModal = true;
                    }

                    if (ImGui.MenuItem(Loc.Tr("CheckUpdates")))
                    {
                        _manualCheckMessage = "Checking...";
                        _manualCheckMessageTimer = 5.0f;
                        CheckForUpdatesInBackground(silent: false);
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.Tr("About")))
                    {
                        _showAboutModal = true;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            if (!string.IsNullOrEmpty(_manualCheckMessage))
            {
                SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), _manualCheckMessage);
                _manualCheckMessageTimer -= ImGui.GetIO().DeltaTime;
                if (_manualCheckMessageTimer <= 0.0f)
                {
                    _manualCheckMessage = "";
                }
            }

            ImGui.Spacing();

            if (_strategies.Count == 0)
            {
                ImGui.Text("No strategies found.");
                if (ImGui.Button(Loc.Tr("AddStrategy"), new Vector2(-1, 0)))
                {
                    _showAddMapModal = true;
                }
            }
            else
            {
                RenderMainUI();
            }

            ImGui.End();

            if (_settings.SeparateImageWindow) RenderSeparateImageWindow();
            if (_isSelectingOcrRegion) RenderOcrSelectionOverlay();
            
            RenderDjToast();

            if (_showAddMapModal) RenderAddMapModal();
            if (_showImportExportModal) RenderImportExportModal();
            if (_showSettingsModal) RenderSettingsModal();
            if (_showAboutModal) RenderAboutModal();
            if (_showHelpModal) RenderHelpModal();
            if (_showDeleteConfirmModal) RenderDeleteConfirmModal();
            if (_showUpdateModal) RenderUpdateModal();
        }

        private void SetupStyle()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 8.0f;
            style.FrameRounding = 6.0f;
            style.PopupRounding = 6.0f;
            style.ScrollbarRounding = 6.0f;

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.07f, 0.08f, 0.09f, _settings.WindowOpacity);
            colors[(int)ImGuiCol.Header] = new Vector4(0.18f, 0.19f, 0.22f, 1.0f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.28f, 0.32f, 0.77f, 1.0f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.22f, 0.25f, 0.60f, 1.0f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.14f, 0.15f, 0.17f, 1.0f);
        }

        private void ProcessBackgroundOcr()
        {
            if (!_settings.EnableOcr || _selectedMapIndex < 0 || _selectedMapIndex >= _strategies.Count) return;

            if ((DateTime.Now - _lastOcrScanTime).TotalSeconds >= 1.5)
            {
                _lastOcrScanTime = DateTime.Now;

                Task.Run(async () =>
                {
                    int? wave = await OcrService.RecognizeWaveFromScreenAsync(
                        _settings.OcrX, _settings.OcrY, _settings.OcrW, _settings.OcrH);

                    if (wave.HasValue)
                    {
                        if (wave.Value < _currentWaveNumber && wave.Value <= 2)
                        {
                            _triggeredDjAlerts.Clear();
                            _triggeredOcrSounds.Clear();
                        }

                        OnWaveChanged(wave.Value);
                        _detectedWaveNumber = wave.Value;
                        _currentWaveNumber = wave.Value;

                        var currentMap = _strategies[_selectedMapIndex];
                        UpdateStepByWaveNumber(currentMap, wave.Value);
                    }
                });
            }
        }

        private void UpdateStepByWaveNumber(MapStrategy map, int waveNumber)
        {
            for (int i = 0; i < map.Steps.Count; i++)
            {
                var s = map.Steps[i];
                if (waveNumber >= s.StartWave && waveNumber <= s.EndWave)
                {
                    if (_currentStepIndex != i)
                    {
                        _currentStepIndex = i;
                        ResetImageTransform();
                    }
                    break;
                }
            }
        }

        private void TriggerDjToast(string color)
        {
            _djToastTimer = 3.0f;
            if (color == "red")
            {
                _djToastMessage = Loc.Tr("DjToastRed");
                _djToastColor = new Vector4(1.0f, 0.2f, 0.2f, 1.0f);
            }
            else if (color == "green")
            {
                _djToastMessage = Loc.Tr("DjToastGreen");
                _djToastColor = new Vector4(0.2f, 1.0f, 0.2f, 1.0f);
            }
            else if (color == "purple")
            {
                _djToastMessage = Loc.Tr("DjToastPurple");
                _djToastColor = new Vector4(0.7f, 0.2f, 1.0f, 1.0f);
            }
        }

        private void RenderDjToast()
        {
            if (_djToastTimer > 0)
            {
                _djToastTimer -= ImGui.GetIO().DeltaTime;
                
                var io = ImGui.GetIO();
                ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.25f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
                
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.09f, 0.95f));
                ImGui.PushStyleColor(ImGuiCol.Border, _djToastColor);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(25, 18));

                ImGui.Begin("DjToastWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing);
                
                ImGui.SetWindowFontScale(1.4f);
                ImGui.TextColored(_djToastColor, _djToastMessage);
                ImGui.SetWindowFontScale(1.0f);

                ImGui.End();

                ImGui.PopStyleVar(3);
                ImGui.PopStyleColor(2);
            }
        }

        private static bool IsMarkdownCheckbox(string line, out bool isCheckedInText, out string cleanText)
        {
            isCheckedInText = false;
            cleanText = line;

            string trimmed = line.TrimStart();

            if (trimmed.StartsWith("- [ ] ") || trimmed.StartsWith("* [ ] "))
            {
                isCheckedInText = false;
                cleanText = trimmed.Substring(6);
                return true;
            }

            if (trimmed.StartsWith("- [x] ") || trimmed.StartsWith("- [X] ") ||
                trimmed.StartsWith("* [x] ") || trimmed.StartsWith("* [X] "))
            {
                isCheckedInText = true;
                cleanText = trimmed.Substring(6);
                return true;
            }

            return false;
        }

        private static float GetIndentPixels(string line)
        {
            float pixels = 0;
            foreach (char c in line)
            {
                if (c == '\t') pixels += 20.0f;
                else if (c == ' ') pixels += 5.0f;
                else break;
            }
            return pixels;
        }

        private void RenderMarkdownFormattedText(string text, bool isTagActive, bool isDone, int headingLevel)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (isTagActive && !isDone)
            {
                SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), "[WAVE!] ");
                ImGui.SameLine(0, 0);
            }

            if (headingLevel == 1)
            {
                ImGui.SetWindowFontScale(1.3f);
                SafeTextColored(new Vector4(1.0f, 0.85f, 0.3f, 1.0f), text);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.Separator();
                return;
            }
            else if (headingLevel == 2)
            {
                ImGui.SetWindowFontScale(1.15f);
                SafeTextColored(new Vector4(0.35f, 0.75f, 1.0f, 1.0f), text);
                ImGui.SetWindowFontScale(1.0f);
                return;
            }
            else if (headingLevel == 3)
            {
                ImGui.SetWindowFontScale(1.05f);
                SafeTextColored(new Vector4(0.85f, 0.5f, 1.0f, 1.0f), text);
                ImGui.SetWindowFontScale(1.0f);
                return;
            }

            if (isDone)
            {
                SafeTextWrapped(new Vector4(0.3f, 1.0f, 0.4f, 0.85f), text);
                return;
            }

            if (!InlineMarkdownRegex.IsMatch(text))
            {
                Vector4 plainColor = isTagActive ? new Vector4(0.3f, 1.0f, 0.4f, 1.0f) : new Vector4(0.92f, 0.92f, 0.96f, 1.0f);
                SafeTextWrapped(plainColor, text);
                return;
            }

            string[] parts = InlineMarkdownRegex.Split(text);

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrEmpty(part)) continue;

                if (i > 0)
                {
                    ImGui.SameLine(0, 0);
                }

                Vector4 color = isTagActive ? new Vector4(0.3f, 1.0f, 0.4f, 1.0f) : new Vector4(0.92f, 0.92f, 0.96f, 1.0f);
                string content = part;

                if (part.StartsWith("**") && part.EndsWith("**") && part.Length >= 4)
                {
                    content = part.Substring(2, part.Length - 4);
                    color = new Vector4(1.0f, 0.90f, 0.35f, 1.0f);
                }
                else if (part.StartsWith("*") && part.EndsWith("*") && part.Length >= 2)
                {
                    content = part.Substring(1, part.Length - 2);
                    color = new Vector4(0.70f, 0.85f, 1.0f, 1.0f);
                }
                else if (part.StartsWith("~~") && part.EndsWith("~~") && part.Length >= 4)
                {
                    content = part.Substring(2, part.Length - 4);
                    color = new Vector4(0.55f, 0.58f, 0.62f, 0.8f);
                }
                else if (part.StartsWith("`") && part.EndsWith("`") && part.Length >= 2)
                {
                    content = "[" + part.Substring(1, part.Length - 2) + "]";
                    color = new Vector4(0.35f, 0.95f, 0.85f, 1.0f);
                }

                SafeTextColored(color, content);
            }
        }

        private void RenderInstructionLine(string rawLine, int activeWave, string taskKey)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                ImGui.Spacing();
                return;
            }

            float indentPixels = GetIndentPixels(rawLine);
            if (indentPixels > 0)
            {
                ImGui.Indent(indentPixels);
            }

            bool hasCheckbox = IsMarkdownCheckbox(rawLine, out bool defaultCheckedInText, out string lineAfterCheck);

            var ocrMatches = OcrTagRegex.Matches(lineAfterCheck);
            bool isTagActive = false;
            string displayText = lineAfterCheck;

            if (ocrMatches.Count > 0)
            {
                foreach (Match ocrMatch in ocrMatches)
                {
                    string waveSpec = ocrMatch.Groups[1].Value.Trim();
                    string djColor = ocrMatch.Groups[2].Value.Trim().ToLower();

                    int startW = 0, endW = 0;
                    if (waveSpec.Contains('-'))
                    {
                        var parts = waveSpec.Split('-');
                        int.TryParse(parts[0], out startW);
                        int.TryParse(parts[1], out endW);
                    }
                    else
                    {
                        int.TryParse(waveSpec, out startW);
                        endW = startW;
                    }

                    if (activeWave >= startW && activeWave <= endW && startW > 0)
                    {
                        isTagActive = true;

                        string soundKey = $"{taskKey}_{activeWave}";
                        if (_triggeredOcrSounds.Add(soundKey))
                        {
                            PlayOcrBeep();
                        }

                        if (!string.IsNullOrEmpty(djColor))
                        {
                            string alertKey = $"{taskKey}_{activeWave}_{djColor}";
                            if (_triggeredDjAlerts.Add(alertKey))
                            {
                                TriggerDjToast(djColor);
                            }
                        }
                    }
                }

                // Заменяем конструкции <ocr N>текст</ocr> на их внутреннее содержимое, сохраняя весь внешний текст
                displayText = OcrTagRegex.Replace(lineAfterCheck, "$3");
            }

            if (!hasCheckbox)
            {
                if (IsMarkdownCheckbox(displayText, out bool checkInside, out string textInside))
                {
                    hasCheckbox = true;
                    defaultCheckedInText = checkInside;
                    displayText = textInside;
                }
            }

            string trimmedText = displayText.TrimStart();
            int headingLevel = 0;
            if (trimmedText.StartsWith("# ")) { headingLevel = 1; displayText = trimmedText.Substring(2); }
            else if (trimmedText.StartsWith("## ")) { headingLevel = 2; displayText = trimmedText.Substring(3); }
            else if (trimmedText.StartsWith("### ")) { headingLevel = 3; displayText = trimmedText.Substring(4); }

            if (hasCheckbox)
            {
                bool isDone = _completedTasks.Contains(taskKey) || defaultCheckedInText;

                ImGui.PushID($"MdTask_{taskKey}");

                if (ImGui.Checkbox("##MdCheck", ref isDone))
                {
                    if (isDone)
                        _completedTasks.Add(taskKey);
                    else
                        _completedTasks.Remove(taskKey);

                    StrategyService.SaveCompletedTasks(_completedTasks);
                }

                ImGui.SameLine();

                RenderMarkdownFormattedText(displayText, isTagActive, isDone, headingLevel);

                ImGui.PopID();
            }
            else
            {
                RenderMarkdownFormattedText(displayText, isTagActive, false, headingLevel);
            }

            if (indentPixels > 0)
            {
                ImGui.Unindent(indentPixels);
            }
        }

        private void RenderMainUI()
        {
            if (!_settings.CompactMode)
            {
                ImGui.BeginChild("MapSelectorCard", new Vector2(0, 92), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

                ImGui.TextDisabled(Loc.Tr("SelectStrategyHeader"));

                string[] comboItems = new string[_strategies.Count + 1];
                comboItems[0] = Loc.Tr("SelectStrategyCombo");
                for (int i = 0; i < _strategies.Count; i++)
                {
                    comboItems[i + 1] = _strategies[i].DisplayName;
                }

                int currentComboIdx = 0;
                if (_selectedMapIndex >= 0 && _selectedMapIndex < _strategies.Count)
                {
                    currentComboIdx = _selectedMapIndex + 1;
                }

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.Combo("##MapSelect", ref currentComboIdx, comboItems, comboItems.Length))
                {
                    if (currentComboIdx > 0 && currentComboIdx <= _strategies.Count)
                    {
                        _selectedMapIndex = currentComboIdx - 1;
                    }
                    else
                    {
                        _selectedMapIndex = -1;
                    }
                    _currentStepIndex = 0;
                    _currentImageIndex = 0;
                    _isEditing = false;
                    _triggeredDjAlerts.Clear();
                    _triggeredOcrSounds.Clear();
                    _previousWaveNumber = 1;
                    ResetImageTransform();
                }

                float availWCard = ImGui.GetContentRegionAvail().X;
                float spacingCard = ImGui.GetStyle().ItemSpacing.X;

                if (_selectedMapIndex >= 0 && _selectedMapIndex < _strategies.Count)
                {
                    float btnW = (availWCard - spacingCard * 2) / 3.0f;

                    if (ImGui.Button(Loc.Tr("AddStrategy"), new Vector2(btnW, 0)))
                    {
                        _showAddMapModal = true;
                    }

                    ImGui.SameLine();
                    var map = _strategies[_selectedMapIndex];
                    string editBtnText = _isEditing ? Loc.Tr("ViewMode") : Loc.Tr("EditStrategy");
                    if (ImGui.Button(editBtnText, new Vector2(btnW, 0)))
                    {
                        _isEditing = !_isEditing;
                        if (_isEditing)
                        {
                            _editMapName = map.MapName;
                            _editDifficulty = map.Difficulty;
                            _editStrategyName = map.StrategyName;
                            _editGeneralInfo = map.GeneralInfo;
                        }
                    }

                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Tr("DeleteStrategy"), new Vector2(btnW, 0)))
                    {
                        _showDeleteConfirmModal = true;
                    }
                }
                else
                {
                    if (ImGui.Button(Loc.Tr("AddStrategy"), new Vector2(availWCard, 0)))
                    {
                        _showAddMapModal = true;
                    }
                }

                ImGui.EndChild();
                ImGui.Spacing();
            }

            if (_selectedMapIndex < 0 || _selectedMapIndex >= _strategies.Count)
            {
                ImGui.Spacing();
                SafeTextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), Loc.Tr("NoStrategySelected"));
                ImGui.TextWrapped(Loc.Tr("SelectStrategyPrompt"));
                return;
            }

            var currentMap = _strategies[_selectedMapIndex];
            int activeWave = _detectedWaveNumber ?? _currentWaveNumber;

            if (!_settings.CompactMode)
            {
                string ocrText = Loc.Tr("AutoOcrHeader");
                string ocrBtnText = Loc.Tr("SelectOcrRegionBtn");

                float ocrTextW = ImGui.CalcTextSize(ocrText).X + 35.0f;
                float ocrBtnW = ImGui.CalcTextSize(ocrBtnText).X + ImGui.GetStyle().FramePadding.X * 2 + 10.0f;
                float availOcrW = ImGui.GetContentRegionAvail().X;

                bool ocrFitsSameLine = availOcrW >= ocrTextW + ocrBtnW + ImGui.GetStyle().ItemSpacing.X * 2;
                float cardH = ocrFitsSameLine ? 42.0f : 68.0f;

                ImGui.BeginChild("OcrHeaderCard", new Vector2(0, cardH), true);
                bool enableOcr = _settings.EnableOcr;
                if (ImGui.Checkbox(ocrText, ref enableOcr))
                {
                    _settings.EnableOcr = enableOcr;
                    StrategyService.SaveSettings(_settings);
                }

                if (ocrFitsSameLine)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ocrBtnW - 12.0f);
                }

                if (ImGui.Button(ocrBtnText))
                {
                    _isSelectingOcrRegion = true;
                    _ocrSelectionState = 0;
                }

                ImGui.EndChild();
                ImGui.Spacing();
            }

            if (!_isEditing)
            {
                if (currentMap.Steps.Count > 0)
                {
                    _currentStepIndex = Math.Clamp(_currentStepIndex, 0, currentMap.Steps.Count - 1);

                    var step = currentMap.Steps[_currentStepIndex];

                    ImGui.BeginChild("StepProgressCard", new Vector2(0, 115), true);

                    SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("CurrentWaveHeader"));
                    ImGui.SameLine();

                    SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"[ {activeWave} ]");

                    if (_detectedWaveNumber.HasValue)
                    {
                        ImGui.SameLine();
                        SafeTextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Loc.Tr("AutoOcrTag"));
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90);
                    if (ImGui.InputInt("##ManualWaveInput", ref _currentWaveNumber, 1, 5))
                    {
                        _currentWaveNumber = Math.Clamp(_currentWaveNumber, 1, 100);
                        _detectedWaveNumber = null;
                        if (_currentWaveNumber == 1)
                        {
                            _triggeredDjAlerts.Clear();
                            _triggeredOcrSounds.Clear();
                        }
                        OnWaveChanged(_currentWaveNumber);
                        activeWave = _currentWaveNumber;
                        UpdateStepByWaveNumber(currentMap, _currentWaveNumber);
                    }

                    float progress = (float)(_currentStepIndex + 1) / currentMap.Steps.Count;
                    ImGui.ProgressBar(progress, new Vector2(-1, 6), "");

                    ImGui.TextDisabled($"{Loc.Tr("Step")} {_currentStepIndex + 1} {Loc.Tr("Of")} {currentMap.Steps.Count}");
                    ImGui.SameLine();

                    bool isOcrMatched = activeWave >= step.StartWave && activeWave <= step.EndWave;
                    Vector4 waveColor = isOcrMatched ? new Vector4(0.3f, 1.0f, 0.4f, 1.0f) : new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
                    SafeTextColored(waveColor, $"{Loc.Tr("Waves")} {step.StartWave} - {step.EndWave}");

                    if (isOcrMatched)
                    {
                        ImGui.SameLine();
                        SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), Loc.Tr("ActiveInGame"));
                    }

                    float navAvailW = ImGui.GetContentRegionAvail().X;
                    float navSpacing = ImGui.GetStyle().ItemSpacing.X;
                    float navBtnW = (navAvailW - navSpacing * 3) / 4.0f;

                    if (ImGui.Button("|<", new Vector2(navBtnW, 0)))
                    {
                        _currentStepIndex = 0;
                        _currentWaveNumber = currentMap.Steps[0].StartWave;
                        _detectedWaveNumber = null;
                        OnWaveChanged(_currentWaveNumber);
                        activeWave = _currentWaveNumber;
                        ResetImageTransform();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("<", new Vector2(navBtnW, 0)))
                    {
                        if (_currentStepIndex > 0)
                        {
                            _currentStepIndex--;
                            _currentWaveNumber = currentMap.Steps[_currentStepIndex].StartWave;
                            _detectedWaveNumber = null;
                            OnWaveChanged(_currentWaveNumber);
                            activeWave = _currentWaveNumber;
                            ResetImageTransform();
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(">", new Vector2(navBtnW, 0)))
                    {
                        if (_currentStepIndex < currentMap.Steps.Count - 1)
                        {
                            _currentStepIndex++;
                            _currentWaveNumber = currentMap.Steps[_currentStepIndex].StartWave;
                            _detectedWaveNumber = null;
                            OnWaveChanged(_currentWaveNumber);
                            activeWave = _currentWaveNumber;
                            ResetImageTransform();
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(">|", new Vector2(navBtnW, 0)))
                    {
                        _currentStepIndex = currentMap.Steps.Count - 1;
                        _currentWaveNumber = currentMap.Steps[^1].StartWave;
                        _detectedWaveNumber = null;
                        OnWaveChanged(_currentWaveNumber);
                        activeWave = _currentWaveNumber;
                        ResetImageTransform();
                    }

                    ImGui.EndChild();
                    ImGui.Spacing();
                }

                if (!string.IsNullOrWhiteSpace(currentMap.GeneralInfo))
                {
                    SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("GeneralInfo"));
                    float infoH = _settings.GeneralInfoBoxHeight;
                    ImGui.BeginChild("GeneralInfoScroll", new Vector2(0, infoH), true);

                    string[] lines = currentMap.GeneralInfo.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                    {
                        string rawLine = lines[lineIdx];
                        string taskKey = $"gen_{_selectedMapIndex}_{lineIdx}";
                        RenderInstructionLine(rawLine, activeWave, taskKey);
                    }

                    ImGui.EndChild();

                    _settings.GeneralInfoBoxHeight = DrawHeightResizeHandle(_settings.GeneralInfoBoxHeight, 40.0f, 400.0f, "GeneralInfoHandle");
                    ImGui.Spacing();
                }

                if (currentMap.Steps.Count > 0)
                {
                    _currentStepIndex = Math.Clamp(_currentStepIndex, 0, currentMap.Steps.Count - 1);
                    var step = currentMap.Steps[_currentStepIndex];

                    string headerText = Loc.Tr("InstructionHeader");
                    ImGui.Text(headerText);

                    if (!_settings.CompactMode)
                    {
                        string copyText = Loc.Tr("CopyInstruction");
                        string clearText = Loc.Tr("ClearChecks");

                        float copyW = ImGui.CalcTextSize(copyText).X + ImGui.GetStyle().FramePadding.X * 2 + 6.0f;
                        float clearW = ImGui.CalcTextSize(clearText).X + ImGui.GetStyle().FramePadding.X * 2 + 6.0f;
                        float totalBtnW = copyW + clearW + ImGui.GetStyle().ItemSpacing.X;

                        float availW = ImGui.GetContentRegionAvail().X;
                        float headerTextW = ImGui.CalcTextSize(headerText).X;

                        bool headerFitsSameLine = availW >= headerTextW + totalBtnW + ImGui.GetStyle().ItemSpacing.X * 2;

                        if (headerFitsSameLine)
                        {
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - totalBtnW - 12.0f);
                        }

                        if (ImGui.Button(copyText, new Vector2(copyW, 0)))
                        {
                            ImagePickerHelper.SetClipboardText(step.Instruction);
                            _toastTimer = 2.0f;
                        }

                        ImGui.SameLine();

                        if (ImGui.Button(clearText, new Vector2(clearW, 0)))
                        {
                            _completedTasks.Clear();
                            _triggeredDjAlerts.Clear();
                            _triggeredOcrSounds.Clear();
                            StrategyService.SaveCompletedTasks(_completedTasks);
                        }

                        if (_toastTimer > 0)
                        {
                            _toastTimer -= ImGui.GetIO().DeltaTime;
                            ImGui.SameLine();
                            SafeTextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Loc.Tr("CopiedToast"));
                        }
                    }

                    float textH = _settings.InstructionBoxHeight;
                    ImGui.BeginChild("StepScroll", new Vector2(0, textH), true);

                    if (string.IsNullOrWhiteSpace(step.Instruction))
                    {
                        ImGui.TextWrapped("[...]");
                    }
                    else
                    {
                        string[] lines = step.Instruction.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                        for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                        {
                            string rawLine = lines[lineIdx];
                            string taskKey = $"{_selectedMapIndex}_{_currentStepIndex}_{lineIdx}";

                            RenderInstructionLine(rawLine, activeWave, taskKey);
                        }
                    }

                    if (_settings.SeparateImageWindow && currentMap.ImagePaths.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.TextDisabled($"{Loc.Tr("SeparateImageNotice")} ({currentMap.ImagePaths.Count})");
                    }

                    ImGui.EndChild();

                    _settings.InstructionBoxHeight = DrawHeightResizeHandle(_settings.InstructionBoxHeight, 60.0f, 500.0f, "InstructionTextHandle");

                    if (!_settings.SeparateImageWindow && currentMap.ImagePaths.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.Separator();

                        RenderImageSelector(currentMap);

                        string? currentImg = GetActiveImagePath(currentMap);
                        if (!string.IsNullOrEmpty(currentImg) && File.Exists(currentImg))
                        {
                            RenderImageCanvas(currentImg, _settings.EmbeddedImageBoxHeight, $"EmbeddedCanvas_{_currentImageIndex}", enableResizeGrip: true);
                        }
                    }
                }
                else
                {
                    ImGui.Text(Loc.Tr("NoStepsNotice"));
                    if (ImGui.Button(Loc.Tr("AddDefaultStepBtn"), new Vector2(-1, 0)))
                    {
                        currentMap.Steps.Add(new StrategyStep { StartWave = 1, EndWave = 30, Instruction = "..." });
                        StrategyService.SaveStrategy(currentMap);
                    }
                }
            }
            else
            {
                // EDITING MODE
                SafeTextColored(new Vector4(1f, 0.8f, 0.2f, 1f), Loc.Tr("EditingTitle"));
                ImGui.Spacing();

                ImGui.Text($"{Loc.Tr("MapName")}:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##EditMapName", ref _editMapName, 500000);

                ImGui.Text($"{Loc.Tr("Difficulty")}:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##EditDifficulty", ref _editDifficulty, 500000);

                ImGui.Text($"{Loc.Tr("StrategyVariant")}:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##EditStrategyName", ref _editStrategyName, 500000);

                ImGui.Spacing();
                ImGui.Text(Loc.Tr("GeneralInfoLabel"));
                ImGui.InputTextMultiline("##EditGeneralInfo", ref _editGeneralInfo, 500000, new Vector2(-1, 60), ImGuiInputTextFlags.AllowTabInput);

                ImGui.Separator();
                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImagesHeader"));

                for (int imgIdx = 0; imgIdx < currentMap.ImagePaths.Count; imgIdx++)
                {
                    ImGui.PushID($"mapImg_{imgIdx}");
                    string imgPath = currentMap.ImagePaths[imgIdx];

                    ImGui.Text(string.Format(Loc.Tr("PhotoNum"), imgIdx + 1));
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 110);
                    if (ImGui.InputText($"##ImgPath_{imgIdx}", ref imgPath, 260))
                    {
                        currentMap.ImagePaths[imgIdx] = imgPath;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Tr("DeletePhotoBtn")))
                    {
                        currentMap.ImagePaths.RemoveAt(imgIdx);
                        ImGui.PopID();
                        break;
                    }
                    ImGui.PopID();
                }

                if (ImGui.Button(Loc.Tr("AddPhotoBtn")))
                {
                    currentMap.ImagePaths.Add("");
                }

                ImGui.Separator();
                ImGui.Text(Loc.Tr("StepsHeader"));
                ImGui.TextDisabled(Loc.Tr("MarkdownHint"));

                for (int i = 0; i < currentMap.Steps.Count; i++)
                {
                    var s = currentMap.Steps[i];
                    ImGui.PushID(i);

                    ImGui.TextDisabled($"{Loc.Tr("Step")} #{i + 1}");

                    int start = s.StartWave;
                    int end = s.EndWave;
                    string inst = s.Instruction;

                    ImGui.SetNextItemWidth(70);
                    if (ImGui.InputInt(Loc.Tr("FromWave"), ref start, 0)) s.StartWave = start;
                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(70);
                    if (ImGui.InputInt(Loc.Tr("ToWave"), ref end, 0)) s.EndWave = end;

                    ImGui.Text(Loc.Tr("StepInstructionLabel"));
                    if (ImGui.InputTextMultiline($"##StepInst_{i}", ref inst, 500000, new Vector2(-1, 60), ImGuiInputTextFlags.AllowTabInput))
                    {
                        s.Instruction = inst;
                    }

                    if (currentMap.Steps.Count > 1)
                    {
                        if (ImGui.Button(Loc.Tr("DeleteStepBtn"), new Vector2(-1, 0)))
                        {
                            currentMap.Steps.RemoveAt(i);
                            ImGui.PopID();
                            break;
                        }
                    }

                    ImGui.Separator();
                    ImGui.PopID();
                }

                if (ImGui.Button(Loc.Tr("AddStepBtn"), new Vector2(-1, 0)))
                {
                    int lastEnd = currentMap.Steps.Count > 0 ? currentMap.Steps[^1].EndWave : 1;
                    currentMap.Steps.Add(new StrategyStep
                    {
                        StartWave = lastEnd + 1,
                        EndWave = lastEnd + 10,
                        Instruction = "..."
                    });
                }

                ImGui.Spacing();

                float editAvailW = ImGui.GetContentRegionAvail().X;
                float editSpacing = ImGui.GetStyle().ItemSpacing.X;
                float editBtnW = (editAvailW - editSpacing) / 2.0f;

                if (ImGui.Button(Loc.Tr("Save"), new Vector2(editBtnW, 0)))
                {
                    currentMap.MapName = _editMapName;
                    currentMap.Difficulty = _editDifficulty;
                    currentMap.StrategyName = _editStrategyName;
                    currentMap.GeneralInfo = _editGeneralInfo;

                    StrategyService.SaveStrategy(currentMap);
                    _isEditing = false;
                }

                ImGui.SameLine();

                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(editBtnW, 0)))
                {
                    _isEditing = false;
                }

                ImGui.Spacing();

                if (ImGui.Button(Loc.Tr("DeleteEntireStrategyBtn"), new Vector2(-1, 0)))
                {
                    _showDeleteConfirmModal = true;
                }
            }
        }

        private void RenderHelpModal()
        {
            ImGui.OpenPopup(Loc.Tr("HelpTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("HelpTitle"), ref _showHelpModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("HelpTitle"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(Loc.Tr("HelpLine1"));
                ImGui.Text(Loc.Tr("HelpLine2"));
                ImGui.Text(Loc.Tr("HelpLine3"));

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    _showHelpModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private void RenderUpdateModal()
        {
            ImGui.OpenPopup(Loc.Tr("UpdateModalTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("UpdateModalTitle"), ref _showUpdateModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("UpdateNotice"));
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text($"{Loc.Tr("CurrentVersionLabel")} {CurrentAppVersion}");
                SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"{Loc.Tr("LatestVersionLabel")} {_latestVersionTag}");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button(Loc.Tr("DownloadUpdateBtn"), new Vector2(160, 0)))
                {
                    UpdateService.OpenUrlInBrowser(_releaseUrl);
                    _showUpdateModal = false;
                }

                ImGui.SameLine();

                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(100, 0)))
                {
                    _showUpdateModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private void RenderOcrSelectionOverlay()
        {
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(3840, 2160));
            ImGui.Begin("OcrSelectorWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground);

            var drawList = ImGui.GetForegroundDrawList();
            var io = ImGui.GetIO();

            drawList.AddRectFilled(Vector2.Zero, new Vector2(3840, 2160), 0x99000000);
            drawList.AddText(new Vector2(30, 30), 0xFF00FFFF, Loc.Tr("OcrOverlayInstruction"));

            if (_ocrSelectionState == 0)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _ocrDragStart = io.MousePos;
                    _ocrSelectionState = 1;
                }
            }
            else if (_ocrSelectionState == 1)
            {
                Vector2 min = Vector2.Min(_ocrDragStart, io.MousePos);
                Vector2 max = Vector2.Max(_ocrDragStart, io.MousePos);

                int w = (int)(max.X - min.X);
                int h = (int)(max.Y - min.Y);

                drawList.AddRect(min, max, 0xFF00FFFF, 0, ImDrawFlags.None, 3.0f);
                drawList.AddRectFilled(min, max, 0x4400FFFF);
                drawList.AddText(max + new Vector2(10, 10), 0xFFFFFFFF, $"{w} x {h} px");

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    if (w > 2 && h > 2)
                    {
                        _settings.OcrX = (int)min.X;
                        _settings.OcrY = (int)min.Y;
                        _settings.OcrW = w;
                        _settings.OcrH = h;
                        _settings.EnableOcr = true;

                        StrategyService.SaveSettings(_settings);

                        _lastOcrScanTime = DateTime.MinValue;
                        _detectedWaveNumber = null;
                    }

                    _isSelectingOcrRegion = false;
                    _ocrSelectionState = 0;
                }
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                _isSelectingOcrRegion = false;
                _ocrSelectionState = 0;
            }

            ImGui.End();
        }

        private string? GetActiveImagePath(MapStrategy map)
        {
            if (map.ImagePaths.Count == 0) return null;
            if (_currentImageIndex >= map.ImagePaths.Count) _currentImageIndex = 0;
            return map.ImagePaths[_currentImageIndex];
        }

        private void RenderImageSelector(MapStrategy map)
        {
            if (map.ImagePaths.Count <= 1) return;

            if (ImGui.Button(Loc.Tr("PrevPhoto")))
            {
                _currentImageIndex = (_currentImageIndex - 1 + map.ImagePaths.Count) % map.ImagePaths.Count;
                ResetImageTransform();
            }

            ImGui.SameLine();
            ImGui.TextDisabled(string.Format(Loc.Tr("PhotoCount"), _currentImageIndex + 1, map.ImagePaths.Count));
            ImGui.SameLine();

            if (ImGui.Button(Loc.Tr("NextPhoto")))
            {
                _currentImageIndex = (_currentImageIndex + 1) % map.ImagePaths.Count;
                ResetImageTransform();
            }
        }

        private float DrawHeightResizeHandle(float currentHeight, float minH, float maxH, string id)
        {
            ImGui.PushID(id);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.18f, 0.20f, 0.22f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.35f, 0.39f, 0.95f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.45f, 0.49f, 1.00f, 1.0f));

            ImGui.Button(Loc.Tr("ResizeHandleText"), new Vector2(-1, 26));

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                float deltaY = ImGui.GetIO().MouseDelta.Y;
                if (Math.Abs(deltaY) > 0.1f)
                {
                    currentHeight += deltaY;
                    currentHeight = Math.Clamp(currentHeight, minH, maxH);
                    StrategyService.SaveSettings(_settings);
                }
            }

            ImGui.PopStyleColor(3);
            ImGui.PopID();

            return currentHeight;
        }

        private void RenderImageCanvas(string imagePath, float height, string canvasId, bool enableResizeGrip = false)
        {
            try
            {
                AddOrGetImagePointer(imagePath, false, out nint handle, out uint imgW, out uint imgH);

                if (handle != nint.Zero && imgW > 0 && imgH > 0)
                {
                    float availW = ImGui.GetContentRegionAvail().X;
                    float baseScale = availW / imgW;

                    Vector2 displaySize = new Vector2(imgW * baseScale * _imageScale, imgH * baseScale * _imageScale);

                    float parentScrollY = ImGui.GetScrollY();

                    ImGui.BeginChild($"ImageScrollRegion_{canvasId}", new Vector2(0, height), true, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);

                    Vector2 startPos = ImGui.GetCursorPos();
                    Vector2 drawPos = startPos + _imageOffset;

                    ImGui.SetCursorPos(drawPos);
                    ImGui.Image(handle, displaySize);

                    ImGui.SetCursorPos(drawPos);
                    ImGui.InvisibleButton($"##PanArea_{canvasId}", displaySize);

                    bool isHovered = ImGui.IsItemHovered() || ImGui.IsWindowHovered();
                    var io = ImGui.GetIO();

                    if (isHovered)
                    {
                        float wheel = io.MouseWheel;
                        if (wheel != 0)
                        {
                            if (wheel > 0)
                                _imageScale = Math.Min(5.0f, _imageScale + 0.15f);
                            else if (wheel < 0)
                                _imageScale = Math.Max(0.2f, _imageScale - 0.15f);

                            io.MouseWheel = 0;
                        }

                        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                        {
                            _imageOffset += io.MouseDelta;
                        }
                    }

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle) || (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Middle)))
                    {
                        ResetImageTransform();
                    }

                    ImGui.EndChild();

                    if (isHovered)
                    {
                        ImGui.SetScrollY(parentScrollY);
                    }

                    if (enableResizeGrip)
                    {
                        _settings.EmbeddedImageBoxHeight = DrawHeightResizeHandle(_settings.EmbeddedImageBoxHeight, 100.0f, 600.0f, $"EmbeddedImageHandle_{canvasId}");
                    }

                    ImGui.TextDisabled(string.Format(Loc.Tr("ZoomNotice"), (int)(_imageScale * 100)));
                }
            }
            catch
            {
                SafeTextColored(new Vector4(1, 0.4f, 0.4f, 1), "[Error loading image]");
            }
        }

        private void RenderSeparateImageWindow()
        {
            if (_selectedMapIndex < 0 || _selectedMapIndex >= _strategies.Count) return;
            var currentMap = _strategies[_selectedMapIndex];

            if (currentMap.ImagePaths.Count == 0) return;

            string? activeImage = GetActiveImagePath(currentMap);
            if (string.IsNullOrWhiteSpace(activeImage) || !File.Exists(activeImage)) return;

            ImGui.SetNextWindowSize(new Vector2(400, 320), ImGuiCond.FirstUseEver);
            ImGui.Begin($"{Loc.Tr("SeparateImageTitle")}##SeparateImageWindow", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse);

            RenderImageSelector(currentMap);
            RenderImageCanvas(activeImage, 230, $"SeparateCanvas_{_currentImageIndex}", enableResizeGrip: false);

            ImGui.End();
        }

        private void ResetImageTransform()
        {
            _imageScale = 1.0f;
            _imageOffset = Vector2.Zero;
        }

        private void RenderAddMapModal()
        {
            ImGui.OpenPopup(Loc.Tr("Create"));
            if (ImGui.BeginPopupModal(Loc.Tr("Create"), ref _showAddMapModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"{Loc.Tr("StrategyVariant")}:");
                ImGui.InputText("##NewMapStrat", ref _newMapStrat, 500000);

                ImGui.Text($"{Loc.Tr("MapName")}:");
                ImGui.InputText("##NewMapName", ref _newMapName, 500000);

                ImGui.Text($"{Loc.Tr("Difficulty")}:");
                ImGui.InputText("##NewMapDiff", ref _newMapDiff, 500000);

                if (ImGui.Button(Loc.Tr("Create"), new Vector2(100, 0)))
                {
                    var map = new MapStrategy
                    {
                        MapName = string.IsNullOrWhiteSpace(_newMapName) ? "New Map" : _newMapName,
                        Difficulty = _newMapDiff,
                        StrategyName = string.IsNullOrWhiteSpace(_newMapStrat) ? "Solo" : _newMapStrat,
                        GeneralInfo = "",
                        ImagePaths = new List<string>(),
                        Steps = new List<StrategyStep>
                        {
                            new StrategyStep
                            {
                                StartWave = 1,
                                EndWave = 30,
                                Instruction = "First step instruction..."
                            }
                        }
                    };

                    StrategyService.SaveStrategy(map);
                    _strategies = StrategyService.LoadStrategies();
                    _selectedMapIndex = _strategies.Count - 1;
                    _currentStepIndex = 0;
                    _currentImageIndex = 0;
                    _showAddMapModal = false;
                }

                ImGui.SameLine();

                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(100, 0)))
                {
                    _showAddMapModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private void RenderImportExportModal()
        {
            ImGui.OpenPopup(Loc.Tr("ImportExport"));
            if (ImGui.BeginPopupModal(Loc.Tr("ImportExport"), ref _showImportExportModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ExportSection"));
                if (_selectedMapIndex >= 0 && _selectedMapIndex < _strategies.Count)
                {
                    var currentMap = _strategies[_selectedMapIndex];
                    ImGui.Text($"Strategy: {currentMap.DisplayName}");
                    ImGui.Spacing();

                    if (ImGui.Button(Loc.Tr("ExportZip"), new Vector2(220, 0)))
                    {
                        string defaultFileName = $"{currentMap.MapName}_{currentMap.Difficulty}_{currentMap.StrategyName}.zip";
                        string? targetPath = ImagePickerHelper.SaveFileDialog(defaultFileName, "ZIP Archive (*.zip)|*.zip", "zip");

                        if (!string.IsNullOrEmpty(targetPath))
                        {
                            if (StrategyService.ExportStrategyToZip(currentMap, _selectedMapIndex, _completedTasks, targetPath))
                            {
                                _exportStatusMessage = $"Saved successfully to:\n{targetPath}";
                            }
                            else
                            {
                                _exportStatusMessage = "Export error!";
                            }
                        }
                    }

                    ImGui.SameLine();

                    if (ImGui.Button(Loc.Tr("ExportClipboard"), new Vector2(220, 0)))
                    {
                        string base64 = StrategyService.ExportStrategyToClipboardBase64(currentMap, _selectedMapIndex, _completedTasks);
                        ImagePickerHelper.SetClipboardText(base64);
                        _exportStatusMessage = Loc.Tr("ClipboardExportSuccess");
                    }

                    if (!string.IsNullOrEmpty(_exportStatusMessage))
                    {
                        SafeTextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), _exportStatusMessage);
                    }
                }
                else
                {
                    ImGui.TextDisabled("(Select a strategy in the main window)");
                }

                ImGui.Separator();
                ImGui.Spacing();

                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImportSection"));
                ImGui.Spacing();

                if (ImGui.Button(Loc.Tr("ImportFileBtn"), new Vector2(220, 0)))
                {
                    string? selectedFile = ImagePickerHelper.OpenFileDialog("Strategy Files (*.zip;*.json)|*.zip;*.json|All Files (*.*)|*.*", "zip");
                    if (!string.IsNullOrEmpty(selectedFile))
                    {
                        if (StrategyService.ImportStrategy(selectedFile, out string msg))
                        {
                            _importStatusMessage = msg;
                            _strategies = StrategyService.LoadStrategies();
                            _selectedMapIndex = _strategies.Count - 1;
                        }
                        else
                        {
                            _importStatusMessage = msg;
                        }
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button(Loc.Tr("ImportClipboard"), new Vector2(220, 0)))
                {
                    string? clipText = ImagePickerHelper.GetClipboardText();
                    if (!string.IsNullOrEmpty(clipText))
                    {
                        if (StrategyService.ImportStrategyFromClipboardBase64(clipText, out string msg))
                        {
                            _importStatusMessage = msg;
                            _strategies = StrategyService.LoadStrategies();
                            _selectedMapIndex = _strategies.Count - 1;
                        }
                        else
                        {
                            _importStatusMessage = msg;
                        }
                    }
                    else
                    {
                        _importStatusMessage = Loc.Tr("ClipboardImportError");
                    }
                }

                if (!string.IsNullOrEmpty(_importStatusMessage))
                {
                    SafeTextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), _importStatusMessage);
                }

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    _showImportExportModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private void RenderSettingsModal()
        {
            ImGui.OpenPopup(Loc.Tr("Settings"));
            if (ImGui.BeginPopupModal(Loc.Tr("Settings"), ref _showSettingsModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("LanguageSetting"));
                ImGui.Spacing();

                foreach (var langCode in Loc.GetAvailableLanguages())
                {
                    string displayName = Loc.GetLanguageDisplayName(langCode);
                    if (ImGui.RadioButton(displayName, Loc.CurrentLanguage.Equals(langCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        Loc.CurrentLanguage = langCode;
                        _settings.Language = langCode;
                        StrategyService.SaveSettings(_settings);
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();

                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImageModeSetting"));
                ImGui.Spacing();

                bool separate = _settings.SeparateImageWindow;

                if (ImGui.RadioButton(Loc.Tr("SeparateWindowMode"), separate))
                {
                    _settings.SeparateImageWindow = true;
                    StrategyService.SaveSettings(_settings);
                }

                if (ImGui.RadioButton(Loc.Tr("EmbeddedMode"), !separate))
                {
                    _settings.SeparateImageWindow = false;
                    StrategyService.SaveSettings(_settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("WindowOpacitySetting"));
                ImGui.Spacing();

                float opacity = _settings.WindowOpacity;
                ImGui.SetNextItemWidth(250);
                if (ImGui.SliderFloat("##OpacitySlider", ref opacity, 0.1f, 1.0f, "%.2f"))
                {
                    _settings.WindowOpacity = opacity;
                    StrategyService.SaveSettings(_settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    _showSettingsModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private void RenderAboutModal()
        {
            ImGui.OpenPopup(Loc.Tr("AboutTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("AboutTitle"), ref _showAboutModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), "TDS Strategy Overlay");
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(Loc.Tr("AboutVersion"));
                ImGui.Text(Loc.Tr("AboutAuthor"));
                ImGui.TextDisabled(Loc.Tr("AboutDesc"));

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    _showAboutModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private void RenderDeleteConfirmModal()
        {
            if (_selectedMapIndex < 0 || _selectedMapIndex >= _strategies.Count) return;
            var currentMap = _strategies[_selectedMapIndex];

            ImGui.OpenPopup(Loc.Tr("DeleteConfirmTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("DeleteConfirmTitle"), ref _showDeleteConfirmModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(string.Format(Loc.Tr("DeleteConfirmText"), currentMap.DisplayName));
                ImGui.Spacing();

                if (ImGui.Button(Loc.Tr("YesDelete"), new Vector2(120, 0)))
                {
                    StrategyService.DeleteStrategy(currentMap);
                    _strategies = StrategyService.LoadStrategies();
                    _selectedMapIndex = -1;
                    _currentStepIndex = 0;
                    _currentImageIndex = 0;
                    _isEditing = false;
                    _showDeleteConfirmModal = false;
                }

                ImGui.SameLine();

                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(100, 0)))
                {
                    _showDeleteConfirmModal = false;
                }

                ImGui.EndPopup();
            }
        }

        public static async Task Main()
        {
            var overlay = new TdsImGuiOverlay();
            await overlay.Run();
        }
    }
}