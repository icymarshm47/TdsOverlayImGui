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
            ImGui.Spacing();

            RenderSectionScreenReader(ctx);
            ImGui.Spacing();

            RenderSectionCurrentWave(ctx, currentMap, activeWave, maxWave);
            ImGui.Spacing();

            RenderSectionGeneralInfo(ctx, currentMap, mapTaskId, activeWave);
            ImGui.Spacing();

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
            float contentX = winPos.X + 12.0f;
            float availW = winW - 24.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 4.0f;
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

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 6.0f));
            float spacing = 8.0f;
            float btnW = (availW - spacing * 2.0f) / 3.0f;

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(56f / 255f, 58f / 255f, 64f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(71f / 255f, 74f / 255f, 81f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));

            if (ImGui.Button(Loc.Tr("CreateBtn"), new Vector2(btnW, 30)))
            {
                ctx.ShowAddMapModal = true;
            }

            ImGui.SameLine(0, spacing);
            if (ImGui.Button(Loc.Tr("EditBtn"), new Vector2(btnW, 30)))
            {
                ctx.BeginEditing(currentMap);
            }

            ImGui.SameLine(0, spacing);
            if (ImGui.Button(Loc.Tr("DeleteBtn"), new Vector2(btnW, 30)))
            {
                ctx.ShowDeleteConfirmModal = true;
            }

            ImGui.PopStyleColor(3);
            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 8.0f));
        }

        private static void RenderSectionScreenReader(OverlayContext ctx)
        {
            bool isOpen = ctx.SectionScreenReaderOpen;
            DrawSectionHeader("ScreenReader", Loc.Tr("ScreenReaderHeader"), ref isOpen);
            ctx.SectionScreenReaderOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 12.0f;
            float availW = winW - 24.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 4.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(71f / 255f, 82f / 255f, 196f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(60f / 255f, 69f / 255f, 165f / 255f, 1.0f));

            if (ImGui.Button(Loc.Tr("SelectScreenAreaBtn"), new Vector2(availW, 34)))
            {
                ctx.IsSelectingOcrRegion = true;
                ctx.OcrSelectionState = 0;
            }

            ImGui.PopStyleColor(3);

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 4.0f));
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

            ImGui.SameLine(0, 8.0f);
            if (ctx.Settings.EnableOcr)
            {
                InstructionRenderer.SafeTextColored(new Vector4(35f / 255f, 165f / 255f, 90f / 255f, 1.0f), Loc.Tr("OcrStatusOn"));
                ImGui.SameLine(0, 6.0f);
                string infoText = string.IsNullOrWhiteSpace(ctx.LastOcrStatus)
                    ? $"[{ctx.Settings.OcrW}x{ctx.Settings.OcrH} px] {Loc.Tr("OcrScanningTag")}"
                    : $"[{ctx.Settings.OcrW}x{ctx.Settings.OcrH} px] {ctx.LastOcrStatus}";
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), infoText);
            }
            else
            {
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), Loc.Tr("OcrStatusOff"));
            }

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 8.0f));
        }

        private static void RenderSectionCurrentWave(OverlayContext ctx, MapStrategy currentMap, int activeWave, int maxWave)
        {
            bool isOpen = ctx.SectionCurrentWaveOpen;
            DrawSectionHeader("CurrentWave", Loc.Tr("CurrentWaveHeader"), ref isOpen);
            ctx.SectionCurrentWaveOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 12.0f;
            float availW = winW - 24.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 4.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(71f / 255f, 82f / 255f, 196f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(60f / 255f, 69f / 255f, 165f / 255f, 1.0f));

            if (ImGui.Button(Loc.Tr("SelectScreenAreaBtn") + "##WaveScreenArea", new Vector2(availW, 34)))
            {
                ctx.IsSelectingOcrRegion = true;
                ctx.OcrSelectionState = 0;
            }

            ImGui.PopStyleColor(3);

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 6.0f));

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(56f / 255f, 58f / 255f, 64f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(71f / 255f, 74f / 255f, 81f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));

            if (ImGui.Button(" ◄ ##PrevWaveStep", new Vector2(34, 32)))
            {
                if (ctx.CurrentStepIndex > 0)
                {
                    ctx.CurrentStepIndex--;
                    ctx.CurrentWaveNumber = currentMap.Steps[ctx.CurrentStepIndex].StartWave;
                    ctx.DetectedWaveNumber = null;
                    AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                    AudioService.CheckAndTriggerAlertsForWave(ctx, ctx.CurrentWaveNumber);
                    ctx.ResetImageTransform();
                }
                else if (ctx.CurrentWaveNumber > 1)
                {
                    ctx.CurrentWaveNumber--;
                    ctx.DetectedWaveNumber = null;
                    AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                    AudioService.CheckAndTriggerAlertsForWave(ctx, ctx.CurrentWaveNumber);
                }
            }

            ImGui.SameLine(0, 10.0f);

            Vector2 textPos = ImGui.GetCursorScreenPos();
            float navY = textPos.Y + (32.0f - ImGui.GetTextLineHeight()) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(textPos.X, navY));

            if (_isManualWaveInputActive)
            {
                ImGui.SetNextItemWidth(70);
                int manualWave = ctx.CurrentWaveNumber;
                if (ImGui.InputInt("##ManualWaveInputInline", ref manualWave, 1, 5, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    ctx.CurrentWaveNumber = Math.Clamp(manualWave, 1, 200);
                    ctx.DetectedWaveNumber = null;
                    AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                    AudioService.CheckAndTriggerAlertsForWave(ctx, ctx.CurrentWaveNumber);
                    _isManualWaveInputActive = false;
                }
                if (ImGui.IsItemDeactivated())
                {
                    _isManualWaveInputActive = false;
                }
            }
            else
            {
                string waveLabel = $"Wave {activeWave}";
                InstructionRenderer.SafeTextColored(new Vector4(1.0f, 1.0f, 1.0f, 1.0f), waveLabel);
                if (ImGui.IsItemClicked())
                {
                    _isManualWaveInputActive = true;
                }

                ImGui.SameLine(0, 6.0f);
                string totalLabel = $"/ {maxWave}";
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), totalLabel);
            }

            ImGui.SameLine(0, 10.0f);
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().X, textPos.Y));

            if (ImGui.Button(" ► ##NextWaveStep", new Vector2(34, 32)))
            {
                if (ctx.CurrentStepIndex < currentMap.Steps.Count - 1)
                {
                    ctx.CurrentStepIndex++;
                    ctx.CurrentWaveNumber = currentMap.Steps[ctx.CurrentStepIndex].StartWave;
                    ctx.DetectedWaveNumber = null;
                    AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                    AudioService.CheckAndTriggerAlertsForWave(ctx, ctx.CurrentWaveNumber);
                    ctx.ResetImageTransform();
                }
                else
                {
                    ctx.CurrentWaveNumber++;
                    ctx.DetectedWaveNumber = null;
                    AudioService.OnWaveChanged(ctx, ctx.CurrentWaveNumber);
                    AudioService.CheckAndTriggerAlertsForWave(ctx, ctx.CurrentWaveNumber);
                }
            }

            ImGui.PopStyleColor(3);

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 6.0f));

            float progress = Math.Clamp((float)activeWave / maxWave, 0.0f, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(35f / 255f, 165f / 255f, 90f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));
            ImGui.ProgressBar(progress, new Vector2(availW, 7), "");
            ImGui.PopStyleColor(2);

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 8.0f));
        }

        private static void RenderSectionGeneralInfo(OverlayContext ctx, MapStrategy currentMap, string mapTaskId, int activeWave)
        {
            bool isOpen = ctx.SectionGeneralInfoOpen;
            DrawSectionHeader("GeneralInfo", Loc.Tr("GeneralInfoHeader"), ref isOpen);
            ctx.SectionGeneralInfoOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 12.0f;
            float availW = winW - 24.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 4.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            float infoH = ctx.Settings.GeneralInfoBoxHeight;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 10.0f));

            ImGui.BeginChild("SectionGeneralInfoBody", new Vector2(availW, infoH), true);

            if (string.IsNullOrWhiteSpace(currentMap.GeneralInfo))
            {
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 0.7f), "[No general notes for this strategy]");
            }
            else
            {
                string[] lines = currentMap.GeneralInfo.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
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

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y));
            ctx.Settings.GeneralInfoBoxHeight = ImageViewer.DrawHeightResizeHandle(ctx, ctx.Settings.GeneralInfoBoxHeight, 40.0f, 400.0f, "GeneralInfoHandle");
            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 6.0f));
        }

        private static void RenderSectionWaveInstructions(TdsImGuiOverlay overlay, OverlayContext ctx, MapStrategy currentMap, string mapTaskId, int activeWave)
        {
            bool isOpen = ctx.SectionWaveInstructionsOpen;
            DrawSectionHeader("WaveInstructions", Loc.Tr("WaveInstructionsHeader"), ref isOpen);
            ctx.SectionWaveInstructionsOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 12.0f;
            float availW = winW - 24.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 4.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            if (currentMap.Steps.Count == 0)
            {
                ImGui.Text(Loc.Tr("NoStepsNotice"));
                if (ImGui.Button(Loc.Tr("AddDefaultStepBtn"), new Vector2(availW, 30)))
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

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(56f / 255f, 58f / 255f, 64f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(71f / 255f, 74f / 255f, 81f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));

            if (ImGui.Button(copyText, new Vector2(copyW, 24)))
            {
                ImagePickerHelper.SetClipboardText(step.Instruction);
                ctx.ToastTimer = 2.0f;
            }

            ImGui.SameLine(0, 6.0f);
            if (ImGui.Button(clearText, new Vector2(clearW, 24)))
            {
                ctx.CompletedTasks.RemoveWhere(k => k.StartsWith($"{mapTaskId}_") || k.StartsWith($"gen_{mapTaskId}_"));
                ctx.TriggeredDjAlerts.Clear();
                ctx.TriggeredOcrSounds.Clear();
                StrategyService.SaveCompletedTasks(ctx.CompletedTasks);
            }

            ImGui.PopStyleColor(3);

            if (ctx.ToastTimer > 0)
            {
                ctx.ToastTimer -= ImGui.GetIO().DeltaTime;
                ImGui.SameLine(0, 8.0f);
                InstructionRenderer.SafeTextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), Loc.Tr("CopiedToast"));
            }

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 4.0f));

            float textH = ctx.Settings.InstructionBoxHeight;
            if (ctx.Settings.SeparateImageWindow || currentMap.ImagePaths.Count == 0)
            {
                float availY = ImGui.GetContentRegionAvail().Y;
                float dynamicH = Math.Max(textH, availY - ImGui.GetFrameHeightWithSpacing() - 28.0f);
                textH = dynamicH;
            }

            ImGui.BeginChild("WaveInstructionsScroll", new Vector2(availW, textH), false);

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

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y));
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

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 8.0f));
        }

        public static bool DrawSectionHeader(string id, string title, ref bool isOpen)
        {
            float headerH = 30.0f;
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
            float arrowLeftX = winPos.X + 16.0f;
            uint arrowCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.95f));

            if (isOpen)
            {
                Vector2 p1 = new Vector2(arrowLeftX, centerY - 3.5f);
                Vector2 p2 = new Vector2(arrowLeftX + 8.0f, centerY - 3.5f);
                Vector2 p3 = new Vector2(arrowLeftX + 4.0f, centerY + 3.5f);
                drawList.AddTriangleFilled(p1, p2, p3, arrowCol);
            }
            else
            {
                Vector2 p1 = new Vector2(arrowLeftX + 1.0f, centerY - 4.5f);
                Vector2 p2 = new Vector2(arrowLeftX + 7.5f, centerY);
                Vector2 p3 = new Vector2(arrowLeftX + 1.0f, centerY + 4.5f);
                drawList.AddTriangleFilled(p1, p2, p3, arrowCol);
            }

            float textX = winPos.X + 32.0f;
            float textY = cursorY + (headerH - ImGui.GetTextLineHeight()) * 0.5f;
            drawList.AddText(new Vector2(textX, textY), 0xFFFFFFFF, title.ToUpperInvariant());

            return isOpen;
        }

        private static void RenderEmptyState(OverlayContext ctx)
        {
            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 12.0f;
            float availW = winW - 24.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 8.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            InstructionRenderer.SafeTextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), Loc.Tr("NoStrategySelected"));
            ImGui.TextWrapped(Loc.Tr("SelectStrategyPrompt"));
            if (ImGui.Button(Loc.Tr("CreateBtn"), new Vector2(availW, 32)))
            {
                ctx.ShowAddMapModal = true;
            }
        }
    }
}
