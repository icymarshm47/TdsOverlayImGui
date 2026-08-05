using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClickableTransparentOverlay;
using ImGuiNET;

namespace TdsOverlayImGui
{
    public class TdsImGuiOverlay : Overlay
    {
        // ВАШИ ДАННЫЕ НА GITHUB
        private const string GitHubOwner = "icymarshm47";
        private const string GitHubRepo = "TdsOverlayImGui";
        private const string CurrentAppVersion = "0.4";

        private List<MapStrategy> _strategies = new();
        private AppSettings _settings = new();

        private int _selectedMapIndex = -1;
        private int _currentStepIndex = 0;
        private int _currentImageIndex = 0;

        // Флаг работы главного окна ImGui (если false — закрываем приложение)
        private bool _isOverlayOpen = true;

        // Auto Update State
        private bool _showUpdateModal = false;
        private string _latestVersionTag = "";
        private string _releaseUrl = "";
        private string _manualCheckMessage = "";
        private float _manualCheckMessageTimer = 0.0f; // Таймер на 5 секунд

        // Completed tasks checklist
        private HashSet<string> _completedTasks = new();

        // Current wave number
        private int _currentWaveNumber = 1;

        // Regex for <ocr N>
        private static readonly Regex OcrTagRegex = new Regex(@"<ocr\s+([\d\-]+)>(.*?)</ocr[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private float _toastTimer = 0.0f;
        private string _clipboardToastMessage = "";

        private float _imageScale = 1.0f;
        private Vector2 _imageOffset = Vector2.Zero;

        // Windows OCR
        private int? _detectedWaveNumber = null;
        private bool _isSelectingOcrRegion = false;
        private int _ocrSelectionState = 0;
        private Vector2 _ocrDragStart = Vector2.Zero;
        private DateTime _lastOcrScanTime = DateTime.MinValue;

        // Editor
        private bool _isEditing = false;
        private string _editMapName = "";
        private string _editDifficulty = "";
        private string _editStrategyName = "";
        private string _editGeneralInfo = "";

        // Modals
        private bool _showAddMapModal = false;
        private string _newMapName = "";
        private string _newMapDiff = "Fallen";
        private string _newMapStrat = "Solo";

        private bool _showImportExportModal = false;
        private string _importFilePath = "";
        private string _importStatusMessage = "";
        private string _exportStatusMessage = "";

        private bool _showSettingsModal = false;
        private bool _showAboutModal = false;
        private bool _showDeleteConfirmModal = false;

        private bool _styleConfigured = false;

        public TdsImGuiOverlay() : base("TDS Strategy Overlay", true)
        {
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
            _selectedMapIndex = -1;

            CheckForUpdatesInBackground(silent: true);
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
                    _manualCheckMessageTimer = 5.0f; // Таймер исчезновения надписи (5 сек)
                }
            });
        }

        protected override void Render()
        {
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
                    if (ImGui.MenuItem(Loc.Tr("CheckUpdates")))
                    {
                        _manualCheckMessage = "Проверка...";
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

            // Таймер сброса надписи статуса проверки обновлений (5 секунд)
            if (!string.IsNullOrEmpty(_manualCheckMessage))
            {
                ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), _manualCheckMessage);
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

            if (_showAddMapModal) RenderAddMapModal();
            if (_showImportExportModal) RenderImportExportModal();
            if (_showSettingsModal) RenderSettingsModal();
            if (_showAboutModal) RenderAboutModal();
            if (_showDeleteConfirmModal) RenderDeleteConfirmModal();
            if (_showUpdateModal) RenderUpdateModal();

            ImGui.End();

            if (_settings.SeparateImageWindow)
            {
                RenderSeparateImageWindow();
            }

            if (_isSelectingOcrRegion)
            {
                RenderOcrSelectionOverlay();
            }
        }

        private void SetupStyle()
        {
            if (_styleConfigured) return;

            var style = ImGui.GetStyle();
            style.WindowRounding = 8.0f;
            style.FrameRounding = 6.0f;
            style.PopupRounding = 6.0f;
            style.ScrollbarRounding = 6.0f;

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.07f, 0.08f, 0.09f, 0.96f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.18f, 0.19f, 0.22f, 1.0f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.28f, 0.32f, 0.77f, 1.0f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.22f, 0.25f, 0.60f, 1.0f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.14f, 0.15f, 0.17f, 1.0f);

            _styleConfigured = true;
        }

        private void RenderMainUI()
        {
            // Карточка выбора стратегии
            ImGui.BeginChild("MapSelectorCard", new Vector2(0, 98), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            ImGui.TextDisabled(Loc.Tr("SelectStrategyHeader"));

            var comboItemsList = new List<string> { Loc.Tr("SelectStrategyCombo") };
            for (int i = 0; i < _strategies.Count; i++)
            {
                comboItemsList.Add(_strategies[i].DisplayName);
            }

            string[] comboItems = comboItemsList.ToArray();
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

            if (_selectedMapIndex < 0 || _selectedMapIndex >= _strategies.Count)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), Loc.Tr("NoStrategySelected"));
                ImGui.TextWrapped(Loc.Tr("SelectStrategyPrompt"));
                return;
            }

            var currentMap = _strategies[_selectedMapIndex];
            int activeWave = _detectedWaveNumber ?? _currentWaveNumber;

            // WINDOWS OCR PANEL
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

            if (!_isEditing)
            {
                // 1. CURRENT WAVE & STEP PROGRESS
                if (currentMap.Steps.Count > 0)
                {
                    if (_currentStepIndex >= currentMap.Steps.Count)
                    {
                        _currentStepIndex = 0;
                    }

                    var step = currentMap.Steps[_currentStepIndex];

                    ImGui.BeginChild("StepProgressCard", new Vector2(0, 115), true);

                    ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("CurrentWaveHeader"));
                    ImGui.SameLine();

                    ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"[ {activeWave} ]");

                    if (_detectedWaveNumber.HasValue)
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Loc.Tr("AutoOcrTag"));
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90);
                    if (ImGui.InputInt("##ManualWaveInput", ref _currentWaveNumber, 1, 5))
                    {
                        _currentWaveNumber = Math.Clamp(_currentWaveNumber, 1, 100);
                        _detectedWaveNumber = null;
                        UpdateStepByWaveNumber(currentMap, _currentWaveNumber);
                    }

                    float progress = (float)(_currentStepIndex + 1) / currentMap.Steps.Count;
                    ImGui.ProgressBar(progress, new Vector2(-1, 6), "");

                    ImGui.TextDisabled($"{Loc.Tr("Step")} {_currentStepIndex + 1} {Loc.Tr("Of")} {currentMap.Steps.Count}");
                    ImGui.SameLine();

                    bool isOcrMatched = activeWave >= step.StartWave && activeWave <= step.EndWave;
                    Vector4 waveColor = isOcrMatched ? new Vector4(0.3f, 1.0f, 0.4f, 1.0f) : new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
                    ImGui.TextColored(waveColor, $"{Loc.Tr("Waves")} {step.StartWave} - {step.EndWave}");

                    if (isOcrMatched)
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), Loc.Tr("ActiveInGame"));
                    }

                    float navAvailW = ImGui.GetContentRegionAvail().X;
                    float navSpacing = ImGui.GetStyle().ItemSpacing.X;
                    float navBtnW = (navAvailW - navSpacing * 3) / 4.0f;

                    if (ImGui.Button("|<", new Vector2(navBtnW, 0))) { _currentStepIndex = 0; ResetImageTransform(); }
                    ImGui.SameLine();
                    if (ImGui.Button("<", new Vector2(navBtnW, 0)))
                    {
                        if (_currentStepIndex > 0) { _currentStepIndex--; ResetImageTransform(); }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(">", new Vector2(navBtnW, 0)))
                    {
                        if (_currentStepIndex < currentMap.Steps.Count - 1) { _currentStepIndex++; ResetImageTransform(); }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(">|", new Vector2(navBtnW, 0))) { _currentStepIndex = currentMap.Steps.Count - 1; ResetImageTransform(); }

                    ImGui.EndChild();
                    ImGui.Spacing();
                }

                // 2. GENERAL INFO
                if (!string.IsNullOrWhiteSpace(currentMap.GeneralInfo))
                {
                    ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("GeneralInfo"));
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

                // 3. INSTRUCTION FEED
                if (currentMap.Steps.Count > 0)
                {
                    var step = currentMap.Steps[_currentStepIndex];

                    string headerText = Loc.Tr("InstructionHeader");
                    string copyText = Loc.Tr("CopyInstruction");
                    string clearText = Loc.Tr("ClearChecks");

                    float copyW = ImGui.CalcTextSize(copyText).X + ImGui.GetStyle().FramePadding.X * 2 + 6.0f;
                    float clearW = ImGui.CalcTextSize(clearText).X + ImGui.GetStyle().FramePadding.X * 2 + 6.0f;
                    float totalBtnW = copyW + clearW + ImGui.GetStyle().ItemSpacing.X;

                    float availW = ImGui.GetContentRegionAvail().X;
                    float headerTextW = ImGui.CalcTextSize(headerText).X;

                    ImGui.Text(headerText);

                    bool headerFitsSameLine = availW >= headerTextW + totalBtnW + ImGui.GetStyle().ItemSpacing.X * 2;

                    if (headerFitsSameLine)
                    {
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - totalBtnW - 12.0f);
                    }

                    if (ImGui.Button(copyText, new Vector2(copyW, 0)))
                    {
                        ImGui.SetClipboardText(step.Instruction);
                        _toastTimer = 2.0f;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button(clearText, new Vector2(clearW, 0)))
                    {
                        _completedTasks.Clear();
                    }

                    if (_toastTimer > 0)
                    {
                        _toastTimer -= ImGui.GetIO().DeltaTime;
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Loc.Tr("CopiedToast"));
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
                // EDITING MODE (Асинхронные кнопки Выбора Файла и Вставки без зависания UI)
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), Loc.Tr("EditingTitle"));
                ImGui.Spacing();

                ImGui.Text($"{Loc.Tr("StrategyVariant")}:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##EditStrategyName", ref _editStrategyName, 500000);

                ImGui.Text($"{Loc.Tr("MapName")}:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##EditMapName", ref _editMapName, 500000);

                ImGui.Text($"{Loc.Tr("Difficulty")}:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##EditDifficulty", ref _editDifficulty, 500000);

                ImGui.Spacing();
                ImGui.Text(Loc.Tr("GeneralInfoLabel"));
                ImGui.InputTextMultiline("##EditGeneralInfo", ref _editGeneralInfo, 500000, new Vector2(-1, 60), ImGuiInputTextFlags.AllowTabInput);

                ImGui.Separator();
                ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImagesHeader"));

                for (int imgIdx = 0; imgIdx < currentMap.ImagePaths.Count; imgIdx++)
                {
                    ImGui.PushID($"mapImg_{imgIdx}");
                    string imgPath = currentMap.ImagePaths[imgIdx];

                    ImGui.Text(string.Format(Loc.Tr("PhotoNum"), imgIdx + 1));
                    ImGui.SameLine();

                    if (string.IsNullOrWhiteSpace(imgPath))
                    {
                        ImGui.TextDisabled(Loc.Tr("NoImageSelected"));
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Path.GetFileName(imgPath));
                    }

                    float availImgW = ImGui.GetContentRegionAvail().X;
                    float spacingImg = ImGui.GetStyle().ItemSpacing.X;
                    float btnImgW = (availImgW - spacingImg * 2 - 30) / 2.0f;

                    // Выбор файла через проводник (асинхронно)
                    if (ImGui.Button(Loc.Tr("SelectFileBtn"), new Vector2(btnImgW, 0)))
                    {
                        int targetIdx = imgIdx;
                        var map = currentMap;
                        ImGui.GetIO().MouseDown[0] = false; // Сбрасываем клик мыши, чтобы окно не открывалось повторно

                        Task.Run(() =>
                        {
                            string? selected = ImagePickerHelper.OpenImageFileDialog();
                            if (!string.IsNullOrEmpty(selected))
                            {
                                map.ImagePaths[targetIdx] = selected;
                            }
                        });
                    }

                    ImGui.SameLine();

                    // Вставка из буфера обмена (асинхронно)
                    if (ImGui.Button(Loc.Tr("PasteClipboardBtn"), new Vector2(btnImgW, 0)))
                    {
                        int targetIdx = imgIdx;
                        var map = currentMap;
                        ImGui.GetIO().MouseDown[0] = false;

                        Task.Run(() =>
                        {
                            string? savedClip = ImagePickerHelper.SaveImageFromClipboard(map.MapName, targetIdx + 1);
                            if (!string.IsNullOrEmpty(savedClip))
                            {
                                map.ImagePaths[targetIdx] = savedClip;
                            }
                            else
                            {
                                _clipboardToastMessage = Loc.Tr("ClipboardNoImageToast");
                            }
                        });
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("X##DelImg", new Vector2(30, 0)))
                    {
                        currentMap.ImagePaths.RemoveAt(imgIdx);
                        ImGui.PopID();
                        break;
                    }

                    if (!string.IsNullOrEmpty(_clipboardToastMessage))
                    {
                        ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), _clipboardToastMessage);
                    }

                    ImGui.PopID();
                    ImGui.Spacing();
                }

                if (ImGui.Button(Loc.Tr("AddPhotoBtn"), new Vector2(-1, 0)))
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

            var ocrMatch = OcrTagRegex.Match(lineAfterCheck);
            bool isTagActive = false;
            string displayText = lineAfterCheck;

            if (ocrMatch.Success)
            {
                string waveSpec = ocrMatch.Groups[1].Value.Trim();
                displayText = ocrMatch.Groups[2].Value;

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
                }
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
                }

                ImGui.SameLine();

                if (isDone)
                {
                    ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.4f, 0.85f), displayText);
                }
                else if (isTagActive)
                {
                    ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"[WAVE!] {displayText}");
                }
                else
                {
                    ImGui.TextWrapped(displayText);
                }

                ImGui.PopID();
            }
            else
            {
                if (isTagActive)
                {
                    ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"[WAVE!] {displayText.TrimStart()}");
                }
                else
                {
                    ImGui.TextWrapped(displayText.TrimStart());
                }
            }

            if (indentPixels > 0)
            {
                ImGui.Unindent(indentPixels);
            }
        }

        private void RenderUpdateModal()
        {
            ImGui.OpenPopup(Loc.Tr("UpdateModalTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("UpdateModalTitle"), ref _showUpdateModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("UpdateNotice"));
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text($"{Loc.Tr("CurrentVersionLabel")} {CurrentAppVersion}");
                ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"{Loc.Tr("LatestVersionLabel")} {_latestVersionTag}");

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
                    if (w > 10 && h > 10)
                    {
                        _settings.OcrX = (int)min.X;
                        _settings.OcrY = (int)min.Y;
                        _settings.OcrW = w;
                        _settings.OcrH = h;
                        _settings.EnableOcr = true;

                        StrategyService.SaveSettings(_settings);
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
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), "[Error loading image]");
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
            ImGui.OpenPopup(Loc.Tr("ImportExportTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("ImportExportTitle"), ref _showImportExportModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ExportSection"));
                if (_selectedMapIndex >= 0 && _selectedMapIndex < _strategies.Count)
                {
                    var currentMap = _strategies[_selectedMapIndex];
                    ImGui.Text($"Strategy: {currentMap.DisplayName}");

                    if (ImGui.Button(Loc.Tr("ExportZip")))
                    {
                        string zipPath = StrategyService.ExportStrategy(currentMap);
                        if (!string.IsNullOrEmpty(zipPath))
                        {
                            _exportStatusMessage = $"Saved to folder:\n{zipPath}";
                        }
                        else
                        {
                            _exportStatusMessage = "Export error!";
                        }
                    }

                    if (!string.IsNullOrEmpty(_exportStatusMessage))
                    {
                        ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), _exportStatusMessage);
                    }
                }
                else
                {
                    ImGui.TextDisabled("(Select a strategy in the main window)");
                }

                ImGui.Separator();
                ImGui.Spacing();

                ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImportSection"));
                ImGui.Text($"{Loc.Tr("FilePath")}:");
                ImGui.InputText("##ImportFilePath", ref _importFilePath, 500000);

                if (ImGui.Button(Loc.Tr("ImportFile")))
                {
                    if (StrategyService.ImportStrategy(_importFilePath, out string msg))
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

                if (!string.IsNullOrEmpty(_importStatusMessage))
                {
                    ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), _importStatusMessage);
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
                ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("LanguageSetting"));
                ImGui.Spacing();

                if (ImGui.RadioButton("English", Loc.CurrentLanguage == AppLanguage.English))
                {
                    Loc.CurrentLanguage = AppLanguage.English;
                    _settings.Language = AppLanguage.English;
                    StrategyService.SaveSettings(_settings);
                }

                if (ImGui.RadioButton("Русский", Loc.CurrentLanguage == AppLanguage.Russian))
                {
                    Loc.CurrentLanguage = AppLanguage.Russian;
                    _settings.Language = AppLanguage.Russian;
                    StrategyService.SaveSettings(_settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

                ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImageModeSetting"));
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
                ImGui.TextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), "TDS Strategy Overlay");
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