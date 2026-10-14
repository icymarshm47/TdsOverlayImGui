using System.Numerics;
using ImGuiNET;

namespace TdsOverlayImGui.UI.Views
{
    public static class ModalsView
    {
        public static void RenderAll(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            if (ctx.ShowAddMapModal) RenderAddMapModal(ctx);
            if (ctx.ShowImportExportModal) RenderImportExportModal(overlay, ctx);
            if (ctx.ShowSettingsModal) RenderSettingsModal(ctx);
            if (ctx.ShowAboutModal) RenderAboutModal(ctx);
            if (ctx.ShowHelpModal) RenderHelpModal(ctx);
            if (ctx.ShowDeleteConfirmModal) RenderDeleteConfirmModal(overlay, ctx);
            if (ctx.ShowUpdateModal) RenderUpdateModal(ctx);
        }

        private static void RenderAddMapModal(OverlayContext ctx)
        {
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
                            new StrategyStep { StartWave = 1, EndWave = 30, Instruction = "- [ ] First step instruction..." }
                        }
                    };

                    StrategyService.SaveStrategy(map);
                    ctx.Strategies = StrategyService.LoadStrategies();
                    ctx.SelectedMapIndex = ctx.Strategies.FindIndex(s => s.FilePath == map.FilePath);
                    if (ctx.SelectedMapIndex < 0) ctx.SelectedMapIndex = ctx.Strategies.Count - 1;
                    ctx.CurrentStepIndex = 0;
                    ctx.CurrentImageIndex = 0;
                    ctx.NewMapName = "";
                    ctx.NewMapDiff = "Fallen";
                    ctx.NewMapStrat = "Solo";
                    ctx.ShowAddMapModal = false;
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(120, 0)))
                {
                    ctx.ShowAddMapModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderImportExportModal(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            string popupId = $"{Loc.Tr("ImportExport")}###ImportExportModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowImportExportModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("ExportSection"));
                if (ctx.CurrentMap != null)
                {
                    var currentMap = ctx.CurrentMap;
                    ImGui.Text($"Strategy: {currentMap.DisplayName}");
                    ImGui.Spacing();

                    if (ImGui.Button(Loc.Tr("ExportZip"), new Vector2(220, 0)))
                    {
                        string defaultFileName = $"{currentMap.MapName}_{currentMap.Difficulty}_{currentMap.StrategyName}.zip";
                        string? targetPath = ImagePickerHelper.SaveFileDialog(defaultFileName, "ZIP Archive (*.zip)|*.zip", "zip", ctx.Settings.AlwaysOnTop);

                        if (!string.IsNullOrEmpty(targetPath))
                        {
                            if (StrategyService.ExportStrategyToZip(currentMap, ctx.CompletedTasks, targetPath))
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
                        string base64 = StrategyService.ExportStrategyToClipboardBase64(currentMap, ctx.CompletedTasks);
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

                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("ImportSection"));
                ImGui.TextDisabled(Loc.Tr("DragDropModalHint"));
                ImGui.Spacing();

                if (ImGui.Button(Loc.Tr("ImportFileBtn"), new Vector2(220, 0)))
                {
                    string? selectedFile = ImagePickerHelper.OpenFileDialog("Strategy Files (*.zip;*.json)|*.zip;*.json|All Files (*.*)|*.*", "zip", ctx.Settings.AlwaysOnTop);
                    if (!string.IsNullOrEmpty(selectedFile))
                    {
                        if (StrategyService.ImportStrategy(selectedFile, out string msg))
                        {
                            ImageViewer.ClearLoadedTextures(overlay);
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
                            ImageViewer.ClearLoadedTextures(overlay);
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
            string popupId = $"{Loc.Tr("Settings")}###SettingsModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowSettingsModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("LanguageSetting"));
                ImGui.Spacing();

                foreach (var langCode in Loc.GetAvailableLanguages())
                {
                    string displayName = Loc.GetLanguageDisplayName(langCode);
                    bool isSelected = Loc.CurrentLanguage.Equals(langCode, StringComparison.OrdinalIgnoreCase);
                    if (ImGui.RadioButton($"{displayName}##lang_{langCode}", isSelected))
                    {
                        Loc.CurrentLanguage = langCode;
                        ctx.Settings.Language = langCode;
                        StrategyService.SaveSettings(ctx.Settings);
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();

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

                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("WindowOpacitySetting"));
                ImGui.Spacing();

                float opacity = ctx.Settings.WindowOpacity;
                ImGui.SetNextItemWidth(250);
                if (ImGui.SliderFloat("##OpacitySlider", ref opacity, 0.2f, 1.0f, "%.2f"))
                {
                    ctx.Settings.WindowOpacity = opacity;
                }
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    StrategyService.SaveSettings(ctx.Settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("AlwaysOnTopSetting"));
                ImGui.Spacing();

                bool alwaysOnTop = ctx.Settings.AlwaysOnTop;
                if (ImGui.Checkbox($"{Loc.Tr("AlwaysOnTopSetting")}##AlwaysOnTopCheck", ref alwaysOnTop))
                {
                    ctx.Settings.AlwaysOnTop = alwaysOnTop;
                    TdsImGuiOverlay.SetAlwaysOnTop(alwaysOnTop);
                    StrategyService.SaveSettings(ctx.Settings);
                }

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(120, 0)))
                {
                    ctx.ShowSettingsModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderAboutModal(OverlayContext ctx)
        {
            string popupId = $"{Loc.Tr("AboutTitle")}###AboutModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowAboutModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), "TDS Strategy Overlay");
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
            string popupId = $"{Loc.Tr("HelpTitle")}###HelpModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowHelpModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Spacing();
                ImGui.Text(Loc.Tr("HelpLine1"));
                ImGui.Text(Loc.Tr("HelpLine2"));
                ImGui.Text(Loc.Tr("HelpLine3"));
                ImGui.Text(Loc.Tr("HelpLine4"));

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button(Loc.Tr("Close"), new Vector2(100, 0)))
                {
                    ctx.ShowHelpModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private static void RenderDeleteConfirmModal(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            if (ctx.CurrentMap == null) return;
            var currentMap = ctx.CurrentMap;

            string popupId = $"{Loc.Tr("DeleteConfirmTitle")}###DeleteConfirmModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowDeleteConfirmModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(string.Format(Loc.Tr("DeleteConfirmText"), currentMap.DisplayName));
                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.20f, 0.22f, 1.0f));
                if (ImGui.Button(Loc.Tr("YesDelete"), new Vector2(120, 0)))
                {
                    ImageViewer.ClearLoadedTextures(overlay);
                    StrategyService.DeleteStrategy(currentMap);
                    ctx.Strategies = StrategyService.LoadStrategies();
                    ctx.SelectedMapIndex = ctx.Strategies.Count > 0 ? 0 : -1;
                    ctx.CurrentStepIndex = 0;
                    ctx.CurrentImageIndex = 0;
                    ctx.IsEditing = false;
                    ctx.ShowDeleteConfirmModal = false;
                }
                ImGui.PopStyleColor();

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
            string popupId = $"{Loc.Tr("UpdateModalTitle")}###UpdateModal";
            ImGui.OpenPopup(popupId);
            if (ImGui.BeginPopupModal(popupId, ref ctx.ShowUpdateModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("UpdateNotice"));
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text($"{Loc.Tr("CurrentVersionLabel")} {TdsImGuiOverlay.CurrentAppVersion}");
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
