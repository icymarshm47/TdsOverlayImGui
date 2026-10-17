using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string, float> _cardHeightCache = new();

        private static string? _lastGeneralInfoRef;
        private static string[] _cachedGeneralInfoLines = Array.Empty<string>();

        private static string? _lastInstructionRef;
        private static string[] _cachedInstructionLines = Array.Empty<string>();

        private static string[] GetGeneralInfoLines(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            if (ReferenceEquals(text, _lastGeneralInfoRef)) return _cachedGeneralInfoLines;
            _lastGeneralInfoRef = text;
            _cachedGeneralInfoLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            return _cachedGeneralInfoLines;
        }

        private static string[] GetInstructionLines(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            if (ReferenceEquals(text, _lastInstructionRef)) return _cachedInstructionLines;
            _lastInstructionRef = text;
            _cachedInstructionLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            return _cachedInstructionLines;
        }

        public static void Render(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            if (ctx.Strategies.Count == 0 || ctx.CurrentMap == null)
            {
                RenderEmptyState(ctx);
                return;
            }

            var currentMap = ctx.CurrentMap;
            currentMap.EnsureDefaultPlayer();
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
            if (currentMap.CurrentPlayer.Steps.Count > 0)
            {
                int stepEndMax = 0;
                foreach (var step in currentMap.CurrentPlayer.Steps)
                {
                    if (step.EndWave > stepEndMax) stepEndMax = step.EndWave;
                }
                if (stepEndMax > 0) maxWave = Math.Max(maxWave, stepEndMax);
            }

            RenderSectionStrategy(overlay, ctx, currentMap);
            RenderSectionStopwatch(ctx);
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
            string previewText = $"{currentMap.DisplayName}  ->  {currentMap.CurrentPlayer.PlayerName}";
            if (ImGui.BeginCombo("##ActiveStratCombo", previewText))
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##StratSearchFilter", Loc.Tr("SearchPlaceholder"), ref ctx.StrategySearchFilter, 128);
                ImGui.Separator();

                for (int i = 0; i < ctx.Strategies.Count; i++)
                {
                    var strat = ctx.Strategies[i];
                    strat.EnsureDefaultPlayer();

                    string stratName = strat.DisplayName;
                    if (!string.IsNullOrWhiteSpace(ctx.StrategySearchFilter) &&
                        !stratName.Contains(ctx.StrategySearchFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), stratName);

                    for (int pIdx = 0; pIdx < strat.Players.Count; pIdx++)
                    {
                        var player = strat.Players[pIdx];
                        bool isSelected = (ctx.SelectedMapIndex == i && strat.SelectedPlayerIndex == pIdx);
                        if (ImGui.Selectable($"  - {player.PlayerName}##strat_{i}_p_{pIdx}", isSelected))
                        {
                            if (ctx.SelectedMapIndex != i || strat.SelectedPlayerIndex != pIdx)
                            {
                                ImageViewer.ClearLoadedTextures(overlay);
                            }
                            ctx.SelectedMapIndex = i;
                            strat.SelectedPlayerIndex = pIdx;
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

                    if (i < ctx.Strategies.Count - 1)
                    {
                        ImGui.Separator();
                    }
                }

                ImGui.EndCombo();
            }

            if (currentMap.Players.Count > 1)
            {
                ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 3.0f));
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), $"{Loc.Tr("SelectPlayer")}:");
                ImGui.SameLine();
                for (int pIdx = 0; pIdx < currentMap.Players.Count; pIdx++)
                {
                    var p = currentMap.Players[pIdx];
                    bool isCurrent = (currentMap.SelectedPlayerIndex == pIdx);
                    if (isCurrent)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f));
                    }
                    if (ImGui.SmallButton($"{p.PlayerName}##quickPlayer_{pIdx}"))
                    {
                        if (currentMap.SelectedPlayerIndex != pIdx)
                        {
                            ImageViewer.ClearLoadedTextures(overlay);
                            currentMap.SelectedPlayerIndex = pIdx;
                            ctx.CurrentStepIndex = 0;
                            ctx.CurrentImageIndex = 0;
                            ctx.ResetImageTransform();
                        }
                    }
                    if (isCurrent)
                    {
                        ImGui.PopStyleColor();
                    }
                    if (pIdx < currentMap.Players.Count - 1)
                    {
                        ImGui.SameLine();
                    }
                }
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

        private static void RenderSectionStopwatch(OverlayContext ctx)
        {
            bool isOpen = ctx.SectionStopwatchOpen;
            DrawSectionHeader("Stopwatch", Loc.Tr("StopwatchHeader"), ref isOpen);
            ctx.SectionStopwatchOpen = isOpen;

            if (!isOpen) return;

            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float contentX = winPos.X + 10.0f;
            float availW = winW - 20.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 4.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            TimeSpan elapsed = ctx.Stopwatch.Elapsed;
            string timeStr = elapsed.TotalHours >= 1
                ? string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D2}", (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds / 10)
                : string.Format("{0:D2}:{1:D2}.{2:D2}", elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds / 10);

            float cardH = 36.0f;
            var drawList = ImGui.GetWindowDrawList();
            float alpha = ImGui.GetStyle().Alpha;

            Vector2 cardMin = new Vector2(contentX, startY);
            Vector2 cardMax = new Vector2(contentX + availW, startY + cardH);

            uint bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(17f / 255f, 18f / 255f, 20f / 255f, alpha));
            uint borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(43f / 255f, 45f / 255f, 49f / 255f, alpha));
            drawList.AddRectFilled(cardMin, cardMax, bgCol, 4.0f);
            drawList.AddRect(cardMin, cardMax, borderCol, 4.0f);

            bool isRunning = ctx.Stopwatch.IsRunning;
            uint statusDotCol = isRunning
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(35f / 255f, 165f / 255f, 90f / 255f, alpha))
                : (elapsed.TotalMilliseconds > 0
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(240f / 255f, 178f / 255f, 50f / 255f, alpha))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 0.6f * alpha)));

            Vector2 dotPos = new Vector2(contentX + 16.0f, startY + cardH * 0.5f);
            drawList.AddCircleFilled(dotPos, 4.5f, statusDotCol);

            Vector2 textSize = ImGui.CalcTextSize(timeStr);
            Vector2 textPos = new Vector2(contentX + (availW - textSize.X) * 0.5f, startY + (cardH - textSize.Y) * 0.5f);

            uint textCol = isRunning
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha))
                : (elapsed.TotalMilliseconds > 0
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(240f / 255f, 240f / 255f, 240f / 255f, 0.95f * alpha))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 0.85f * alpha)));

            drawList.AddText(textPos, textCol, timeStr);

            float btnStartY = startY + cardH + 6.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, btnStartY));

            float spacing = 6.0f;
            float btnW = (availW - spacing * 2.0f) / 3.0f;
            float btnH = 26.0f;

            if (!isRunning)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(35f / 255f, 165f / 255f, 90f / 255f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(45f / 255f, 185f / 255f, 105f / 255f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(30f / 255f, 145f / 255f, 80f / 255f, 1.0f));

                string startLabel = elapsed.TotalMilliseconds > 0 ? Loc.Tr("StopwatchResume") : Loc.Tr("StopwatchStart");
                if (ImGui.Button($"{startLabel}##SwStart", new Vector2(btnW, btnH)))
                {
                    ctx.Stopwatch.Start();
                }
                ImGui.PopStyleColor(3);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(240f / 255f, 178f / 255f, 50f / 255f, 0.9f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(254f / 255f, 195f / 255f, 65f / 255f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(220f / 255f, 160f / 255f, 40f / 255f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f));

                if (ImGui.Button($"{Loc.Tr("StopwatchPause")}##SwPause", new Vector2(btnW, btnH)))
                {
                    ctx.Stopwatch.Stop();
                }
                ImGui.PopStyleColor(4);
            }

            ImGui.SameLine(0, spacing);

            bool canReset = elapsed.TotalMilliseconds > 0 || ctx.StopwatchLaps.Count > 0;
            if (!canReset)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
            }

            if (ImGui.Button($"{Loc.Tr("StopwatchReset")}##SwReset", new Vector2(btnW, btnH)))
            {
                if (canReset)
                {
                    ctx.Stopwatch.Reset();
                    ctx.StopwatchLaps.Clear();
                }
            }

            if (!canReset)
            {
                ImGui.PopStyleVar();
            }

            ImGui.SameLine(0, spacing);

            bool canLap = isRunning || elapsed.TotalMilliseconds > 0;
            if (!canLap)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
            }

            if (ImGui.Button($"{Loc.Tr("StopwatchLap")}##SwLap", new Vector2(btnW, btnH)))
            {
                if (canLap && ctx.StopwatchLaps.Count < 20)
                {
                    ctx.StopwatchLaps.Insert(0, elapsed);
                }
            }

            if (!canLap)
            {
                ImGui.PopStyleVar();
            }

            if (ctx.StopwatchLaps.Count > 0)
            {
                float lapsStartY = btnStartY + btnH + 6.0f;
                ImGui.SetCursorScreenPos(new Vector2(contentX, lapsStartY));
                float lapAreaH = Math.Min(90.0f, ctx.StopwatchLaps.Count * 22.0f + 10.0f);
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8.0f, 6.0f));

                if (ImGui.BeginChild("##StopwatchLapsArea", new Vector2(availW, lapAreaH), true, ImGuiWindowFlags.None))
                {
                    for (int i = 0; i < ctx.StopwatchLaps.Count; i++)
                    {
                        var lapTime = ctx.StopwatchLaps[i];
                        int lapNum = ctx.StopwatchLaps.Count - i;
                        string lapStr = lapTime.TotalHours >= 1
                            ? string.Format("#{0}  {1:D2}:{2:D2}:{3:D2}.{4:D2}", lapNum, (int)lapTime.TotalHours, lapTime.Minutes, lapTime.Seconds, lapTime.Milliseconds / 10)
                            : string.Format("#{0}  {1:D2}:{2:D2}.{3:D2}", lapNum, lapTime.Minutes, lapTime.Seconds, lapTime.Milliseconds / 10);

                        InstructionRenderer.SafeTextColored(new Vector4(180f / 255f, 185f / 255f, 195f / 255f, 0.9f), lapStr);
                    }
                    ImGui.EndChild();
                }

                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor(2);
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
            float alpha = ImGui.GetStyle().Alpha;
            bool isPrevHovered = ImGui.IsItemHovered();
            uint prevBg = ImGui.ColorConvertFloat4ToU32(isPrevHovered
                ? new Vector4(71f / 255f, 74f / 255f, 81f / 255f, alpha)
                : new Vector4(56f / 255f, 58f / 255f, 64f / 255f, alpha));
            drawList.AddRectFilled(rowStart, new Vector2(rowStart.X + btnSize, rowStart.Y + btnSize), prevBg, 4.0f);
            drawList.AddTriangleFilled(
                new Vector2(rowStart.X + 9.0f, rowStart.Y + btnSize * 0.5f),
                new Vector2(rowStart.X + 18.0f, rowStart.Y + btnSize * 0.5f - 5.0f),
                new Vector2(rowStart.X + 18.0f, rowStart.Y + btnSize * 0.5f + 5.0f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha)));

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
                ? new Vector4(71f / 255f, 74f / 255f, 81f / 255f, alpha)
                : new Vector4(56f / 255f, 58f / 255f, 64f / 255f, alpha));
            drawList.AddRectFilled(new Vector2(nextBtnX, rowStart.Y), new Vector2(nextBtnX + btnSize, rowStart.Y + btnSize), nextBg, 4.0f);
            drawList.AddTriangleFilled(
                new Vector2(nextBtnX + btnSize - 9.0f, rowStart.Y + btnSize * 0.5f),
                new Vector2(nextBtnX + btnSize - 18.0f, rowStart.Y + btnSize * 0.5f - 5.0f),
                new Vector2(nextBtnX + btnSize - 18.0f, rowStart.Y + btnSize * 0.5f + 5.0f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha)));

            var activePlayer = currentMap.CurrentPlayer;
            if (activePlayer.Steps.Count > 0)
            {
                string stepLabel = $"{Loc.Tr("Step")} {ctx.CurrentStepIndex + 1}";
                string stepTotal = $"/ {activePlayer.Steps.Count}";
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

            var activePlayer = currentMap.CurrentPlayer;
            string[] lines = GetGeneralInfoLines(activePlayer.GeneralInfo);

            string cacheKey = $"gen_{mapTaskId}_{currentMap.SelectedPlayerIndex}_{activePlayer.GeneralInfo?.GetHashCode() ?? 0}";
            if (!_cardHeightCache.TryGetValue(cacheKey, out float cardH))
            {
                cardH = EstimateCardHeight(lines, availW, false);
            }

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 8.0f));

            ImGui.BeginChild("SectionGeneralInfoBody", new Vector2(availW, cardH), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            if (lines.Length == 0)
            {
                float wrapX = Math.Max(ImGui.GetCursorPosX() + 40.0f, ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X);
                ImGui.PushTextWrapPos(wrapX);
                InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 0.7f), "[No general notes for this strategy]");
                ImGui.PopTextWrapPos();
            }
            else
            {
                for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                {
                    string rawLine = lines[lineIdx];
                    string taskKey = $"gen_{mapTaskId}_{currentMap.SelectedPlayerIndex}_{lineIdx}";
                    InstructionRenderer.RenderInstructionLine(ctx, rawLine, activeWave, taskKey);
                }
            }

            float measuredH = Math.Max(32.0f, ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y + 2.0f);
            if (Math.Abs(measuredH - cardH) > 1.0f)
            {
                if (_cardHeightCache.Count > 500) _cardHeightCache.Clear();
                _cardHeightCache[cacheKey] = measuredH;
            }

            ImGui.EndChild();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);

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
            float contentX = winPos.X + 10.0f;
            float availW = winW - 20.0f;
            float startY = ImGui.GetCursorScreenPos().Y + 3.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, startY));

            var activePlayer = currentMap.CurrentPlayer;
            if (activePlayer.Steps.Count == 0)
            {
                ImGui.Text(Loc.Tr("NoStepsNotice"));
                if (ImGui.Button(Loc.Tr("AddDefaultStepBtn"), new Vector2(availW, 0)))
                {
                    activePlayer.Steps.Add(new StrategyStep { StartWave = 1, EndWave = 30, Instruction = "..." });
                    currentMap.Steps = new List<StrategyStep>(activePlayer.Steps);
                    StrategyService.SaveStrategy(currentMap);
                }
                return;
            }

            ctx.CurrentStepIndex = Math.Clamp(ctx.CurrentStepIndex, 0, activePlayer.Steps.Count - 1);
            var step = activePlayer.Steps[ctx.CurrentStepIndex];

            string copyText = Loc.Tr("CopyInstruction");
            string clearText = Loc.Tr("ClearChecks");

            float copyW = ImGui.CalcTextSize(copyText).X + ImGui.GetStyle().FramePadding.X * 2.0f + 6.0f;
            float clearW = ImGui.CalcTextSize(clearText).X + ImGui.GetStyle().FramePadding.X * 2.0f + 6.0f;

            if (ImGui.Button(copyText, new Vector2(copyW, 0)))
            {
                ImagePickerHelper.SetClipboardText(step.Instruction);
                ctx.ToastTimer = 2.0f;
            }

            if (availW >= copyW + 6.0f + clearW)
            {
                ImGui.SameLine(0, 6.0f);
            }
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

            float cardStartY = ImGui.GetCursorScreenPos().Y + 5.0f;
            ImGui.SetCursorScreenPos(new Vector2(contentX, cardStartY));

            string[] lines = GetInstructionLines(step.Instruction);

            string cacheKey = $"step_{mapTaskId}_{currentMap.SelectedPlayerIndex}_{ctx.CurrentStepIndex}_{step.Instruction?.GetHashCode() ?? 0}";
            if (!_cardHeightCache.TryGetValue(cacheKey, out float cardH))
            {
                cardH = EstimateCardHeight(lines, availW, activePlayer.ImagePaths.Count > 0);
            }

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(17f / 255f, 18f / 255f, 20f / 255f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(43f / 255f, 45f / 255f, 49f / 255f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 8.0f));

            ImGui.BeginChild("WaveInstructionsScroll", new Vector2(availW, cardH), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            if (lines.Length == 0)
            {
                float wrapX = Math.Max(ImGui.GetCursorPosX() + 40.0f, ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X);
                ImGui.PushTextWrapPos(wrapX);
                ImGui.TextUnformatted("[...]");
                ImGui.PopTextWrapPos();
            }
            else
            {
                for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                {
                    string rawLine = lines[lineIdx];
                    string taskKey = $"{mapTaskId}_{currentMap.SelectedPlayerIndex}_{ctx.CurrentStepIndex}_{lineIdx}";
                    InstructionRenderer.RenderInstructionLine(ctx, rawLine, activeWave, taskKey);
                }
            }

            if (activePlayer.ImagePaths.Count > 0)
            {
                ImGui.Spacing();
                if (!ctx.IsSeparateImageWindowOpen)
                {
                    if (ImGui.SmallButton($"{Loc.Tr("SeparateImageNotice")} ({activePlayer.ImagePaths.Count})"))
                    {
                        ctx.IsSeparateImageWindowOpen = true;
                    }
                }
                else
                {
                    InstructionRenderer.SafeTextColored(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f), $"{Loc.Tr("SeparateImageNotice")} ({activePlayer.ImagePaths.Count})");
                }
            }

            float measuredH = Math.Max(36.0f, ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y + 2.0f);
            if (Math.Abs(measuredH - cardH) > 1.0f)
            {
                if (_cardHeightCache.Count > 500) _cardHeightCache.Clear();
                _cardHeightCache[cacheKey] = measuredH;
            }

            ImGui.EndChild();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);

            ImGui.SetCursorScreenPos(new Vector2(contentX, ImGui.GetCursorScreenPos().Y + 6.0f));
        }

        private static float EstimateCardHeight(string[] lines, float availW, bool hasImageNotice)
        {
            float padY = 8.0f * 2.0f;
            float lineH = ImGui.GetTextLineHeightWithSpacing();
            float innerW = Math.Max(80.0f, availW - 20.0f);
            float totalH = padY;

            if (lines.Length == 0)
            {
                totalH += lineH;
            }
            else
            {
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        totalH += lineH * 0.5f;
                        continue;
                    }

                    float effectiveW = innerW;
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("- [") || trimmed.StartsWith("* ["))
                    {
                        effectiveW = Math.Max(40.0f, innerW - 24.0f);
                    }

                    string cleanLine = line;
                    if (cleanLine.IndexOf("ocr", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        cleanLine = AudioService.AlertTagRegex.Replace(cleanLine, m =>
                        {
                            AudioService.ParseAlertMatch(m, out int startW, out int endW, out string alertType, out string innerText);
                            return !string.IsNullOrWhiteSpace(innerText)
                                ? innerText
                                : InstructionRenderer.GetFallbackBadge(alertType, startW, endW);
                        });
                    }

                    float textW = ImGui.CalcTextSize(cleanLine).X;
                    int wrapped = Math.Max(1, (int)Math.Ceiling(textW / effectiveW));
                    totalH += wrapped * lineH;
                }
            }

            if (hasImageNotice)
            {
                totalH += lineH + 10.0f;
            }

            return Math.Max(36.0f, totalH);
        }

        public static bool DrawSectionHeader(string id, string title, ref bool isOpen)
        {
            float headerH = 26.0f;
            float winW = ImGui.GetWindowWidth();
            Vector2 winPos = ImGui.GetWindowPos();
            float cursorY = ImGui.GetCursorScreenPos().Y;
            var drawList = ImGui.GetWindowDrawList();
            float alpha = ImGui.GetStyle().Alpha;

            ImGui.SetCursorScreenPos(new Vector2(winPos.X, cursorY));
            ImGui.InvisibleButton($"##SecHeaderBtn_{id}", new Vector2(winW, headerH));
            bool isHovered = ImGui.IsItemHovered();
            if (ImGui.IsItemClicked())
            {
                isOpen = !isOpen;
            }

            uint bgCol = ImGui.ColorConvertFloat4ToU32(isHovered
                ? new Vector4(43f / 255f, 45f / 255f, 49f / 255f, alpha)
                : new Vector4(35f / 255f, 36f / 255f, 40f / 255f, alpha));

            drawList.AddRectFilled(new Vector2(winPos.X, cursorY), new Vector2(winPos.X + winW, cursorY + headerH), bgCol, 0.0f);

            float centerY = cursorY + headerH * 0.5f;
            float arrowLeftX = winPos.X + 14.0f;
            uint arrowCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.95f * alpha));

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
            uint textCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha));
            drawList.AddText(new Vector2(textX, textY), textCol, title.ToUpperInvariant());

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
