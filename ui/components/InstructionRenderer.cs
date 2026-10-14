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

            var matches = AudioService.AlertTagRegex.Matches(lineAfterCheck);
            bool isTagActive = false;
            string displayText = lineAfterCheck;
            Vector4? tagBadgeColor = null;

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string tagContent = match.Groups[1].Value;
                    if (AudioService.ParseAlertTagContent(tagContent, activeWave, activeWave, out int startW, out int endW, out string rawType))
                    {
                        if (activeWave >= startW && activeWave <= endW && startW > 0)
                        {
                            isTagActive = true;
                        }

                        if (rawType is "chain_commander" or "commander" or "chain" or "cta")
                            tagBadgeColor = new Vector4(1.0f, 0.72f, 0.15f, 1.0f);
                        else if (rawType == "red")
                            tagBadgeColor = new Vector4(1.0f, 0.28f, 0.32f, 1.0f);
                        else if (rawType == "green")
                            tagBadgeColor = new Vector4(0.25f, 1.0f, 0.45f, 1.0f);
                        else if (rawType == "purple")
                            tagBadgeColor = new Vector4(0.85f, 0.38f, 1.0f, 1.0f);
                        else if (rawType == "medic")
                            tagBadgeColor = new Vector4(0.20f, 0.85f, 1.0f, 1.0f);
                    }
                }

                string stripped = AudioService.AlertTagRegex.Replace(lineAfterCheck, m =>
                {
                    string inside = m.Groups[2].Success ? m.Groups[2].Value :
                        (m.Groups[3].Success ? m.Groups[3].Value : "");
                    return inside;
                }).Trim();

                if (string.IsNullOrWhiteSpace(stripped))
                {
                    Match first = matches[0];
                    string tagContent = first.Groups[1].Value;
                    AudioService.ParseAlertTagContent(tagContent, activeWave, activeWave, out int sW, out int _, out string rawType);

                    string waveLabel = sW > 0 ? $" (W:{sW})" : "";
                    string label = rawType switch
                    {
                        "chain_commander" or "commander" or "chain" or "cta" => $"[ ⚡ CHAIN COMMANDER{waveLabel} ]",
                        "red" => $"[ 🎵 DJ RED TRACK{waveLabel} ]",
                        "green" => $"[ 🎵 DJ GREEN TRACK{waveLabel} ]",
                        "purple" => $"[ 🎵 DJ PURPLE TRACK{waveLabel} ]",
                        "medic" => $"[ ➕ MEDIC CLEANSE{waveLabel} ]",
                        _ => $"[ ⚠ ALERT{waveLabel} ]"
                    };

                    displayText = label;
                }
                else
                {
                    displayText = stripped;
                }
            }

            if (!hasCheckbox)
            {
                if (IsCheckbox(displayText, out bool checkInside, out string textInside))
                {
                    hasCheckbox = true;
                    defaultCheckedInText = checkInside;
                    displayText = textInside;
                }
            }

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

                RenderLineText(displayText, isTagActive, isDone, tagBadgeColor);

                ImGui.PopID();
            }
            else
            {
                RenderLineText(displayText, isTagActive, false, tagBadgeColor);
            }

            if (indentPixels > 0)
            {
                ImGui.Unindent(indentPixels);
            }
        }

        public static void RenderLineText(string text, bool isTagActive, bool isDone, Vector4? customColor = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (isTagActive && !isDone)
            {
                Vector4 activeBadge = customColor ?? new Vector4(0.3f, 1.0f, 0.4f, 1.0f);
                SafeTextColored(activeBadge, "[WAVE!] ");
                ImGui.SameLine(0, 0);
            }

            if (isDone)
            {
                SafeTextWrapped(new Vector4(0.3f, 1.0f, 0.4f, 0.75f), text);
                return;
            }

            Vector4 textColor = isTagActive
                ? (customColor ?? new Vector4(0.3f, 1.0f, 0.4f, 1.0f))
                : (customColor.HasValue ? new Vector4(customColor.Value.X, customColor.Value.Y, customColor.Value.Z, 0.9f) : new Vector4(0.92f, 0.92f, 0.96f, 1.0f));

            SafeTextWrapped(textColor, text);
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
