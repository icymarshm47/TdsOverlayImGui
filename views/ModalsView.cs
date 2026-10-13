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
<<<<<<< HEAD
            string popupId = $"{Loc.Tr("Create")}###AddMapModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowAddMapModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"{Loc.Tr("StrategyVariant")}:");
                ImGui.InputText("##NewMapStrat", ref ctx.NewMapStrat, 256);

                ImGui.Text($"{Loc.Tr("MapName")}:");
                ImGui.InputText("##NewMapName", ref ctx.NewMapName, 256);

                ImGui.Text($"{Loc.Tr("Difficulty")}:");
                ImGui.InputText("##NewMapDiff", ref ctx.NewMapDiff, 256);

                if (ImGui.Button(Loc.Tr("Create"), new Vector2(120, 0)))
=======
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
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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
<<<<<<< HEAD
                            new StrategyStep { StartWave = 1, EndWave = 30, Instruction = "- [ ] First step instruction..." }
=======
                            new StrategyStep { StartWave = 1, EndWave = 30, Instruction = "First step instruction..." }
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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
<<<<<<< HEAD
                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(120, 0)))
=======
                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(100, 0)))
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
                {
                    ctx.ShowAddMapModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderImportExportModal(OverlayContext ctx)
        {
<<<<<<< HEAD
            string popupId = $"{Loc.Tr("ImportExport")}###ImportExportModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowImportExportModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("ExportSection"));
=======
            ImGui.OpenPopup(Loc.Tr("ImportExport"));
            if (ImGui.BeginPopupModal(Loc.Tr("ImportExport"), ref ctx.ShowImportExportModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ExportSection"));
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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

<<<<<<< HEAD
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("ImportSection"));
                ImGui.TextDisabled(Loc.Tr("DragDropModalHint"));
=======
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImportSection"));
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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
<<<<<<< HEAD
            string popupId = $"{Loc.Tr("Settings")}###SettingsModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowSettingsModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("LanguageSetting"));
=======
            ImGui.OpenPopup(Loc.Tr("Settings"));
            if (ImGui.BeginPopupModal(Loc.Tr("Settings"), ref ctx.ShowSettingsModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("LanguageSetting"));
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
                ImGui.Spacing();

                foreach (var langCode in Loc.GetAvailableLanguages())
                {
                    string displayName = Loc.GetLanguageDisplayName(langCode);
<<<<<<< HEAD
                    bool isSelected = Loc.CurrentLanguage.Equals(langCode, StringComparison.OrdinalIgnoreCase);
                    if (ImGui.RadioButton($"{displayName}##lang_{langCode}", isSelected))
=======
                    if (ImGui.RadioButton(displayName, Loc.CurrentLanguage.Equals(langCode, StringComparison.OrdinalIgnoreCase)))
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
                    {
                        Loc.CurrentLanguage = langCode;
                        ctx.Settings.Language = langCode;
                        StrategyService.SaveSettings(ctx.Settings);
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();

<<<<<<< HEAD
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("AudioAndOcrSetting"));
                ImGui.Spacing();

                bool ocrSound = ctx.Settings.EnableOcrSound;
                if (ImGui.Checkbox(Loc.Tr("EnableOcrSoundSetting"), ref ocrSound))
                {
                    ctx.Settings.EnableOcrSound = ocrSound;
                    StrategyService.SaveSettings(ctx.Settings);
                }

                bool ocrPreprocess = ctx.Settings.OcrPreprocess;
                if (ImGui.Checkbox(Loc.Tr("OcrPreprocessSetting"), ref ocrPreprocess))
                {
                    ctx.Settings.OcrPreprocess = ocrPreprocess;
                    StrategyService.SaveSettings(ctx.Settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("ImageModeSetting"));
=======
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImageModeSetting"));
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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

<<<<<<< HEAD
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("WindowOpacitySetting"));
=======
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("WindowOpacitySetting"));
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
                ImGui.Spacing();

                float opacity = ctx.Settings.WindowOpacity;
                ImGui.SetNextItemWidth(250);
<<<<<<< HEAD
                if (ImGui.SliderFloat("##OpacitySlider", ref opacity, 0.2f, 1.0f, "%.2f"))
                {
                    ctx.Settings.WindowOpacity = opacity;
                }
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
=======
                if (ImGui.SliderFloat("##OpacitySlider", ref opacity, 0.1f, 1.0f, "%.2f"))
                {
                    ctx.Settings.WindowOpacity = opacity;
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
                    StrategyService.SaveSettings(ctx.Settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

<<<<<<< HEAD
                if (ImGui.Button(Loc.Tr("Close"), new Vector2(120, 0)))
=======
                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
                {
                    ctx.ShowSettingsModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderAboutModal(OverlayContext ctx)
        {
<<<<<<< HEAD
            string popupId = $"{Loc.Tr("AboutTitle")}###AboutModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowAboutModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), "TDS Strategy Overlay");
=======
            ImGui.OpenPopup(Loc.Tr("AboutTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("AboutTitle"), ref ctx.ShowAboutModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), "TDS Strategy Overlay");
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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
<<<<<<< HEAD
            string popupId = $"{Loc.Tr("HelpTitle")}###HelpModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowHelpModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Spacing();
=======
            ImGui.OpenPopup(Loc.Tr("HelpTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("HelpTitle"), ref ctx.ShowHelpModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("HelpTitle"));
                ImGui.Separator();
                ImGui.Spacing();

>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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

<<<<<<< HEAD
            string popupId = $"{Loc.Tr("DeleteConfirmTitle")}###DeleteConfirmModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowDeleteConfirmModal, ImGuiWindowFlags.AlwaysAutoResize))
=======
            ImGui.OpenPopup(Loc.Tr("DeleteConfirmTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("DeleteConfirmTitle"), ref ctx.ShowDeleteConfirmModal, ImGuiWindowFlags.AlwaysAutoResize))
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
            {
                ImGui.Text(string.Format(Loc.Tr("DeleteConfirmText"), currentMap.DisplayName));
                ImGui.Spacing();

<<<<<<< HEAD
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.20f, 0.22f, 1.0f));
=======
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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
<<<<<<< HEAD
                ImGui.PopStyleColor();
=======
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5

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
<<<<<<< HEAD
            string popupId = $"{Loc.Tr("UpdateModalTitle")}###UpdateModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowUpdateModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("UpdateNotice"));
=======
            ImGui.OpenPopup(Loc.Tr("UpdateModalTitle"));
            if (ImGui.BeginPopupModal(Loc.Tr("UpdateModalTitle"), ref ctx.ShowUpdateModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("UpdateNotice"));
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

<<<<<<< HEAD
                ImGui.Text($"{Loc.Tr("CurrentVersionLabel")} {TdsImGuiOverlay.CurrentAppVersion}");
=======
                ImGui.Text($"{Loc.Tr("CurrentVersionLabel")} 0.4");
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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
<<<<<<< HEAD
}
=======
}
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
