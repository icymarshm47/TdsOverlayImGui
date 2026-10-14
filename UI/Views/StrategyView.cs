using System.IO;
using System.Numerics;
using ImGuiNET;

namespace TdsOverlayImGui.UI.Views
{
    public static class StrategyView
    {
        public static void Render(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            if (!ctx.Settings.CompactMode)
            {
                ImGui.BeginChild("MapSelectorCard", new Vector2(0, 96), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

                ImGui.TextDisabled(Loc.Tr("SelectStrategyHeader"));

                string previewText = ctx.CurrentMap != null ? ctx.CurrentMap.DisplayName : Loc.Tr("SelectStrategyCombo");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                if (ImGui.BeginCombo("##MapSelect", previewText))
                {
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##StratSearch", Loc.Tr("SearchPlaceholder"), ref ctx.StrategySearchFilter, 128);
                    ImGui.Separator();

                    for (int i = 0; i < ctx.Strategies.Count; i++)
                    {
                        string displayName = ctx.Strategies[i].DisplayName;
                        if (!string.IsNullOrWhiteSpace(ctx.StrategySearchFilter) &&
                            !displayName.Contains(ctx.StrategySearchFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        bool isSelected = (ctx.SelectedMapIndex == i);
                        if (ImGui.Selectable(displayName, isSelected))
                        {
                            if (ctx.SelectedMapIndex != i)
                            {
                                ImageViewer.ClearLoadedTextures(overlay);
                            }
                            ctx.SelectedMapIndex = i;
                            ctx.CurrentStepIndex = 0;
                            ctx.CurrentImageIndex = 0;
                            ctx.IsEditing = false;
                            ctx.TriggeredDjAlerts.Clear();
                            ctx.TriggeredOcrSounds.Clear();
                            ctx.PreviousWaveNumber = 1;
                            ctx.ResetImageTransform();
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }

                    ImGui.EndCombo();
                }

                float availWCard = ImGui.GetContentRegionAvail().X;
                float spacingCard = ImGui.GetStyle().ItemSpacing.X;

                if (ctx.CurrentMap != null)
                {
                    float btnW = (availWCard - spacingCard * 2) / 3.0f;

                    if (ImGui.Button(Loc.Tr("AddStrategy"), new Vector2(btnW, 0)))
                    {
                        ctx.ShowAddMapModal = true;
                    }

                    ImGui.SameLine();
                    var map = ctx.CurrentMap;
                    string editBtnText = ctx.IsEditing ? Loc.Tr("ViewMode") : Loc.Tr("EditStrategy");
                    if (ImGui.Button(editBtnText, new Vector2(btnW, 0)))
                    {
                        if (!ctx.IsEditing)
                        {
                            ctx.BeginEditing(map);
                        }
                        else
                        {
                            ctx.IsEditing = false;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.20f, 0.22f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.80f, 0.25f, 0.28f, 1.0f));
                    if (ImGui.Button(Loc.Tr("DeleteStrategy"), new Vector2(btnW, 0)))
                    {
                        ctx.ShowDeleteConfirmModal = true;
                    }
                    ImGui.PopStyleColor(2);
                }
                else
                {
                    if (ImGui.Button(Loc.Tr("AddStrategy"), new Vector2(availWCard, 0)))
                    {
                        ctx.ShowAddMapModal = true;
                    }
                }

                ImGui.EndChild();
                ImGui.Spacing();
            }

            if (ctx.CurrentMap == null)
            {
                ImGui.Spacing();
                InstructionRenderer.SafeTextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), Loc.Tr("NoStrategySelected"));
                ImGui.TextWrapped(Loc.Tr("SelectStrategyPrompt"));
                return;
            }

            if (!ctx.Settings.EnableOcr)
            {
                ctx.DetectedWaveNumber = null;
            }

            var currentMap = ctx.CurrentMap;
            string mapTaskId = currentMap.TaskId;
            int activeWave = (ctx.Settings.EnableOcr && ctx.DetectedWaveNumber.HasValue)
                ? ctx.DetectedWaveNumber.Value
                : ctx.CurrentWaveNumber;

            if (!ctx.Settings.CompactMode)
            {
                ImGui.BeginChild("OcrHeaderCard", new Vector2(0, 44), true, ImGuiWindowFlags.NoScrollbar);

                bool enableOcr = ctx.Settings.EnableOcr;
                if (ImGui.Checkbox(Loc.Tr("AutoOcrHeader"), ref enableOcr))
                {
                    ctx.Settings.EnableOcr = enableOcr;
                    if (!enableOcr)
                    {
                        ctx.DetectedWaveNumber = null;
                    }
                    else
                    {
                        ctx.LastOcrScanTime = DateTime.MinValue;
                    }
                    StrategyService.SaveSettings(ctx.Settings);
                }

                ImGui.SameLine();
                if (ctx.Settings.EnableOcr)
                {
                    InstructionRenderer.SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), Loc.Tr("OcrStatusOn"));
                }
                else
                {
                    InstructionRenderer.SafeTextColored(new Vector4(0.55f, 0.58f, 0.65f, 1.0f), Loc.Tr("OcrStatusOff"));
                }

                string ocrBtnText = Loc.Tr("SelectOcrRegionBtn");
                float ocrBtnW = ImGui.CalcTextSize(ocrBtnText).X + ImGui.GetStyle().FramePadding.X * 2 + 8.0f;
                if (ImGui.GetContentRegionAvail().X > ocrBtnW)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - ocrBtnW - 10.0f);
                }
                else
                {
                    ImGui.SameLine();
                }

                if (ImGui.Button(ocrBtnText))
                {
                    ctx.IsSelectingOcrRegion = true;
                    ctx.OcrSelectionState = 0;
                }

                ImGui.EndChild();
                ImGui.Spacing();
            }

            if (!ctx.IsEditing)
            {
                if (currentMap.Steps.Count > 0)
                {
                    ctx.CurrentStepIndex = Math.Clamp(ctx.CurrentStepIndex, 0, currentMap.Steps.Count - 1);
                    var step = currentMap.Steps[ctx.CurrentStepIndex];

                    ImGui.BeginChild("StepProgressCard", new Vector2(0, 118), true, ImGuiWindowFlags.NoScrollbar);

                    InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("CurrentWaveHeader"));
                    ImGui.SameLine();

                    InstructionRenderer.SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"[ {activeWave} ]");

                    if (ctx.Settings.EnableOcr)
                    {
                        ImGui.SameLine();
                        if (ctx.DetectedWaveNumber.HasValue)
                        {
                            InstructionRenderer.SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), Loc.Tr("AutoOcrTag"));
                        }
                        else
                        {
                            ImGui.TextDisabled(Loc.Tr("OcrScanningTag"));
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(95);
                    int waveNum = ctx.CurrentWaveNumber;
                    if (ImGui.InputInt("##ManualWaveInput", ref waveNum, 1, 5))
                    {
                        ctx.CurrentWaveNumber = Math.Clamp(waveNum, 1, 200);
                        ctx.DetectedWaveNumber = null;
                        if (ctx.CurrentWaveNumber == 1)
                        {
                            ctx.TriggeredDjAlerts.Clear();
                            ctx.TriggeredOcrSounds.Clear();
                        }
                        AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                        activeWave = ctx.CurrentWaveNumber;
                        ctx.UpdateStepByWaveNumber(currentMap, ctx.CurrentWaveNumber);
                    }

                    float progress = (float)(ctx.CurrentStepIndex + 1) / currentMap.Steps.Count;
                    ImGui.ProgressBar(progress, new Vector2(-1, 6), "");

                    ImGui.TextDisabled($"{Loc.Tr("Step")} {ctx.CurrentStepIndex + 1} {Loc.Tr("Of")} {currentMap.Steps.Count}");
                    ImGui.SameLine();

                    bool isOcrMatched = activeWave >= step.StartWave && activeWave <= step.EndWave;
                    Vector4 waveColor = isOcrMatched ? new Vector4(0.3f, 1.0f, 0.4f, 1.0f) : new Vector4(0.45f, 0.52f, 1.0f, 1.0f);
                    InstructionRenderer.SafeTextColored(waveColor, $"{Loc.Tr("Waves")} {step.StartWave} - {step.EndWave}");

                    if (isOcrMatched)
                    {
                        ImGui.SameLine();
                        InstructionRenderer.SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), Loc.Tr("ActiveInGame"));
                    }

                    float navAvailW = ImGui.GetContentRegionAvail().X;
                    float navSpacing = ImGui.GetStyle().ItemSpacing.X;
                    float navBtnW = (navAvailW - navSpacing * 3) / 4.0f;

                    if (ImGui.Button("|<", new Vector2(navBtnW, 0)))
                    {
                        ctx.CurrentStepIndex = 0;
                        ctx.CurrentWaveNumber = currentMap.Steps[0].StartWave;
                        ctx.DetectedWaveNumber = null;
                        AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                        ctx.ResetImageTransform();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Tr("PrevStep"), new Vector2(navBtnW, 0)))
                    {
                        if (ctx.CurrentStepIndex > 0)
                        {
                            ctx.CurrentStepIndex--;
                            ctx.CurrentWaveNumber = currentMap.Steps[ctx.CurrentStepIndex].StartWave;
                            ctx.DetectedWaveNumber = null;
                            AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                            ctx.ResetImageTransform();
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Tr("NextStep"), new Vector2(navBtnW, 0)))
                    {
                        if (ctx.CurrentStepIndex < currentMap.Steps.Count - 1)
                        {
                            ctx.CurrentStepIndex++;
                            ctx.CurrentWaveNumber = currentMap.Steps[ctx.CurrentStepIndex].StartWave;
                            ctx.DetectedWaveNumber = null;
                            AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                            ctx.ResetImageTransform();
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(">|", new Vector2(navBtnW, 0)))
                    {
                        ctx.CurrentStepIndex = currentMap.Steps.Count - 1;
                        ctx.CurrentWaveNumber = currentMap.Steps[^1].StartWave;
                        ctx.DetectedWaveNumber = null;
                        AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                        ctx.ResetImageTransform();
                    }

                    ImGui.EndChild();
                    ImGui.Spacing();
                }

                if (!string.IsNullOrWhiteSpace(currentMap.GeneralInfo))
                {
                    InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("GeneralInfo"));
                    float infoH = ctx.Settings.GeneralInfoBoxHeight;
                    ImGui.BeginChild("GeneralInfoScroll", new Vector2(0, infoH), true);

                    string[] lines = currentMap.GeneralInfo.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                    {
                        string rawLine = lines[lineIdx];
                        string taskKey = $"gen_{mapTaskId}_{lineIdx}";
                        InstructionRenderer.RenderInstructionLine(ctx, rawLine, activeWave, taskKey);
                    }

                    ImGui.EndChild();

                    ctx.Settings.GeneralInfoBoxHeight = ImageViewer.DrawHeightResizeHandle(ctx, ctx.Settings.GeneralInfoBoxHeight, 40.0f, 400.0f, "GeneralInfoHandle");
                    ImGui.Spacing();
                }

                if (currentMap.Steps.Count > 0)
                {
                    ctx.CurrentStepIndex = Math.Clamp(ctx.CurrentStepIndex, 0, currentMap.Steps.Count - 1);
                    var step = currentMap.Steps[ctx.CurrentStepIndex];

                    string headerText = Loc.Tr("InstructionHeader");
                    InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), headerText);

                    if (!ctx.Settings.CompactMode)
                    {
                        string copyText = Loc.Tr("CopyInstruction");
                        string clearText = Loc.Tr("ClearChecks");

                        float copyW = ImGui.CalcTextSize(copyText).X + ImGui.GetStyle().FramePadding.X * 2 + 6.0f;
                        float clearW = ImGui.CalcTextSize(clearText).X + ImGui.GetStyle().FramePadding.X * 2 + 6.0f;
                        float totalBtnW = copyW + clearW + ImGui.GetStyle().ItemSpacing.X;

                        float availW = ImGui.GetContentRegionAvail().X;
                        float headerTextW = ImGui.CalcTextSize(headerText).X;

                        if (availW >= headerTextW + totalBtnW + ImGui.GetStyle().ItemSpacing.X * 2)
                        {
                            ImGui.SameLine(ImGui.GetWindowWidth() - totalBtnW - 12.0f);
                        }

                        if (ImGui.Button(copyText, new Vector2(copyW, 0)))
                        {
                            ImagePickerHelper.SetClipboardText(step.Instruction);
                            ctx.ToastTimer = 2.0f;
                        }

                        ImGui.SameLine();

                        if (ImGui.Button(clearText, new Vector2(clearW, 0)))
                        {
                            ctx.CompletedTasks.RemoveWhere(k => k.StartsWith($"{mapTaskId}_") || k.StartsWith($"gen_{mapTaskId}_"));
                            ctx.TriggeredDjAlerts.Clear();
                            ctx.TriggeredOcrSounds.Clear();
                            StrategyService.SaveCompletedTasks(ctx.CompletedTasks);
                        }

                        if (ctx.ToastTimer > 0)
                        {
                            ctx.ToastTimer -= ImGui.GetIO().DeltaTime;
                            ImGui.SameLine();
                            InstructionRenderer.SafeTextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Loc.Tr("CopiedToast"));
                        }
                    }

                    float textH = ctx.Settings.InstructionBoxHeight;
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
                            string taskKey = $"{mapTaskId}_{ctx.CurrentStepIndex}_{lineIdx}";

                            InstructionRenderer.RenderInstructionLine(ctx, rawLine, activeWave, taskKey);
                        }
                    }

                    if (ctx.Settings.SeparateImageWindow && currentMap.ImagePaths.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.TextDisabled($"{Loc.Tr("SeparateImageNotice")} ({currentMap.ImagePaths.Count})");
                    }

                    ImGui.EndChild();

                    ctx.Settings.InstructionBoxHeight = ImageViewer.DrawHeightResizeHandle(ctx, ctx.Settings.InstructionBoxHeight, 60.0f, 500.0f, "InstructionTextHandle");

                    if (!ctx.Settings.SeparateImageWindow && currentMap.ImagePaths.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.Separator();

                        ImageViewer.RenderImageSelector(ctx, currentMap);

                        string? currentImg = ImageViewer.GetActiveImagePath(ctx, currentMap);
                        if (!string.IsNullOrEmpty(currentImg) && File.Exists(currentImg))
                        {
                            ImageViewer.RenderImageCanvas(overlay, ctx, currentImg, ctx.Settings.EmbeddedImageBoxHeight, $"EmbeddedCanvas_{ctx.CurrentImageIndex}", enableResizeGrip: true);
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
        }
    }
}
