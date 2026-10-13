using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClickableTransparentOverlay;
using ImGuiNET;
using TdsOverlayImGui.Components;
using TdsOverlayImGui.Views;

namespace TdsOverlayImGui
{
    public class TdsImGuiOverlay : Overlay
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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
        public const string CurrentAppVersion = "0.6";

        private readonly OverlayContext _ctx = new();

        public TdsImGuiOverlay() : base("TDS Strategy Overlay", true)
        {
            VSync = true;

            Loc.LoadLanguages();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] fontCandidates = new[]
            {
                Path.Combine(baseDir, "fonts", "Roboto-Regular.ttf"),
                Path.Combine(baseDir, "Roboto-Regular.ttf"),
                Path.Combine("fonts", "Roboto-Regular.ttf"),
                "Roboto-Regular.ttf",
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

        private void HookDragAndDrop()
        {
            IntPtr hWnd = FindWindow(null, "TDS Strategy Overlay");
            if (hWnd == IntPtr.Zero || _oldWndProc != IntPtr.Zero) return;

            DragAcceptFiles(hWnd, true);
            _wndProcDelegate = CustomWndProc;
            IntPtr fnPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _oldWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, fnPtr);
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
                var (available, latestTag, url, error) = await UpdateService.CheckForUpdatesAsync(
                    GitHubOwner, GitHubRepo, CurrentAppVersion);

                if (available)
                {
                    _ctx.LatestVersionTag = latestTag;
                    _ctx.ReleaseUrl = url;
                    _ctx.ShowUpdateModal = true;
                }
                else if (!silent)
                {
                    _ctx.ManualCheckMessage = string.IsNullOrEmpty(error) ? Loc.Tr("NoUpdatesNotice") : error;
                    _ctx.ManualCheckMessageTimer = 5.0f;
                }
            });
        }

        protected override void Render()
        {
            if (_ctx.IsFirstFrame)
            {
                SetAlwaysOnTop(_ctx.Settings.AlwaysOnTop);
                HookDragAndDrop();
                _ctx.IsFirstFrame = false;
            }

            if (!_ctx.IsOverlayOpen)
            {
                ImageViewer.ClearLoadedTextures(this);
                Close();
                return;
            }

            ProcessDroppedFiles();
            SetupStyle();
            OcrOverlayView.ProcessBackgroundOcr(_ctx);

            ImGui.SetNextWindowSize(new Vector2(540, 640), ImGuiCond.FirstUseEver);

            bool isOpen = _ctx.IsOverlayOpen;
            ImGui.Begin("TDS Strategy Overlay (ImGui)", ref isOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar);
            _ctx.IsOverlayOpen = isOpen;

            RenderMenuBar();

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

            ImGui.End();

            if (_ctx.Settings.SeparateImageWindow && !_ctx.IsEditing) ImageViewer.RenderSeparateImageWindow(this, _ctx);
            if (_ctx.IsSelectingOcrRegion) OcrOverlayView.RenderSelectionOverlay(_ctx);

            ToastAndAudioService.RenderDjToast(_ctx);
            ModalsView.RenderAll(this, _ctx);
        }

        private void RenderMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu(Loc.Tr("File")))
                {
                    if (ImGui.MenuItem(Loc.Tr("ImportExport")))
                    {
                        _ctx.ShowImportExportModal = true;
                        _ctx.ImportStatusMessage = "";
                        _ctx.ExportStatusMessage = "";
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.Tr("Settings")))
                    {
                        _ctx.ShowSettingsModal = true;
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.Tr("ExitApp")))
                    {
                        _ctx.IsOverlayOpen = false;
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu(Loc.Tr("Other")))
                {
                    bool isCompact = _ctx.Settings.CompactMode;
                    if (ImGui.MenuItem(Loc.Tr("CompactMode"), "", ref isCompact))
                    {
                        _ctx.Settings.CompactMode = isCompact;
                        StrategyService.SaveSettings(_ctx.Settings);
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.Tr("Help")))
                    {
                        _ctx.ShowHelpModal = true;
                    }

                    if (ImGui.MenuItem(Loc.Tr("CheckUpdates")))
                    {
                        _ctx.ManualCheckMessage = "Checking...";
                        _ctx.ManualCheckMessageTimer = 5.0f;
                        CheckForUpdatesInBackground(silent: false);
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.Tr("About")))
                    {
                        _ctx.ShowAboutModal = true;
                    }

                    ImGui.EndMenu();
                }

                float hintW = ImGui.CalcTextSize(Loc.Tr("DragDropHint")).X + 16.0f;
                if (ImGui.GetWindowWidth() > 380)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - hintW);
                    ImGui.TextDisabled(Loc.Tr("DragDropHint"));
                }

                ImGui.EndMenuBar();
            }
        }

        private void SetupStyle()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 10.0f;
            style.ChildRounding = 8.0f;
            style.FrameRounding = 6.0f;
            style.PopupRounding = 8.0f;
            style.ScrollbarRounding = 6.0f;
            style.GrabRounding = 5.0f;
            style.WindowBorderSize = 1.0f;
            style.ChildBorderSize = 1.0f;
            style.ItemSpacing = new Vector2(8, 6);
            style.FramePadding = new Vector2(8, 4);

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.08f, 0.09f, 0.11f, _ctx.Settings.WindowOpacity);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.11f, 0.12f, 0.15f, _ctx.Settings.WindowOpacity);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.10f, 0.11f, 0.14f, 0.98f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.22f, 0.24f, 0.30f, 0.70f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.32f, 0.38f, 0.90f, 1.0f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.26f, 0.31f, 0.78f, 1.0f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.28f, 0.33f, 0.82f, 1.0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.36f, 0.42f, 0.95f, 1.0f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.22f, 0.26f, 0.68f, 1.0f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.15f, 0.16f, 0.20f, 1.0f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.24f, 0.26f, 0.33f, 1.0f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.35f, 0.95f, 0.50f, 1.0f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.36f, 0.42f, 0.95f, 1.0f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.45f, 0.52f, 1.0f, 1.0f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.09f, 0.10f, 0.13f, 1.0f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.14f, 0.16f, 0.22f, 1.0f);
        }

        public static async Task Main()
        {
            var overlay = new TdsImGuiOverlay();
            await overlay.Run();
        }
    }
}
