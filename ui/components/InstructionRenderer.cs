using System;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using TdsOverlayImGui.Services;

namespace TdsOverlayImGui.UI.Components
{
    public static class InstructionRenderer
    {
        private static readonly Regex FragmentTokenRegex = new Regex(@"\S+|\s+", RegexOptions.Compiled);

        public static bool IsCheckbox(string line, out bool isCheckedInText, out string cleanText)
        {
            isCheckedInText = false;
            cleanText = line;

            string trimmed = line.TrimStart();

            if (trimmed.StartsWith("- [ ] ") || trimmed.StartsWith("* [ ] "))
            {
                isCheckedInText = false;
                cleanText = trimmed.Substring(6);
                return true;
            }

            if (trimmed.StartsWith("- [x] ") || trimmed.StartsWith("- [X] ") ||
                trimmed.StartsWith("* [x] ") || trimmed.StartsWith("* [X] "))
            {
                isCheckedInText = true;
                cleanText = trimmed.Substring(6);
                return true;
            }

            return false;
        }

        public static float GetIndentPixels(string line)
        {
            float pixels = 0;
            foreach (char c in line)
            {
                if (c == '\t') pixels += 20.0f;
                else if (c == ' ') pixels += 5.0f;
                else break;
            }
            return pixels;
        }

        public static void RenderInstructionLine(OverlayContext ctx, string rawLine, int activeWave, string taskKey)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                ImGui.Spacing();
                return;
            }

            float indentPixels = GetIndentPixels(rawLine);
            if (indentPixels > 0)
            {
                ImGui.Indent(indentPixels);
            }

            float lineStartX = ImGui.GetCursorPosX();
            bool hasCheckbox = IsCheckbox(rawLine, out bool defaultCheckedInText, out string lineAfterCheck);

            if (hasCheckbox)
            {
                bool isDone = defaultCheckedInText
                    ? !ctx.CompletedTasks.Contains($"uncheck_{taskKey}")
                    : ctx.CompletedTasks.Contains(taskKey);

                ImGui.PushID($"Task_{taskKey}");

                Vector2 cPos = ImGui.GetCursorScreenPos();
                float boxSize = 16.0f;
                float lineH = ImGui.GetTextLineHeight();
                float boxOffsetY = (lineH - boxSize) * 0.5f;

                if (ImGui.InvisibleButton("##CustomTaskCheck", new Vector2(boxSize, lineH)))
                {
                    isDone = !isDone;
                    if (isDone)
                    {
                        ctx.CompletedTasks.Add(taskKey);
                        ctx.CompletedTasks.Remove($"uncheck_{taskKey}");
                    }
                    else
                    {
                        ctx.CompletedTasks.Remove(taskKey);
                        if (defaultCheckedInText)
                        {
                            ctx.CompletedTasks.Add($"uncheck_{taskKey}");
                        }
                    }

                    StrategyService.SaveCompletedTasks(ctx.CompletedTasks);
                }

                bool isBoxHovered = ImGui.IsItemHovered();
                var drawList = ImGui.GetWindowDrawList();
                Vector2 bMin = new Vector2(cPos.X, cPos.Y + boxOffsetY);
                Vector2 bMax = new Vector2(cPos.X + boxSize, cPos.Y + boxOffsetY + boxSize);

                if (isDone)
                {
                    uint checkBg = ImGui.ColorConvertFloat4ToU32(new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f));
                    drawList.AddRectFilled(bMin, bMax, checkBg, 3.0f);

                    Vector2 p1 = new Vector2(bMin.X + 3.5f, bMin.Y + 8.0f);
                    Vector2 p2 = new Vector2(bMin.X + 6.5f, bMin.Y + 11.5f);
                    Vector2 p3 = new Vector2(bMin.X + 12.5f, bMin.Y + 4.5f);
                    drawList.AddLine(p1, p2, 0xFFFFFFFF, 2.0f);
                    drawList.AddLine(p2, p3, 0xFFFFFFFF, 2.0f);
                }
                else
                {
                    uint borderCol = ImGui.ColorConvertFloat4ToU32(isBoxHovered
                        ? new Vector4(120f / 255f, 124f / 255f, 133f / 255f, 1.0f)
                        : new Vector4(78f / 255f, 80f / 255f, 88f / 255f, 1.0f));
                    uint bgBoxCol = ImGui.ColorConvertFloat4ToU32(new Vector4(30f / 255f, 31f / 255f, 35f / 255f, 1.0f));
                    drawList.AddRectFilled(bMin, bMax, bgBoxCol, 3.0f);
                    drawList.AddRect(bMin, bMax, borderCol, 3.0f, ImDrawFlags.None, 1.5f);
                }

                ImGui.SameLine(0, 8.0f);

                ImGui.BeginGroup();
                Vector2 textStartPos = ImGui.GetCursorScreenPos();
                RenderHighlightedLine(lineAfterCheck, activeWave, isDone);

                if (isDone)
                {
                    string cleanLine = lineAfterCheck;
                    if (cleanLine.IndexOf("ocr", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        cleanLine = AudioService.AlertTagRegex.Replace(cleanLine, m =>
                        {
                            AudioService.ParseAlertMatch(m, out int startW, out int endW, out string alertType, out string innerText);
                            return !string.IsNullOrWhiteSpace(innerText)
                                ? innerText
                                : GetFallbackBadge(alertType, startW, endW);
                        });
                    }
                    float maxAllowedW = Math.Max(10.0f, ImGui.GetWindowPos().X + ImGui.GetWindowWidth() - textStartPos.X - ImGui.GetStyle().WindowPadding.X);
                    float textW = Math.Min(ImGui.CalcTextSize(cleanLine).X, maxAllowedW);
                    float strikeY = textStartPos.Y + lineH * 0.5f;
                    uint strikeCol = ImGui.ColorConvertFloat4ToU32(new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 0.95f));
                    drawList.AddLine(new Vector2(textStartPos.X, strikeY), new Vector2(textStartPos.X + textW, strikeY), strikeCol, 1.5f);
                }
                ImGui.EndGroup();

                ImGui.PopID();

                ImGui.SetCursorPosX(lineStartX);
            }
            else
            {
                RenderHighlightedLine(lineAfterCheck, activeWave, false);
                ImGui.SetCursorPosX(lineStartX);
            }

            if (indentPixels > 0)
            {
                ImGui.Unindent(indentPixels);
            }
        }

        public static void RenderHighlightedLine(string line, int activeWave, bool isDone)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            Vector4 normalColor = new Vector4(220f / 255f, 222f / 255f, 225f / 255f, 1.0f);
            Vector4 completedColor = new Vector4(148f / 255f, 155f / 255f, 164f / 255f, 1.0f);

            if (line.IndexOf("ocr", StringComparison.OrdinalIgnoreCase) < 0)
            {
                float wrapX = Math.Max(ImGui.GetCursorPosX() + 40.0f, ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X);
                ImGui.PushTextWrapPos(wrapX);
                SafeTextColored(isDone ? completedColor : normalColor, line);
                ImGui.PopTextWrapPos();
                return;
            }

            var matches = AudioService.AlertTagRegex.Matches(line);
            if (matches.Count == 0)
            {
                float wrapX = Math.Max(ImGui.GetCursorPosX() + 40.0f, ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X);
                ImGui.PushTextWrapPos(wrapX);
                SafeTextColored(isDone ? completedColor : normalColor, line);
                ImGui.PopTextWrapPos();
                return;
            }

            float startLocalX = ImGui.GetCursorPosX();
            float maxLocalX = Math.Max(startLocalX + 40.0f, ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X);
            int lastIndex = 0;
            bool isFirstOnLine = true;
            float currentLocalX = startLocalX;
            float pendingSpaceW = 0.0f;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    string beforeText = line.Substring(lastIndex, match.Index - lastIndex);
                    RenderFragment(beforeText, isDone ? completedColor : normalColor, startLocalX, maxLocalX, ref isFirstOnLine, ref currentLocalX, ref pendingSpaceW);
                }

                AudioService.ParseAlertMatch(match, out int startW, out int endW, out string alertType, out string innerText);
                bool isWaveActive = (activeWave >= startW && activeWave <= endW && startW > 0);

                Vector4 alertColor = isDone ? completedColor : GetAlertColor(alertType, isWaveActive);

                if (isWaveActive && !isDone)
                {
                    RenderFragment("[WAVE!]", new Vector4(0.3f, 1.0f, 0.4f, 1.0f), startLocalX, maxLocalX, ref isFirstOnLine, ref currentLocalX, ref pendingSpaceW);
                }

                string displayText = !string.IsNullOrWhiteSpace(innerText)
                    ? innerText
                    : GetFallbackBadge(alertType, startW, endW);

                RenderFragment(displayText, alertColor, startLocalX, maxLocalX, ref isFirstOnLine, ref currentLocalX, ref pendingSpaceW);

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < line.Length)
            {
                string afterText = line.Substring(lastIndex);
                RenderFragment(afterText, isDone ? completedColor : normalColor, startLocalX, maxLocalX, ref isFirstOnLine, ref currentLocalX, ref pendingSpaceW);
            }
        }

        private static void RenderFragment(string text, Vector4 color, float startLocalX, float maxLocalX, ref bool isFirstOnLine, ref float currentLocalX, ref float pendingSpaceW)
        {
            if (string.IsNullOrEmpty(text)) return;

            var matches = FragmentTokenRegex.Matches(text);
            foreach (Match match in matches)
            {
                string token = match.Value;
                if (string.IsNullOrEmpty(token)) continue;

                if (char.IsWhiteSpace(token[0]))
                {
                    pendingSpaceW += ImGui.CalcTextSize(token).X;
                }
                else
                {
                    float wordW = ImGui.CalcTextSize(token).X;
                    if (isFirstOnLine)
                    {
                        ImGui.SetCursorPosX(startLocalX + pendingSpaceW);
                        SafeTextColored(color, token);
                        currentLocalX = startLocalX + pendingSpaceW + wordW;
                        isFirstOnLine = false;
                    }
                    else
                    {
                        if (currentLocalX + pendingSpaceW + wordW <= maxLocalX)
                        {
                            ImGui.SameLine(0, pendingSpaceW);
                            SafeTextColored(color, token);
                            currentLocalX += pendingSpaceW + wordW;
                        }
                        else
                        {
                            ImGui.SetCursorPosX(startLocalX);
                            SafeTextColored(color, token);
                            currentLocalX = startLocalX + wordW;
                        }
                    }

                    pendingSpaceW = 0.0f;
                }
            }
        }

        private static Vector4 GetAlertColor(string alertType, bool isActive)
        {
            string norm = AudioService.NormalizeAlertType(alertType);
            return norm switch
            {
                "red" => isActive ? new Vector4(1.0f, 0.28f, 0.32f, 1.0f) : new Vector4(0.92f, 0.40f, 0.42f, 0.85f),
                "green" => isActive ? new Vector4(0.25f, 1.0f, 0.45f, 1.0f) : new Vector4(0.35f, 0.85f, 0.45f, 0.85f),
                "purple" => isActive ? new Vector4(0.85f, 0.38f, 1.0f, 1.0f) : new Vector4(0.75f, 0.45f, 0.90f, 0.85f),
                "chain_commander" => isActive ? new Vector4(1.0f, 0.72f, 0.15f, 1.0f) : new Vector4(0.92f, 0.72f, 0.30f, 0.85f),
                "medic" => isActive ? new Vector4(0.20f, 0.85f, 1.0f, 1.0f) : new Vector4(0.35f, 0.75f, 0.90f, 0.85f),
                _ => isActive ? new Vector4(0.40f, 0.75f, 1.0f, 1.0f) : new Vector4(0.50f, 0.70f, 0.90f, 0.85f)
            };
        }

        public static string GetFallbackBadge(string alertType, int startW, int endW)
        {
            string norm = AudioService.NormalizeAlertType(alertType);
            string waveLabel = startW == endW ? $"W:{startW}" : $"W:{startW}-{endW}";

            return norm switch
            {
                "chain_commander" => $"[ ⚡ CHAIN COMMANDER ({waveLabel}) ]",
                "red" => $"[ 🎵 DJ RED TRACK ({waveLabel}) ]",
                "green" => $"[ 🎵 DJ GREEN TRACK ({waveLabel}) ]",
                "purple" => $"[ 🎵 DJ PURPLE TRACK ({waveLabel}) ]",
                "medic" => $"[ ➕ MEDIC CLEANSE ({waveLabel}) ]",
                _ => $"[ ⚠ ALERT ({waveLabel}) ]"
            };
        }

        public static void SafeTextColored(Vector4 color, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
        }

        public static void SafeTextWrapped(Vector4 color, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextWrapped(text);
            ImGui.PopStyleColor();
        }
    }
}
