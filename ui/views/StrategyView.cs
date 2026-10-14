using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using TdsOverlayImGui.Models;
using TdsOverlayImGui.Services;
using TdsOverlayImGui.UI.Components;

namespace TdsOverlayImGui.UI.Views
{
    public static class StrategyView
    {
        private static bool _isManualWaveInputActive = false;

        public static void Render(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            if (ctx.Strategies.Count == 0 || ctx.CurrentMap == null)
            {
                RenderEmptyState(ctx);
                return;
            }

            var currentMap = ctx.CurrentMap;
            string mapTaskId = currentMap.TaskId;

            if (!ctx.Settings.EnableOcr)
            {
                ctx.DetectedWaveNumber = null;
            }

            int activeWave = (ctx.Settings.EnableOcr && ctx.DetectedWaveNumber.HasValue)
                ? ctx.DetectedWaveNumber.Value
                : ctx.CurrentWaveNumber;

            ctx.UpdateStepByWaveNumber(currentMap, activeWave);

            int maxWave = 40;
            if (currentMap.Steps.Count > 0)
            {
                int stepEndMax = 0;
                foreach (var step in currentMap.Steps)
                {
                    if (step.EndWave > stepEndMax) stepEndMax = step.EndWave;
                }
                if (stepEndMax > 0) maxWave = Math.Max(maxWave, stepEndMax);
            }

            RenderSectionStrategy(overlay, ctx, currentMap);
            RenderSectionScreenReader(ctx);
            RenderSectionCurrentWave(ctx, currentMap, activeWave, maxWave);
            RenderSectionGeneralInfo(ctx, currentMap, mapTaskId, activeWave);
            RenderSectionWaveInstructions(overlay, ctx, currentMap, mapTaskId, activeWave);
        }

        private static void RenderSectionStrategy(TdsImGuiOverlay overlay, OverlayContext ctx, MapStrategy currentMap)
        {
            bool isOpen = ctx.SectionStrategyOpen;
            DrawSectionHeader("StrategySelection", Loc.Tr("StrategySelectionHeader"), ref isOpen);
            ctx.SectionStrategyOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 10.0f;
            float availW = winW - 20.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 3.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), Loc.Tr("ActiveStrategyLabel"));

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 2.0f));
            ImGui.SetNextItemWidth(availW);
            string previewText = currentMap.DisplayName;
            if (ImGui.BeginCombo("##ActiveStratCombo", previewText))
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##StratSearchFilter", Loc.Tr("SearchPlaceholder"), ref ctx.StrategySearchFilter, 128);
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

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 4.0f));
            float spacing = 6.0f;
            float btnW = (availW - spacing * 2.0f) / 3.0f;

            if (ImGui.Button(Loc.Tr("CreateBtn"), new Vector2(btnW, 0)))
            {
                ctx.ShowAddMapModal = true;
            }

            ImGui.SameLine(0, spacing);
            if (ImGui.Button(Loc.Tr("EditBtn"), new Vector2(btnW, 0)))
            {
                ctx.BeginEditing(currentMap);
            }

            ImGui.SameLine(0, spacing);
            if (ImGui.Button(Loc.Tr("DeleteBtn"), new Vector2(btnW, 0)))
            {
                ctx.ShowDeleteConfirmModal = true;
            }

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 5.0f));
        }

        private static void RenderSectionScreenReader(OverlayContext ctx)
        {
            bool isOpen = ctx.SectionScreenReaderOpen;
            DrawSectionHeader("ScreenReader", Loc.Tr("ScreenReaderHeader"), ref isOpen);
            ctx.SectionScreenReaderOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 10.0f;
            float availW = winW - 20.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 4.0f;

            float btnW = Math.Max(150.0f, availW * 0.52f);
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            bool enableOcr = ctx.Settings.EnableOcr;
            if (ImGui.Checkbox(Loc.Tr("AutoOcrHeader"), ref enableOcr))
            {
                ctx.Settings.EnableOcr = enableOcr;
                if (!enableOcr)
                {
                    ctx.DetectedWaveNumber = null;
                    ctx.LastOcrStatus = "";
                }
                else
                {
                    ctx.LastOcrScanTime = DateTime.MinValue;
                    ctx.TriggerOneShotOcr = true;
                }
                StrategyService.SaveSettings(ctx.Settings);
            }

            ImGui.SameLine(0, 4.0f);
            if (ctx.Settings.EnableOcr)
            {
                InstructionRenderer.SafeTextColored(new Vector4(35f / 255f, 165f / 255f, 90f / 255f, 1.0f), Loc.Tr("OcrStatusOn"));
            }
            else
            {
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), Loc.Tr("OcrStatusOff"));
            }

            float btnX = contentX + availW - btnW;
            ImGui.SetCursorScreenPos(new Vector2(btnX, startY));

            if (ImGui.Button(Loc.Tr("SelectScreenAreaBtn"), new Vector2(btnW, 0)))
            {
                ctx.IsSelectingOcrRegion = true;
                ctx.OcrSelectionState = 0;
            }

            if (ctx.Settings.EnableOcr && !string.IsNullOrWhiteSpace(ctx.LastOcrStatus))
            {
                ImGui.SetCursorScreenPos(new Vector2(contentX, startY + ImGui.GetFrameHeight() + 2.0f));
                string infoText = $"[{ctx.Settings.OcrW}x{ctx.Settings.OcrH} px] {ctx.LastOcrStatus}";
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), infoText);
            }

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 5.0f));
        }

        private static void RenderSectionCurrentWave(OverlayContext ctx, MapStrategy currentMap, int activeWave, int maxWave)
        {
            bool isOpen = ctx.SectionCurrentWaveOpen;
            DrawSectionHeader("CurrentWave", Loc.Tr("CurrentWaveHeader"), ref isOpen);
            ctx.SectionCurrentWaveOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 10.0f;
            float availW = winW - 20.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 4.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            float btnSize = 28.0f;
            Vector2 rowStart = new Vector2(contentX, startY);
            var drawList = ImGui.GetWindowDrawList();

            ImGui.SetCursorScreenPos(rowStart);
            if (ImGui.InvisibleButton("##PrevWaveStep", new Vector2(btnSize, btnSize)))
            {
                if (ctx.CurrentWaveNumber > 1)
                {
                    ctx.CurrentWaveNumber--;
                    ctx.DetectedWaveNumber = null;
                    ctx.UpdateStepByWaveNumber(currentMap, ctx.CurrentWaveNumber);
                    AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                    AudioService.CheckAndTriggerAlertsForWave(ctx, ctx.CurrentWaveNumber);
                }
            }
            bool isPrevHovered = ImGui.IsItemHovered();
            uint prevBg = ImGui.ColorConvertFloat4ToU32(isPrevHovered
                ? new Vector4(71f / 255f, 74f / 255f, 81f / 255f, 1.0f)
                : new Vector4(56f / 255f, 58f / 255f, 64f / 255f, 1.0f));
            drawList.AddRectFilled(rowStart, new Vector2(rowStart.X + btnSize, rowStart.Y + btnSize), prevBg, 4.0f);
            drawList.AddTriangleFilled(
                new Vector2(rowStart.X + 9.0f, rowStart.Y + btnSize * 0.5f),
                new Vector2(rowStart.X + 18.0f, rowStart.Y + btnSize * 0.5f - 5.0f),
                new Vector2(rowStart.X + 18.0f, rowStart.Y + btnSize * 0.5f + 5.0f),
                0xFFFFFFFF);

            float textStartX = contentX + btnSize + 10.0f;
            float baselineY = rowStart.Y + (btnSize - ImGui.GetTextLineHeight()) * 0.5f;
            float totalWaveW = 0.0f;

            if (_isManualWaveInputActive)
            {
                ImGui.SetCursorScreenPos(new Vector2(textStartX, rowStart.Y));
                ImGui.SetNextItemWidth(70);
                int manualWave = ctx.CurrentWaveNumber;
                if (ImGui.InputInt("##ManualWaveInputInline", ref manualWave, 1, 5, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    ctx.CurrentWaveNumber = Math.Clamp(manualWave, 1, 200);
                    ctx.DetectedWaveNumber = null;
                    ctx.UpdateStepByWaveNumber(currentMap, ctx.CurrentWaveNumber);
                    AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                    AudioService.CheckAndTriggerAlertsForWave(ctx, ctx.CurrentWaveNumber);
                    _isManualWaveInputActive = false;
                }
                if (ImGui.IsItemDeactivated())
                {
                    _isManualWaveInputActive = false;
                }
                totalWaveW = 70.0f;
            }
            else
            {
                string waveStr = $"Wave {activeWave}";
                string totalStr = $"/ {maxWave}";
                float waveStrW = ImGui.CalcTextSize(waveStr).X;
                float totalStrW = ImGui.CalcTextSize(totalStr).X;

                ImGui.SetCursorScreenPos(new Vector2(textStartX, baselineY));
                InstructionRenderer.SafeTextColored(new Vector4(1f, 1f, 1f, 1f), waveStr);
                if (ImGui.IsItemClicked())
                {
                    _isManualWaveInputActive = true;
                }

                ImGui.SetCursorScreenPos(new Vector2(textStartX + waveStrW + 6.0f, baselineY));
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), totalStr);

                totalWaveW = waveStrW + 6.0f + totalStrW;
            }

            float nextBtnX = textStartX + totalWaveW + 10.0f;
            ImGui.SetCursorScreenPos(new Vector2(nextBtnX, rowStart.Y));
            if (ImGui.InvisibleButton("##NextWaveStep", new Vector2(btnSize, btnSize)))
            {
                if (ctx.CurrentWaveNumber < maxWave)
                {
                    ctx.CurrentWaveNumber++;
                    ctx.DetectedWaveNumber = null;
                    ctx.UpdateStepByWaveNumber(currentMap, ctx.CurrentWaveNumber);
                    AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                    AudioService.CheckAndTriggerAlertsForWave(ctx, ctx.CurrentWaveNumber);
                }
            }
            bool isNextHovered = ImGui.IsItemHovered();
            uint nextBg = ImGui.ColorConvertFloat4ToU32(isNextHovered
                ? new Vector4(71f / 255f, 74f / 255f, 81f / 255f, 1.0f)
                : new Vector4(56f / 255f, 58f / 255f, 64f / 255f, 1.0f));
            drawList.AddRectFilled(new Vector2(nextBtnX, rowStart.Y), new Vector2(nextBtnX + btnSize, rowStart.Y + btnSize), nextBg, 4.0f);
            drawList.AddTriangleFilled(
                new Vector2(nextBtnX + btnSize - 9.0f, rowStart.Y + btnSize * 0.5f),
                new Vector2(nextBtnX + btnSize - 18.0f, rowStart.Y + btnSize * 0.5f - 5.0f),
                new Vector2(nextBtnX + btnSize - 18.0f, rowStart.Y + btnSize * 0.5f + 5.0f),
                0xFFFFFFFF);

            if (currentMap.Steps.Count > 0)
            {
                string stepLabel = $"{Loc.Tr("Step")} {ctx.CurrentStepIndex + 1}";
                string stepTotal = $"/ {currentMap.Steps.Count}";
                float stepLabelW = ImGui.CalcTextSize(stepLabel).X;
                float stepTotalW = ImGui.CalcTextSize(stepTotal).X;
                float stepBlockW = stepLabelW + 6.0f + stepTotalW;
                float stepStartX = Math.Max(nextBtnX + btnSize + 10.0f, contentX + availW - stepBlockW);

                ImGui.SetCursorScreenPos(new Vector2(stepStartX, baselineY));
                InstructionRenderer.SafeTextColored(new Vector4(1f, 1f, 1f, 1f), stepLabel);
                ImGui.SetCursorScreenPos(new Vector2(stepStartX + stepLabelW + 6.0f, baselineY));
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), stepTotal);
            }

            float progressY = rowStart.Y + btnSize + 6.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, progressY));
            float progress = Math.Clamp((float)activeWave / maxWave, 0.0f, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(35f / 255f, 165f / 255f, 90f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));
            ImGui.ProgressBar(progress, new Vector2(availW, 6.0f), "");
            ImGui.PopStyleColor(2);

            ImGui.SetCursorScreenPos(new Vector2(contentX, progressY + 10.0f));
        }

        private static void RenderSectionGeneralInfo(OverlayContext ctx, MapStrategy currentMap, string mapTaskId, int activeWave)
        {
            bool isOpen = ctx.SectionGeneralInfoOpen;
            DrawSectionHeader("GeneralInfo", Loc.Tr("GeneralInfoHeader"), ref isOpen);
            ctx.SectionGeneralInfoOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 10.0f;
            float availW = winW - 20.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 3.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            string[] lines = string.IsNullOrWhiteSpace(currentMap.GeneralInfo)
                ? Array.Empty<string>()
                : currentMap.GeneralInfo.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            float lineH = ImGui.GetTextLineHeightWithSpacing();
            float contentH = Math.Max(1, lines.Length) * lineH + 16.0f;
            float infoH = Math.Clamp(contentH, 36.0f, 220.0f);

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 8.0f));

            ImGui.BeginChild("SectionGeneralInfoBody", new Vector2(availW, infoH), true);

            if (lines.Length == 0)
            {
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 0.7f), "[No general notes for this strategy]");
            }
            else
            {
                for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                {
                    string rawLine = lines[lineIdx];
                    string taskKey = $"gen_{mapTaskId}_{lineIdx}";
                    InstructionRenderer.RenderInstructionLine(ctx, rawLine, activeWave, taskKey);
                }
            }

            ImGui.EndChild();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 5.0f));
        }

        private static void RenderSectionWaveInstructions(TdsImGuiOverlay overlay, OverlayContext ctx, MapStrategy currentMap, string mapTaskId, int activeWave)
        {
            bool isOpen = ctx.SectionWaveInstructionsOpen;
            DrawSectionHeader("WaveInstructions", Loc.Tr("WaveInstructionsHeader"), ref isOpen);
            ctx.SectionWaveInstructionsOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 10.0f;
            float availW = winW - 20.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 3.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            if (currentMap.Steps.Count == 0)
            {
                ImGui.Text(Loc.Tr("NoStepsNotice"));
                if (ImGui.Button(Loc.Tr("AddDefaultStepBtn"), new Vector2(availW, 0)))
                {
                    currentMap.Steps.Add(new StrategyStep { StartWave = 1, EndWave = 30, Instruction = "..." });
                    StrategyService.SaveStrategy(currentMap);
                }
                return;
            }

            ctx.CurrentStepIndex = Math.Clamp(ctx.CurrentStepIndex, 0, currentMap.Steps.Count - 1);
            var step = currentMap.Steps[ctx.CurrentStepIndex];

            string copyText = Loc.Tr("CopyInstruction");
            string clearText = Loc.Tr("ClearChecks");

            float copyW = ImGui.CalcTextSize(copyText).X + ImGui.GetStyle().FramePadding.X * 2.0f + 6.0f;
            float clearW = ImGui.CalcTextSize(clearText).X + ImGui.GetStyle().FramePadding.X * 2.0f + 6.0f;

            if (ImGui.Button(copyText, new Vector2(copyW, 0)))
            {
                ImagePickerHelper.SetClipboardText(step.Instruction);
                ctx.ToastTimer = 2.0f;
            }

            ImGui.SameLine(0, 6.0f);
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
                ImGui.SameLine(0, 8.0f);
                InstructionRenderer.SafeTextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Loc.Tr("CopiedToast"));
            }

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 5.0f));

            string[] lines = string.IsNullOrWhiteSpace(step.Instruction)
                ? Array.Empty<string>()
                : step.Instruction.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            float lineH = ImGui.GetTextLineHeightWithSpacing() + 4.0f;
            float contentH = Math.Max(1, lines.Length) * lineH + 16.0f;
            float availY = ImGui.GetContentRegionAvail().Y - 24.0f;
            float textH = Math.Clamp(contentH, 40.0f, Math.Max(80.0f, availY));

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 8.0f));

            ImGui.BeginChild("WaveInstructionsScroll", new Vector2(availW, textH), true);

            if (lines.Length == 0)
            {
                ImGui.TextWrapped("[...]");
            }
            else
            {
                for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                {
                    string rawLine = lines[lineIdx];
                    string taskKey = $"{mapTaskId}_{ctx.CurrentStepIndex}_{lineIdx}";
                    InstructionRenderer.RenderInstructionLine(ctx, rawLine, activeWave, taskKey);
                }
            }

            if (currentMap.ImagePaths.Count > 0)
            {
                ImGui.Spacing();
                ImGui.TextDisabled($"{Loc.Tr("SeparateImageNotice")} ({currentMap.ImagePaths.Count})");
            }

            ImGui.EndChild();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 5.0f));
        }

        public static bool DrawSectionHeader(string id, string title, ref bool isOpen)
        {
            float headerH = 26.0f;
            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float cursorY = ImGui.GetCursorScreenPos().Y;
            var drawList = ImGui.GetWindowDrawList();

            ImGui.SetCursorScreenPos(new Vector2(winPos.X, cursorY));
            ImGui.InvisibleButton($"##SecHeaderBtn_{id}", new Vector2(winW, headerH));
            bool isHovered = ImGui.IsItemHovered();
            if (ImGui.IsItemClicked())
            {
                isOpen = !isOpen;
            }

            uint bgCol = ImGui.ColorConvertFloat4ToU32(isHovered
                ? new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f)
                : new Vector4(35f / 255f, 36f / 255f, 40f / 255f, 1.0f));

            drawList.AddRectFilled(new Vector2(winPos.X, cursorY), new Vector2(winPos.X + winW, cursorY + headerH), bgCol, 0.0f);

            float centerY = cursorY + headerH * 0.5f;
            float arrowLeftX = winPos.X + 14.0f;
            uint arrowCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.95f));

            if (isOpen)
            {
                Vector2 p1 = new Vector2(arrowLeftX, centerY - 3.0f);
                Vector2 p2 = new Vector2(arrowLeftX + 8.0f, centerY - 3.0f);
                Vector2 p3 = new Vector2(arrowLeftX + 4.0f, centerY + 3.0f);
                drawList.AddTriangleFilled(p1, p2, p3, arrowCol);
            }
            else
            {
                Vector2 p1 = new Vector2(arrowLeftX + 1.0f, centerY - 4.0f);
                Vector2 p2 = new Vector2(arrowLeftX + 7.0f, centerY);
                Vector2 p3 = new Vector2(arrowLeftX + 1.0f, centerY + 4.0f);
                drawList.AddTriangleFilled(p1, p2, p3, arrowCol);
            }

            float textX = winPos.X + 28.0f;
            float textY = cursorY + (headerH - ImGui.GetTextLineHeight()) * 0.5f;
            drawList.AddText(new Vector2(textX, textY), 0xFFFFFFFF, title.ToUpperInvariant());

            return isOpen;
        }

        private static void RenderEmptyState(OverlayContext ctx)
        {
            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 10.0f;
            float availW = winW - 20.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 6.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            InstructionRenderer.SafeTextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), Loc.Tr("NoStrategySelected"));
            ImGui.TextWrapped(Loc.Tr("SelectStrategyPrompt"));
            if (ImGui.Button(Loc.Tr("CreateBtn"), new Vector2(availW, 0)))
            {
                ctx.ShowAddMapModal = true;
            }
        }
    }
}
