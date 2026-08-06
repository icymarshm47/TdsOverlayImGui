using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;

namespace TdsOverlayImGui.Components
{
    public static class InstructionRenderer
    {
        private static readonly Regex OcrTagRegex = new Regex(@"<ocr\s+([\d\-]+)(?:\s+(red|green|purple))?\s*>(.*?)</ocr[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

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

            var ocrMatches = OcrTagRegex.Matches(lineAfterCheck);
            bool isTagActive = false;
            string displayText = lineAfterCheck;

            if (ocrMatches.Count > 0)
            {
                foreach (Match ocrMatch in ocrMatches)
                {
                    string waveSpec = ocrMatch.Groups[1].Value.Trim();
                    string djColor = ocrMatch.Groups[2].Value.Trim().ToLower();

                    int startW = 0, endW = 0;
                    if (waveSpec.Contains('-'))
                    {
                        var parts = waveSpec.Split('-');
                        int.TryParse(parts[0], out startW);
                        int.TryParse(parts[1], out endW);
                    }
                    else
                    {
                        int.TryParse(waveSpec, out startW);
                        endW = startW;
                    }

                    if (activeWave >= startW && activeWave <= endW && startW > 0)
                    {
                        isTagActive = true;

                        string soundKey = $"{taskKey}_{activeWave}";
                        if (ctx.TriggeredOcrSounds.Add(soundKey))
                        {
                            ToastAndAudioService.PlayOcrBeep();
                        }

                        if (!string.IsNullOrEmpty(djColor))
                        {
                            string alertKey = $"{taskKey}_{activeWave}_{djColor}";
                            if (ctx.TriggeredDjAlerts.Add(alertKey))
                            {
                                ToastAndAudioService.TriggerDjToast(ctx, djColor);
                            }
                        }
                    }
                }

                displayText = OcrTagRegex.Replace(lineAfterCheck, "$3");
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

                RenderLineText(displayText, isTagActive, isDone);

                ImGui.PopID();
            }
            else
            {
                RenderLineText(displayText, isTagActive, false);
            }

            if (indentPixels > 0)
            {
                ImGui.Unindent(indentPixels);
            }
        }

        public static void RenderLineText(string text, bool isTagActive, bool isDone)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (isTagActive && !isDone)
            {
                SafeTextColored(new Vector4(0.3f, 1.0f, 0.4f, 1.0f), "[WAVE!] ");
                ImGui.SameLine(0, 0);
            }

            if (isDone)
            {
                SafeTextWrapped(new Vector4(0.3f, 1.0f, 0.4f, 0.85f), text);
                return;
            }

            Vector4 plainColor = isTagActive ? new Vector4(0.3f, 1.0f, 0.4f, 1.0f) : new Vector4(0.92f, 0.92f, 0.96f, 1.0f);
            SafeTextWrapped(plainColor, text);
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