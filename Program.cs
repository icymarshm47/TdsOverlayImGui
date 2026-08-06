using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
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

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        private const string GitHubOwner = "icymarshm47";
        private const string GitHubRepo = "TdsOverlayImGui";
        private const string CurrentAppVersion = "0.5";

        private readonly OverlayContext _ctx = new();

        public TdsImGuiOverlay() : base("TDS Strategy Overlay", true)
        {
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
                SetAlwaysOnTop(true);
                _ctx.IsFirstFrame = false;
            }

            if (!_ctx.IsOverlayOpen)
            {
                Close();
                return;
            }

            SetupStyle();
            OcrOverlayView.ProcessBackgroundOcr(_ctx);

            ImGui.SetNextWindowSize(new Vector2(520, 600), ImGuiCond.FirstUseEver);

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

            ImGui.Spacing();

            if (_ctx.Strategies.Count == 0)
            {
                ImGui.Text("No strategies found.");
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
                    StrategyEditorView.Render(_ctx);
            }

            ImGui.End();

            if (_ctx.Settings.SeparateImageWindow) ImageViewer.RenderSeparateImageWindow(this, _ctx);
            if (_ctx.IsSelectingOcrRegion) OcrOverlayView.RenderSelectionOverlay(_ctx);
            
            ToastAndAudioService.RenderDjToast(_ctx);
            ModalsView.RenderAll(_ctx);
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

                ImGui.EndMenuBar();
            }
        }

        private void SetupStyle()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 8.0f;
            style.FrameRounding = 6.0f;
            style.PopupRounding = 6.0f;
            style.ScrollbarRounding = 6.0f;

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.07f, 0.08f, 0.09f, _ctx.Settings.WindowOpacity);
            colors[(int)ImGuiCol.Header] = new Vector4(0.18f, 0.19f, 0.22f, 1.0f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.28f, 0.22f, 0.77f, 1.0f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.22f, 0.25f, 0.60f, 1.0f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.14f, 0.15f, 0.17f, 1.0f);
        }

        public static async Task Main()
        {
            var overlay = new TdsImGuiOverlay();
            await overlay.Run();
        }
    }
}