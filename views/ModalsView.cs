using System;
using System.Numerics;
using ImGuiNET;
using TdsOverlayImGui.Components;

namespace TdsOverlayImGui.Views
{
    public static class ModalsView
    {
        public static void RenderAll(OverlayContext ctx)
        {
            if (ctx.ShowAddMapModal) RenderAddMapModal(ctx);
            if (ctx.ShowImportExportModal) RenderImportExportModal(ctx);
            if (ctx.ShowSettingsModal) RenderSettingsModal(ctx);
            if (ctx.ShowAboutModal) RenderAboutModal(ctx);
            if (ctx.ShowHelpModal) RenderHelpModal(ctx);
            if (ctx.ShowDeleteConfirmModal) RenderDeleteConfirmModal(ctx);
            if (ctx.ShowUpdateModal) RenderUpdateModal(ctx);
        }

        private static void RenderAddMapModal(OverlayContext ctx)
        {
            ImGui.OpenPopup(Loc.Tr("Create"));
            if (ImGui.BeginPopupModal(Loc.Tr("Create"), ref ctx.ShowAddMapModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"{Loc.Tr("StrategyVariant")}:");
                ImGui.InputText("##NewMapStrat", ref ctx.NewMapStrat, 500000);

                ImGui.Text($"{Loc.Tr("MapName")}:");
                ImGui.InputText("##NewMapName", ref ctx.NewMapName, 500000);

                ImGui.Text($"{Loc.Tr("Difficulty")}:");
                ImGui.InputText("##NewMapDiff", ref ctx.NewMapDiff, 500000);

                if (ImGui.Button(Loc.Tr("Create"), new Vector2(100, 0)))
                {
                    var map = new MapStrategy
                    {
                        MapName = string.IsNullOrWhiteSpace(ctx.NewMapName) ? "New Map" : ctx.NewMapName,
                        Difficulty = ctx.NewMapDiff,
                        StrategyName = string.IsNullOrWhiteSpace(ctx.NewMapStrat) ? "Solo" : ctx.NewMapStrat,
                        GeneralInfo = "",
                        ImagePaths = new(),
                        Steps = new()
                        {
                            new StrategyStep { StartWave = 1, EndWave = 30, Instruction = "First step instruction..." }
                        }
                    };

                    StrategyService.SaveStrategy(map);
                    ctx.Strategies = StrategyService.LoadStrategies();
                    ctx.SelectedMapIndex = ctx.Strategies.Count - 1;
                    ctx.CurrentStepIndex = 0;
                    ctx.CurrentImageIndex = 0;
                    ctx.ShowAddMapModal = false;
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(100, 0)))
                {
                    ctx.ShowAddMapModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderImportExportModal(OverlayContext ctx)
        {
            ImGui.OpenPopup(Loc.Tr("ImportExport"));
            if (ImGui.BeginPopupModal(Loc.Tr("ImportExport"), ref ctx.ShowImportExportModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ExportSection"));
                if (ctx.CurrentMap != null)
                {
                    var currentMap = ctx.CurrentMap;
                    ImGui.Text($"Strategy: {currentMap.DisplayName}");
                    ImGui.Spacing();

                    if (ImGui.Button(Loc.Tr("ExportZip"), new Vector2(220, 0)))
                    {
                        string defaultFileName = $"{currentMap.MapName}_{currentMap.Difficulty}_{currentMap.StrategyName}.zip";
                        string? targetPath = ImagePickerHelper.SaveFileDialog(defaultFileName, "ZIP Archive (*.zip)|*.zip", "zip");

                        if (!string.IsNullOrEmpty(targetPath))
                        {
                            if (StrategyService.ExportStrategyToZip(currentMap, ctx.SelectedMapIndex, ctx.CompletedTasks, targetPath))
                            {
                                ctx.ExportStatusMessage = $"Saved successfully to:\n{targetPath}";
                            }
                            else
                            {
                                ctx.ExportStatusMessage = "Export error!";
                            }
                        }
                    }

                    ImGui.SameLine();

                    if (ImGui.Button(Loc.Tr("ExportClipboard"), new Vector2(220, 0)))
                    {
                        string base64 = StrategyService.ExportStrategyToClipboardBase64(currentMap, ctx.SelectedMapIndex, ctx.CompletedTasks);
                        ImagePickerHelper.SetClipboardText(base64);
                        ctx.ExportStatusMessage = Loc.Tr("ClipboardExportSuccess");
                    }

                    if (!string.IsNullOrEmpty(ctx.ExportStatusMessage))
                    {
                        InstructionRenderer.SafeTextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), ctx.ExportStatusMessage);
                    }
                }
                else
                {
                    ImGui.TextDisabled("(Select a strategy in the main window)");
                }

                ImGui.Separator();
                ImGui.Spacing();

                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImportSection"));
                ImGui.Spacing();

                if (ImGui.Button(Loc.Tr("ImportFileBtn"), new Vector2(220, 0)))
                {
                    string? selectedFile = ImagePickerHelper.OpenFileDialog("Strategy Files (*.zip;*.json)|*.zip;*.json|All Files (*.*)|*.*", "zip");
                    if (!string.IsNullOrEmpty(selectedFile))
                    {
                        if (StrategyService.ImportStrategy(selectedFile, out string msg))
                        {
                            ctx.ImportStatusMessage = msg;
                            ctx.Strategies = StrategyService.LoadStrategies();
                            ctx.SelectedMapIndex = ctx.Strategies.Count - 1;
                        }
                        else
                        {
                            ctx.ImportStatusMessage = msg;
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
                            ctx.ImportStatusMessage = msg;
                            ctx.Strategies = StrategyService.LoadStrategies();
                            ctx.SelectedMapIndex = ctx.Strategies.Count - 1;
                        }
                        else
                        {
                            ctx.ImportStatusMessage = msg;
                        }
                    }
                    else
                    {
                        ctx.ImportStatusMessage = Loc.Tr("ClipboardImportError");
                    }
                }

                if (!string.IsNullOrEmpty(ctx.ImportStatusMessage))
                {
                    InstructionRenderer.SafeTextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), ctx.ImportStatusMessage);
                }

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    ctx.ShowImportExportModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderSettingsModal(OverlayContext ctx)
        {
            ImGui.OpenPopup(Loc.Tr("Settings"));
            if (ImGui.BeginPopupModal(Loc.Tr("Settings"), ref ctx.ShowSettingsModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("LanguageSetting"));
                ImGui.Spacing();

                foreach (var langCode in Loc.GetAvailableLanguages())
                {
                    string displayName = Loc.GetLanguageDisplayName(langCode);
                    if (ImGui.RadioButton(displayName, Loc.CurrentLanguage.Equals(langCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        Loc.CurrentLanguage = langCode;
                        ctx.Settings.Language = langCode;
                        StrategyService.SaveSettings(ctx.Settings);
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();

                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImageModeSetting"));
                ImGui.Spacing();

                bool separate = ctx.Settings.SeparateImageWindow;

                if (ImGui.RadioButton(Loc.Tr("SeparateWindowMode"), separate))
                {
                    ctx.Settings.SeparateImageWindow = true;
                    StrategyService.SaveSettings(ctx.Settings);
                }

                if (ImGui.RadioButton(Loc.Tr("EmbeddedMode"), !separate))
                {
                    ctx.Settings.SeparateImageWindow = false;
                    StrategyService.SaveSettings(ctx.Settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("WindowOpacitySetting"));
                ImGui.Spacing();

                float opacity = ctx.Settings.WindowOpacity;
                ImGui.SetNextItemWidth(250);
                if (ImGui.SliderFloat("##OpacitySlider", ref opacity, 0.1f, 1.0f, "%.2f"))
                {
                    ctx.Settings.WindowOpacity = opacity;
                    StrategyService.SaveSettings(ctx.Settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    ctx.ShowSettingsModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderAboutModal(OverlayContext ctx)
        {
            ImGui.OpenPopup(Loc.Tr("AboutTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("AboutTitle"), ref ctx.ShowAboutModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), "TDS Strategy Overlay");
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(Loc.Tr("AboutVersion"));
                ImGui.Text(Loc.Tr("AboutAuthor"));
                ImGui.TextDisabled(Loc.Tr("AboutDesc"));

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    ctx.ShowAboutModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderHelpModal(OverlayContext ctx)
        {
            ImGui.OpenPopup(Loc.Tr("HelpTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("HelpTitle"), ref ctx.ShowHelpModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("HelpTitle"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(Loc.Tr("HelpLine1"));
                ImGui.Text(Loc.Tr("HelpLine2"));
                ImGui.Text(Loc.Tr("HelpLine3"));

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    ctx.ShowHelpModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderDeleteConfirmModal(OverlayContext ctx)
        {
            if (ctx.CurrentMap == null) return;
            var currentMap = ctx.CurrentMap;

            ImGui.OpenPopup(Loc.Tr("DeleteConfirmTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("DeleteConfirmTitle"), ref ctx.ShowDeleteConfirmModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(string.Format(Loc.Tr("DeleteConfirmText"), currentMap.DisplayName));
                ImGui.Spacing();

                if (ImGui.Button(Loc.Tr("YesDelete"), new Vector2(120, 0)))
                {
                    StrategyService.DeleteStrategy(currentMap);
                    ctx.Strategies = StrategyService.LoadStrategies();
                    ctx.SelectedMapIndex = -1;
                    ctx.CurrentStepIndex = 0;
                    ctx.CurrentImageIndex = 0;
                    ctx.IsEditing = false;
                    ctx.ShowDeleteConfirmModal = false;
                }

                ImGui.SameLine();

                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(100, 0)))
                {
                    ctx.ShowDeleteConfirmModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderUpdateModal(OverlayContext ctx)
        {
            ImGui.OpenPopup(Loc.Tr("UpdateModalTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("UpdateModalTitle"), ref ctx.ShowUpdateModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("UpdateNotice"));
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text($"{Loc.Tr("CurrentVersionLabel")} 0.4");
                InstructionRenderer.SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"{Loc.Tr("LatestVersionLabel")} {ctx.LatestVersionTag}");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button(Loc.Tr("DownloadUpdateBtn"), new Vector2(160, 0)))
                {
                    UpdateService.OpenUrlInBrowser(ctx.ReleaseUrl);
                    ctx.ShowUpdateModal = false;
                }

                ImGui.SameLine();

                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(100, 0)))
                {
                    ctx.ShowUpdateModal = false;
                }

                ImGui.EndPopup();
            }
        }
    }
}