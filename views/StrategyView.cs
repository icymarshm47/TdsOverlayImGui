using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using TdsOverlayImGui.Components;

namespace TdsOverlayImGui.Views
{
    public static class StrategyView
    {
        public static void Render(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            if (!ctx.Settings.CompactMode)
            {
                ImGui.BeginChild("MapSelectorCard", new Vector2(0, 92), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

                ImGui.TextDisabled(Loc.Tr("SelectStrategyHeader"));

                string[] comboItems = new string[ctx.Strategies.Count + 1];
                comboItems[0] = Loc.Tr("SelectStrategyCombo");
                for (int i = 0; i < ctx.Strategies.Count; i++)
                {
                    comboItems[i + 1] = ctx.Strategies[i].DisplayName;
                }

                int currentComboIdx = (ctx.SelectedMapIndex >= 0 && ctx.SelectedMapIndex < ctx.Strategies.Count) 
                    ? ctx.SelectedMapIndex + 1 
                    : 0;

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.Combo("##MapSelect", ref currentComboIdx, comboItems, comboItems.Length))
                {
                    ctx.SelectedMapIndex = (currentComboIdx > 0 && currentComboIdx <= ctx.Strategies.Count) ? currentComboIdx - 1 : -1;
                    ctx.CurrentStepIndex = 0;
                    ctx.CurrentImageIndex = 0;
                    ctx.IsEditing = false;
                    ctx.TriggeredDjAlerts.Clear();
                    ctx.TriggeredOcrSounds.Clear();
                    ctx.PreviousWaveNumber = 1;
                    ctx.ResetImageTransform();
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
                        ctx.IsEditing = !ctx.IsEditing;
                        if (ctx.IsEditing)
                        {
                            ctx.EditMapName = map.MapName;
                            ctx.EditDifficulty = map.Difficulty;
                            ctx.EditStrategyName = map.StrategyName;
                            ctx.EditGeneralInfo = map.GeneralInfo;
                        }
                    }

                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Tr("DeleteStrategy"), new Vector2(btnW, 0)))
                    {
                        ctx.ShowDeleteConfirmModal = true;
                    }
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

            var currentMap = ctx.CurrentMap;
            int activeWave = ctx.DetectedWaveNumber ?? ctx.CurrentWaveNumber;

            if (!ctx.Settings.CompactMode)
            {
                string ocrText = Loc.Tr("AutoOcrHeader");
                string ocrBtnText = Loc.Tr("SelectOcrRegionBtn");

                float ocrTextW = ImGui.CalcTextSize(ocrText).X + 35.0f;
                float ocrBtnW = ImGui.CalcTextSize(ocrBtnText).X + ImGui.GetStyle().FramePadding.X * 2 + 10.0f;
                float availOcrW = ImGui.GetContentRegionAvail().X;

                bool ocrFitsSameLine = availOcrW >= ocrTextW + ocrBtnW + ImGui.GetStyle().ItemSpacing.X * 2;
                float cardH = ocrFitsSameLine ? 42.0f : 68.0f;

                ImGui.BeginChild("OcrHeaderCard", new Vector2(0, cardH), true);
                bool enableOcr = ctx.Settings.EnableOcr;
                if (ImGui.Checkbox(ocrText, ref enableOcr))
                {
                    ctx.Settings.EnableOcr = enableOcr;
                    StrategyService.SaveSettings(ctx.Settings);
                }

                if (ocrFitsSameLine)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ocrBtnW - 12.0f);
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

                    ImGui.BeginChild("StepProgressCard", new Vector2(0, 115), true);

                    InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("CurrentWaveHeader"));
                    ImGui.SameLine();

                    InstructionRenderer.SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), $"[ {activeWave} ]");

                    if (ctx.DetectedWaveNumber.HasValue)
                    {
                        ImGui.SameLine();
                        InstructionRenderer.SafeTextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Loc.Tr("AutoOcrTag"));
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90);
                    int waveNum = ctx.CurrentWaveNumber;
                    if (ImGui.InputInt("##ManualWaveInput", ref waveNum, 1, 5))
                    {
                        ctx.CurrentWaveNumber = Math.Clamp(waveNum, 1, 100);
                        ctx.DetectedWaveNumber = null;
                        if (ctx.CurrentWaveNumber == 1)
                        {
                            ctx.TriggeredDjAlerts.Clear();
                            ctx.TriggeredOcrSounds.Clear();
                        }
                        ToastAndAudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                        activeWave = ctx.CurrentWaveNumber;
                        ctx.UpdateStepByWaveNumber(currentMap, ctx.CurrentWaveNumber);
                    }

                    float progress = (float)(ctx.CurrentStepIndex + 1) / currentMap.Steps.Count;
                    ImGui.ProgressBar(progress, new Vector2(-1, 6), "");

                    ImGui.TextDisabled($"{Loc.Tr("Step")} {ctx.CurrentStepIndex + 1} {Loc.Tr("Of")} {currentMap.Steps.Count}");
                    ImGui.SameLine();

                    bool isOcrMatched = activeWave >= step.StartWave && activeWave <= step.EndWave;
                    Vector4 waveColor = isOcrMatched ? new Vector4(0.3f, 1.0f, 0.4f, 1.0f) : new Vector4(0.35f, 0.39f, 0.95f, 1.0f);
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
                        ToastAndAudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                        ctx.ResetImageTransform();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("<", new Vector2(navBtnW, 0)))
                    {
                        if (ctx.CurrentStepIndex > 0)
                        {
                            ctx.CurrentStepIndex--;
                            ctx.CurrentWaveNumber = currentMap.Steps[ctx.CurrentStepIndex].StartWave;
                            ctx.DetectedWaveNumber = null;
                            ToastAndAudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                            ctx.ResetImageTransform();
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(">", new Vector2(navBtnW, 0)))
                    {
                        if (ctx.CurrentStepIndex < currentMap.Steps.Count - 1)
                        {
                            ctx.CurrentStepIndex++;
                            ctx.CurrentWaveNumber = currentMap.Steps[ctx.CurrentStepIndex].StartWave;
                            ctx.DetectedWaveNumber = null;
                            ToastAndAudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                            ctx.ResetImageTransform();
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(">|", new Vector2(navBtnW, 0)))
                    {
                        ctx.CurrentStepIndex = currentMap.Steps.Count - 1;
                        ctx.CurrentWaveNumber = currentMap.Steps[^1].StartWave;
                        ctx.DetectedWaveNumber = null;
                        ToastAndAudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                        ctx.ResetImageTransform();
                    }

                    ImGui.EndChild();
                    ImGui.Spacing();
                }

                if (!string.IsNullOrWhiteSpace(currentMap.GeneralInfo))
                {
                    InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("GeneralInfo"));
                    float infoH = ctx.Settings.GeneralInfoBoxHeight;
                    ImGui.BeginChild("GeneralInfoScroll", new Vector2(0, infoH), true);

                    string[] lines = currentMap.GeneralInfo.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                    {
                        string rawLine = lines[lineIdx];
                        string taskKey = $"gen_{ctx.SelectedMapIndex}_{lineIdx}";
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
                    ImGui.Text(headerText);

                    if (!ctx.Settings.CompactMode)
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
                            ctx.ToastTimer = 2.0f;
                        }

                        ImGui.SameLine();

                        if (ImGui.Button(clearText, new Vector2(clearW, 0)))
                        {
                            ctx.CompletedTasks.Clear();
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
                            string taskKey = $"{ctx.SelectedMapIndex}_{ctx.CurrentStepIndex}_{lineIdx}";

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