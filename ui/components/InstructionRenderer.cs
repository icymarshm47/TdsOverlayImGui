using System;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using TdsOverlayImGui.Services;

namespace TdsOverlayImGui.UI.Components
{
    public static class InstructionRenderer
    {
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

            bool hasCheckbox = IsCheckbox(rawLine, out bool defaultCheckedInText, out string lineAfterCheck);

            if (hasCheckbox)
            {
                bool isDone = ctx.CompletedTasks.Contains(taskKey) || defaultCheckedInText;

                ImGui.PushID($"Task_{taskKey}");

                if (ImGui.Checkbox("##TaskCheck", ref isDone))
                {
                    if (isDone)
                        ctx.CompletedTasks.Add(taskKey);
                    else
                        ctx.CompletedTasks.Remove(taskKey);

                    StrategyService.SaveCompletedTasks(ctx.CompletedTasks);
                }

                ImGui.SameLine();

                RenderHighlightedLine(lineAfterCheck, activeWave, isDone);

                ImGui.PopID();
            }
            else
            {
                RenderHighlightedLine(lineAfterCheck, activeWave, false);
            }

            if (indentPixels > 0)
            {
                ImGui.Unindent(indentPixels);
            }
        }

        public static void RenderHighlightedLine(string line, int activeWave, bool isDone)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            Vector4 normalColor = new Vector4(0.92f, 0.92f, 0.96f, 1.0f);
            Vector4 completedColor = new Vector4(0.30f, 1.0f, 0.40f, 0.75f);

            var matches = AudioService.AlertTagRegex.Matches(line);
            if (matches.Count == 0)
            {
                SafeTextWrapped(isDone ? completedColor : normalColor, line);
                return;
            }

            int lastIndex = 0;
            bool isFirstOnLine = true;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    string beforeText = line.Substring(lastIndex, match.Index - lastIndex);
                    RenderFragment(beforeText, isDone ? completedColor : normalColor, ref isFirstOnLine);
                }

                AudioService.ParseAlertMatch(match, out int startW, out int endW, out string alertType, out string innerText);
                bool isWaveActive = (activeWave >= startW && activeWave <= endW && startW > 0);

                Vector4 alertColor = isDone ? completedColor : GetAlertColor(alertType, isWaveActive);

                if (isWaveActive && !isDone)
                {
                    RenderFragment("[WAVE!]", new Vector4(0.3f, 1.0f, 0.4f, 1.0f), ref isFirstOnLine);
                }

                string displayText = !string.IsNullOrWhiteSpace(innerText)
                    ? innerText
                    : GetFallbackBadge(alertType, startW, endW);

                RenderFragment(displayText, alertColor, ref isFirstOnLine);

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < line.Length)
            {
                string afterText = line.Substring(lastIndex);
                RenderFragment(afterText, isDone ? completedColor : normalColor, ref isFirstOnLine);
            }
        }

        private static void RenderFragment(string text, Vector4 color, ref bool isFirstOnLine)
        {
            if (string.IsNullOrEmpty(text)) return;

            string[] words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (string.IsNullOrEmpty(word)) continue;

                float wordW = ImGui.CalcTextSize(word).X;
                float spaceW = ImGui.CalcTextSize(" ").X;
                float avail = ImGui.GetContentRegionAvail().X;

                if (!isFirstOnLine)
                {
                    if (avail < wordW + spaceW && avail < ImGui.GetWindowWidth() * 0.85f)
                    {
                        ImGui.NewLine();
                        isFirstOnLine = true;
                    }
                    else
                    {
                        ImGui.SameLine(0, spaceW);
                    }
                }

                SafeTextColored(color, word);
                isFirstOnLine = false;
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

        private static string GetFallbackBadge(string alertType, int startW, int endW)
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
