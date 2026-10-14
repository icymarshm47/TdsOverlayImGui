using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("shell32.dll")]
        private static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder? lpszFile, int cch);

        [DllImport("shell32.dll")]
        private static extern void DragFinish(IntPtr hDrop);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr igGetWindowDockNode();

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr igDockNodeGetRootNode(IntPtr node);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        private static extern void igDockBuilderSetNodePos(uint node_id, Vector2 pos);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igIsWindowDocked();

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate? _wndProcDelegate;
        private IntPtr _oldWndProc = IntPtr.Zero;

        private const int GWLP_WNDPROC = -4;
        private const uint WM_DROPFILES = 0x0233;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        private const string GitHubOwner = "icymarshm47";
        private const string GitHubRepo = "TdsOverlayImGui";
        public const string CurrentAppVersion = "0.7";

        private readonly OverlayContext _ctx = new();
        private readonly string _footerQuote = GetRandomQuote();

        private static string GetRandomQuote()
        {
            string[] defaultQuotes = new[]
            {
                "icymarsh",
                "gti-olen",
                "gemini",
                "skip wave?",
                "hacking your pc...",
                "privacy matters",
                "your account will be banned",
                "also try touching some grass!"
            };

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quotes.txt");
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllLines(path, defaultQuotes);
                    return defaultQuotes[Random.Shared.Next(defaultQuotes.Length)];
                }

                var lines = File.ReadAllLines(path);
                var validQuotes = new List<string>();
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var part in parts)
                    {
                        if (!string.IsNullOrWhiteSpace(part))
                        {
                            validQuotes.Add(part);
                        }
                    }
                }

                if (validQuotes.Count > 0)
                {
                    return validQuotes[Random.Shared.Next(validQuotes.Count)];
                }
            }
            catch { }

            return defaultQuotes[Random.Shared.Next(defaultQuotes.Length)];
        }

        public TdsImGuiOverlay() : base("TDS Strategy Overlay", true)
        {
            VSync = true;

            Loc.LoadLanguages();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] fontCandidates = new[]
            {
                Path.Combine(baseDir, "fonts", "JetBrainsMono-Regular.ttf"),
                Path.Combine(baseDir, "JetBrainsMono-Regular.ttf"),
                Path.Combine("fonts", "JetBrainsMono-Regular.ttf"),
                "JetBrainsMono-Regular.ttf",
                Path.Combine(baseDir, "fonts", "Roboto-Regular.ttf"),
                Path.Combine(baseDir, "Roboto-Regular.ttf"),
                Path.Combine("fonts", "Roboto-Regular.ttf"),
                "Roboto-Regular.ttf",
                @"C:\Windows\Fonts\JetBrainsMono-Regular.ttf",
                @"C:\Windows\Fonts\Roboto-Regular.ttf",
                @"C:\Windows\Fonts\segoeui.ttf",
                @"C:\Windows\Fonts\arial.ttf"
            };

            foreach (var candidate in fontCandidates)
            {
                if (File.Exists(candidate))
                {
                    ReplaceFont(candidate, 18, FontGlyphRangeType.Cyrillic);
                    break;
                }
            }

            _ctx.Settings = StrategyService.LoadSettings();
            _ctx.Strategies = StrategyService.LoadStrategies();
            _ctx.CompletedTasks = StrategyService.LoadCompletedTasks();

            if (_ctx.Strategies.Count > 0)
            {
                _ctx.SelectedMapIndex = 0;
            }

            if (_ctx.Settings.CheckUpdatesOnStartup)
            {
                CheckForUpdatesInBackground(silent: true);
            }
        }

        private static IntPtr _cachedHWnd = IntPtr.Zero;

        public static IntPtr GetOverlayHWnd()
        {
            if (_cachedHWnd != IntPtr.Zero && IsWindow(_cachedHWnd))
            {
                return _cachedHWnd;
            }
            _cachedHWnd = FindWindow(null, "TDS Strategy Overlay");
            return _cachedHWnd;
        }

        public static void SetAlwaysOnTop(bool enable)
        {
            IntPtr hWnd = GetOverlayHWnd();
            if (hWnd != IntPtr.Zero)
            {
                IntPtr insertAfter = enable ? HWND_TOPMOST : HWND_NOTOPMOST;
                SetWindowPos(hWnd, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        public static void SetHideFromCapture(bool enable)
        {
            IntPtr hWnd = GetOverlayHWnd();
            if (hWnd != IntPtr.Zero)
            {
                if (enable)
                {
                    if (!SetWindowDisplayAffinity(hWnd, WDA_EXCLUDEFROMCAPTURE))
                    {
                        SetWindowDisplayAffinity(hWnd, WDA_MONITOR);
                    }
                }
                else
                {
                    SetWindowDisplayAffinity(hWnd, WDA_NONE);
                }
            }
        }

        private bool _isDragDropHooked = false;
        private IntPtr _hookedHWnd = IntPtr.Zero;

        private bool HookDragAndDrop()
        {
            if (_isDragDropHooked) return true;
            IntPtr hWnd = GetOverlayHWnd();
            if (hWnd == IntPtr.Zero) return false;

            DragAcceptFiles(hWnd, true);
            _wndProcDelegate = CustomWndProc;
            IntPtr fnPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _oldWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, fnPtr);
            _hookedHWnd = hWnd;
            _cachedHWnd = hWnd;
            _isDragDropHooked = true;
            if (_ctx.Settings.HideFromCapture)
            {
                SetHideFromCapture(true);
            }
            return true;
        }

        private void UnhookDragAndDrop()
        {
            if (_isDragDropHooked && _hookedHWnd != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
            {
                SetWindowLongPtr(_hookedHWnd, GWLP_WNDPROC, _oldWndProc);
                DragAcceptFiles(_hookedHWnd, false);
                _oldWndProc = IntPtr.Zero;
                _isDragDropHooked = false;
            }
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_DROPFILES)
            {
                uint count = DragQueryFile(wParam, 0xFFFFFFFF, null, 0);
                for (uint i = 0; i < count; i++)
                {
                    var sb = new StringBuilder(1024);
                    if (DragQueryFile(wParam, i, sb, sb.Capacity) > 0)
                    {
                        _ctx.DroppedFilesQueue.Enqueue(sb.ToString());
                    }
                }
                DragFinish(wParam);
                return IntPtr.Zero;
            }
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void ProcessDroppedFiles()
        {
            while (_ctx.DroppedFilesQueue.TryDequeue(out string? filePath))
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) continue;

                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".zip" || ext == ".json")
                {
                    if (StrategyService.ImportStrategy(filePath, out string msg))
                    {
                        ImageViewer.ClearLoadedTextures(this);
                        _ctx.Strategies = StrategyService.LoadStrategies();
                        _ctx.SelectedMapIndex = _ctx.Strategies.Count - 1;
                        _ctx.CurrentStepIndex = 0;
                        _ctx.CurrentImageIndex = 0;
                        _ctx.DropStatusMessage = msg;
                        _ctx.DropStatusTimer = 4.0f;
                    }
                    else
                    {
                        _ctx.DropStatusMessage = msg;
                        _ctx.DropStatusTimer = 4.0f;
                    }
                }
                else if (_ctx.IsEditing && _ctx.CurrentMap != null && (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".webp" || ext == ".bmp"))
                {
                    _ctx.EditImagePaths.Add(filePath);
                    _ctx.DropStatusMessage = $"{Loc.Tr("PhotoAddedToast")}: {Path.GetFileName(filePath)}";
                    _ctx.DropStatusTimer = 3.0f;
                }
            }
        }

        private void CheckForUpdatesInBackground(bool silent = true)
        {
            Task.Run(async () =>
            {
                var (available, latestTag, url, errorKey) = await UpdateService.CheckForUpdatesAsync(
                    GitHubOwner, GitHubRepo, CurrentAppVersion);

                if (available)
                {
                    _ctx.LatestVersionTag = latestTag;
                    _ctx.ReleaseUrl = url;
                    _ctx.ShowUpdateModal = true;
                    if (!silent)
                    {
                        _ctx.ManualCheckMessage = "";
                    }
                }
                else if (!silent)
                {
                    _ctx.ManualCheckMessage = string.IsNullOrEmpty(errorKey) ? Loc.Tr("NoUpdatesNotice") : Loc.Tr(errorKey);
                    _ctx.ManualCheckMessageTimer = 5.0f;
                }
            });
        }

        protected override void Render()
        {
            if (_ctx.IsFirstFrame)
            {
                SetAlwaysOnTop(_ctx.Settings.AlwaysOnTop);
                if (_ctx.Settings.HideFromCapture)
                {
                    SetHideFromCapture(true);
                }
                _ctx.IsFirstFrame = false;
            }

            if (!_isDragDropHooked)
            {
                HookDragAndDrop();
            }

            if (!_ctx.IsOverlayOpen)
            {
                UnhookDragAndDrop();
                ImageViewer.ClearLoadedTextures(this);
                Close();
                return;
            }

            ProcessDroppedFiles();
            SetupStyle();
            OcrOverlayView.ProcessBackgroundOcr(_ctx);
            AudioService.ProcessPeriodicAlerts(_ctx);

            var wc = new ImGuiWindowClass();
            wc.DockNodeFlagsOverrideSet = ImGuiDockNodeFlags.AutoHideTabBar;
            unsafe
            {
                ImGui.SetNextWindowClass(new ImGuiWindowClassPtr(&wc));
            }
            ImGui.SetNextWindowSize(new Vector2(430, 800), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(250.0f, 250.0f), new Vector2(2000.0f, 2000.0f));

            bool isOpen = _ctx.IsOverlayOpen;
            ImGui.Begin("TDS Strategy Overlay (ImGui)", ref isOpen, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            _ctx.IsOverlayOpen = isOpen;

            _ctx.MainWindowWidth = ImGui.GetWindowWidth();
            _ctx.MainWindowHeight = ImGui.GetWindowHeight();

            RenderCustomTitleBar();

            float footerH = 24.0f;
            ImGui.BeginChild("MainContentScrollArea", new Vector2(0, -footerH - 2.0f), false, ImGuiWindowFlags.None);

            if (!string.IsNullOrEmpty(_ctx.ManualCheckMessage))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), _ctx.ManualCheckMessage);
                _ctx.ManualCheckMessageTimer -= ImGui.GetIO().DeltaTime;
                if (_ctx.ManualCheckMessageTimer <= 0.0f)
                {
                    _ctx.ManualCheckMessage = "";
                }
            }

            if (_ctx.DropStatusTimer > 0.0f && !string.IsNullOrEmpty(_ctx.DropStatusMessage))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.4f, 0.9f, 1.0f, 1.0f), _ctx.DropStatusMessage);
                _ctx.DropStatusTimer -= ImGui.GetIO().DeltaTime;
                if (_ctx.DropStatusTimer <= 0.0f)
                {
                    _ctx.DropStatusMessage = "";
                }
            }

            ImGui.Spacing();

            if (_ctx.Strategies.Count == 0)
            {
                ImGui.Text(Loc.Tr("NoStrategySelected"));
                if (ImGui.Button(Loc.Tr("AddStrategy"), new Vector2(-1, 0)))
                {
                    _ctx.ShowAddMapModal = true;
                }
            }
            else
            {
                if (!_ctx.IsEditing)
                    StrategyView.Render(this, _ctx);
                else
                    StrategyEditorView.Render(this, _ctx);
            }

            ImGui.EndChild();

            RenderFooter();
            ImGui.End();

            if (!_ctx.IsEditing) ImageViewer.RenderSeparateImageWindow(this, _ctx);
            if (_ctx.IsSelectingOcrRegion) OcrOverlayView.RenderSelectionOverlay(_ctx);

            DjToastOverlay.Render(_ctx);
            ModalsView.RenderAll(this, _ctx);
        }

        public void RenderCustomTitleBar(string title = "TDS Overlay", bool isMainWindow = true, string popupSuffix = "")
        {
            float availW = ImGui.GetContentRegionAvail().X;
            float titleH = 34.0f;

            float buttonsTotalW = 65.0f;
            float dragW = Math.Max(0.0f, availW - buttonsTotalW);
            ImGui.InvisibleButton("##CustomTitleDragArea" + popupSuffix, new Vector2(dragW, titleH));
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                Vector2 delta = ImGui.GetIO().MouseDelta;
                if (igIsWindowDocked() != 0)
                {
                    IntPtr dockNode = igGetWindowDockNode();
                    if (dockNode != IntPtr.Zero)
                    {
                        IntPtr rootNode = igDockNodeGetRootNode(dockNode);
                        if (rootNode != IntPtr.Zero)
                        {
                            unsafe
                            {
                                uint rootId = *(uint*)rootNode;
                                Vector2 newPos = ImGui.GetWindowPos() + delta;
                                igDockBuilderSetNodePos(rootId, newPos);
                            }
                        }
                    }
                }
                else
                {
                    ImGui.SetWindowPos(ImGui.GetWindowPos() + delta);
                }
            }

            Vector2 winPos = ImGui.GetWindowPos();
            var drawList = ImGui.GetWindowDrawList();

            float textY = winPos.Y + 9.0f;
            ImGui.SetCursorScreenPos(new Vector2(winPos.X + 8.0f, textY));
            InstructionRenderer.SafeTextColored(new Vector4(1f, 1f, 1f, 1f), title);

            if (isMainWindow)
            {
                ImGui.SameLine(0, 8.0f);
                string verTag = $"v{CurrentAppVersion}";
                Vector2 verSize = ImGui.CalcTextSize(verTag);
                Vector2 badgePos = ImGui.GetCursorScreenPos();
                drawList.AddRectFilled(
                    new Vector2(badgePos.X - 3.0f, textY - 1.0f),
                    new Vector2(badgePos.X + verSize.X + 3.0f, textY + verSize.Y + 2.0f),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f)),
                    4.0f);
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), verTag);
            }

            float rightX = winPos.X + availW;
            float btnSize = 20.0f;
            float btnY = winPos.Y + 8.5f;

            float closeBtnX = rightX - btnSize - 4.0f;
            ImGui.SetCursorScreenPos(new Vector2(closeBtnX, btnY));
            if (ImGui.InvisibleButton("##CloseBtn" + popupSuffix, new Vector2(btnSize, btnSize)))
            {
                if (isMainWindow)
                {
                    _ctx.IsOverlayOpen = false;
                }
                else
                {
                    _ctx.IsSeparateImageWindowOpen = false;
                }
            }
            bool isCloseHovered = ImGui.IsItemHovered();
            uint closeCircleCol = ImGui.ColorConvertFloat4ToU32(isCloseHovered
                ? new Vector4(218f / 255f, 55f / 255f, 60f / 255f, 1.0f)
                : new Vector4(242f / 255f, 63f / 255f, 67f / 255f, 1.0f));

            float cX = MathF.Floor(closeBtnX + btnSize * 0.5f);
            float cY = MathF.Floor(btnY + btnSize * 0.5f);
            drawList.AddCircleFilled(new Vector2(cX, cY), 8.0f, closeCircleCol);
            float d = 3.5f;
            drawList.AddLine(new Vector2(cX - d, cY - d), new Vector2(cX + d, cY + d), 0xFFFFFFFF, 1.6f);
            drawList.AddLine(new Vector2(cX + d, cY - d), new Vector2(cX - d, cY + d), 0xFFFFFFFF, 1.6f);

            float menuBtnX = closeBtnX - btnSize - 8.0f;
            ImGui.SetCursorScreenPos(new Vector2(menuBtnX, btnY));
            if (ImGui.InvisibleButton("##MenuBtn" + popupSuffix, new Vector2(btnSize, btnSize)))
            {
                ImGui.OpenPopup("TitleMenuPopup" + popupSuffix);
            }
            bool isMenuHovered = ImGui.IsItemHovered();
            uint menuLineCol = ImGui.ColorConvertFloat4ToU32(isMenuHovered ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1f));
            drawList.AddLine(new Vector2(menuBtnX + 3.0f, btnY + 5.0f), new Vector2(menuBtnX + btnSize - 3.0f, btnY + 5.0f), menuLineCol, 1.8f);
            drawList.AddLine(new Vector2(menuBtnX + 3.0f, btnY + 10.0f), new Vector2(menuBtnX + btnSize - 3.0f, btnY + 10.0f), menuLineCol, 1.8f);
            drawList.AddLine(new Vector2(menuBtnX + 3.0f, btnY + 15.0f), new Vector2(menuBtnX + btnSize - 3.0f, btnY + 15.0f), menuLineCol, 1.8f);

            if (ImGui.BeginPopup("TitleMenuPopup" + popupSuffix))
            {
                if (ImGui.MenuItem(Loc.Tr("ImportExport")))
                {
                    _ctx.ShowImportExportModal = true;
                    _ctx.ImportStatusMessage = "";
                    _ctx.ExportStatusMessage = "";
                }
                if (ImGui.MenuItem(Loc.Tr("Settings")))
                {
                    _ctx.ShowSettingsModal = true;
                }
                ImGui.Separator();
                if (ImGui.MenuItem(Loc.Tr("Help")))
                {
                    _ctx.ShowHelpModal = true;
                }
                if (ImGui.MenuItem(Loc.Tr("CheckUpdates")))
                {
                    _ctx.ManualCheckMessage = Loc.Tr("CheckingUpdates");
                    _ctx.ManualCheckMessageTimer = 5.0f;
                    CheckForUpdatesInBackground(silent: false);
                }
                if (ImGui.MenuItem(Loc.Tr("About")))
                {
                    _ctx.ShowAboutModal = true;
                }
                ImGui.Separator();
                if (ImGui.MenuItem(Loc.Tr("ExitApp")))
                {
                    _ctx.IsOverlayOpen = false;
                }
                ImGui.EndPopup();
            }

            ImGui.SetCursorScreenPos(new Vector2(winPos.X, winPos.Y + titleH));
            ImGui.Spacing();
        }

        private void RenderFooter()
        {
            float footerH = 24.0f;
            float winW = ImGui.GetWindowWidth();
            float winH = ImGui.GetWindowHeight();
            Vector2 winPos = ImGui.GetWindowPos();

            var drawList = ImGui.GetWindowDrawList();
            Vector2 fMin = new Vector2(winPos.X, winPos.Y + winH - footerH);
            Vector2 fMax = new Vector2(winPos.X + winW, winPos.Y + winH);

            drawList.AddRectFilled(fMin, fMax, ImGui.ColorConvertFloat4ToU32(new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f)), 8.0f, ImDrawFlags.RoundCornersBottom);
            drawList.AddLine(fMin, new Vector2(fMax.X, fMin.Y), ImGui.ColorConvertFloat4ToU32(new Vector4(43f / 255f, 45f / 255f, 55f / 255f, 0.7f)));

            string authorText = _footerQuote;
            Vector2 textSize = ImGui.CalcTextSize(authorText);
            Vector2 textPos = new Vector2(fMin.X + (winW - textSize.X) * 0.5f, fMin.Y + (footerH - textSize.Y) * 0.5f);
            drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(92f / 255f, 95f / 255f, 102f / 255f, 1.0f)), authorText);

            float gripX = winPos.X + winW - 5.0f;
            float gripY = winPos.Y + winH - 5.0f;
            uint gripCol = ImGui.ColorConvertFloat4ToU32(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 0.85f));
            drawList.AddLine(new Vector2(gripX - 10.0f, gripY), new Vector2(gripX, gripY - 10.0f), gripCol, 1.5f);
            drawList.AddLine(new Vector2(gripX - 6.0f, gripY), new Vector2(gripX, gripY - 6.0f), gripCol, 1.5f);
            drawList.AddLine(new Vector2(gripX - 2.0f, gripY), new Vector2(gripX, gripY - 2.0f), gripCol, 1.5f);
        }

        private void SetupStyle()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 8.0f;
            style.ChildRounding = 6.0f;
            style.FrameRounding = 6.0f;
            style.PopupRounding = 6.0f;
            style.ScrollbarRounding = 4.0f;
            style.GrabRounding = 4.0f;
            style.WindowBorderSize = 1.0f;
            style.ChildBorderSize = 1.0f;
            style.FrameBorderSize = 1.0f;
            style.WindowPadding = new Vector2(8, 8);
            style.ItemSpacing = new Vector2(6, 6);
            style.FramePadding = new Vector2(8, 4);

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(17f / 255f, 18f / 255f, 20f / 255f, _ctx.Settings.WindowOpacity);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(30f / 255f, 30f / 255f, 46f / 255f, _ctx.Settings.WindowOpacity);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(24f / 255f, 25f / 255f, 32f / 255f, 0.98f);
            colors[(int)ImGuiCol.Border] = new Vector4(43f / 255f, 45f / 255f, 55f / 255f, 1.0f);
            colors[(int)ImGuiCol.Header] = new Vector4(35f / 255f, 36f / 255f, 40f / 255f, 1.0f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(45f / 255f, 47f / 255f, 54f / 255f, 1.0f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(55f / 255f, 58f / 255f, 66f / 255f, 1.0f);
            colors[(int)ImGuiCol.Button] = new Vector4(56f / 255f, 58f / 255f, 64f / 255f, 1.0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(71f / 255f, 74f / 255f, 81f / 255f, 1.0f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(56f / 255f, 58f / 255f, 64f / 255f, 1.0f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(66f / 255f, 69f / 255f, 76f / 255f, 1.0f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            colors[(int)ImGuiCol.PlotHistogram] = new Vector4(35f / 255f, 165f / 255f, 90f / 255f, 1.0f);
            colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(40f / 255f, 180f / 255f, 100f / 255f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 0.6f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(56f / 255f, 58f / 255f, 64f / 255f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f);
            colors[(int)ImGuiCol.Text] = new Vector4(242f / 255f, 243f / 255f, 245f / 255f, 1.0f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f);
        }

        public static async Task Main()
        {
            var overlay = new TdsImGuiOverlay();
            await overlay.Run();
        }
    }
}
