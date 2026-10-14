using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TdsOverlayImGui.Services
{
    public static class AudioService
    {
        public static readonly Regex AlertTagRegex = new Regex(
            @"[<\[](?:ocr|alert|wave)\s+([\d]+(?:-[\d]+)?)(?:[\s,:]+([a-zA-Z0-9_\-]+(?:\s+[a-zA-Z0-9_\-]+)*))?\s*(?:>(.*?)</(?:ocr|alert|wave)>|\](.*?)[\[]/(?:ocr|alert|wave)\]|/?>|\])",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public static void PlayOcrBeep()
        {
            Task.Run(() =>
            {
                try
                {
                    System.Media.SystemSounds.Asterisk.Play();
                }
                catch
                {
                    try
                    {
                        Console.Beep(880, 100);
                        Console.Beep(1175, 180);
                    }
                    catch { }
                }
            });
        }

        public static void PlayCommanderTone()
        {
            Task.Run(() =>
            {
                try
                {
                    System.Media.SystemSounds.Exclamation.Play();
                }
                catch
                {
                    try
                    {
                        Console.Beep(587, 100);
                        Console.Beep(880, 200);
                    }
                    catch { }
                }
            });
        }

        public static void OnWaveChanged(OverlayContext ctx, int newWave)
        {
            if (newWave < ctx.PreviousWaveNumber)
            {
                RemoveTriggersForWaveOrHigher(ctx.TriggeredOcrSounds, newWave);
                RemoveTriggersForWaveOrHigher(ctx.TriggeredDjAlerts, newWave);
            }
            if (newWave <= 1)
            {
                ctx.TriggeredDjAlerts.Clear();
                ctx.TriggeredOcrSounds.Clear();
            }
            ctx.PreviousWaveNumber = newWave;
        }

        private static void RemoveTriggersForWaveOrHigher(HashSet<string> set, int targetWave)
        {
            set.RemoveWhere(key =>
            {
                int wIndex = key.IndexOf("|w:", StringComparison.Ordinal);
                if (wIndex >= 0)
                {
                    int start = wIndex + 3;
                    int end = key.IndexOf('|', start);
                    string waveStr = end >= 0 ? key[start..end] : key[start..];
                    if (int.TryParse(waveStr, out int w))
                    {
                        return w >= targetWave;
                    }
                }

                var parts = key.Split('_');
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    if (int.TryParse(parts[i], out int w))
                    {
                        return w >= targetWave;
                    }
                }
                return false;
            });
        }

        public static void TriggerAlert(OverlayContext ctx, string alertType, string customTitle = "")
        {
            ctx.AlertToastMaxDuration = 4.0f;
            ctx.AlertToastTimer = ctx.AlertToastMaxDuration;

            string type = alertType.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
            switch (type)
            {
                case "red":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastRed");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastRedSub");
                    ctx.AlertToastColor = new Vector4(1.0f, 0.28f, 0.32f, 1.0f);
                    PlayOcrBeep();
                    break;

                case "green":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastGreen");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastGreenSub");
                    ctx.AlertToastColor = new Vector4(0.25f, 1.0f, 0.45f, 1.0f);
                    PlayOcrBeep();
                    break;

                case "purple":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastPurple");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastPurpleSub");
                    ctx.AlertToastColor = new Vector4(0.85f, 0.38f, 1.0f, 1.0f);
                    PlayOcrBeep();
                    break;

                case "chain_commander":
                case "commander":
                case "chain":
                case "cta":
                    ctx.AlertToastBadge = Loc.Tr("CommanderBadge");
                    ctx.AlertToastTitle = Loc.Tr("CommanderToastTitle");
                    ctx.AlertToastSubtitle = Loc.Tr("CommanderToastSubtitle");
                    ctx.AlertToastColor = new Vector4(1.0f, 0.72f, 0.15f, 1.0f);
                    PlayCommanderTone();
                    break;

                case "medic":
                    ctx.AlertToastBadge = "MEDIC ABILITY";
                    ctx.AlertToastTitle = Loc.Tr("MedicToastTitle");
                    ctx.AlertToastSubtitle = Loc.Tr("MedicToastSubtitle");
                    ctx.AlertToastColor = new Vector4(0.20f, 0.85f, 1.0f, 1.0f);
                    PlayCommanderTone();
                    break;

                default:
                    ctx.AlertToastBadge = "ALERT";
                    ctx.AlertToastTitle = string.IsNullOrWhiteSpace(customTitle) ? alertType.ToUpperInvariant() : customTitle;
                    ctx.AlertToastSubtitle = "";
                    ctx.AlertToastColor = new Vector4(0.40f, 0.75f, 1.0f, 1.0f);
                    PlayCommanderTone();
                    break;
            }
        }

        public static void TriggerDjToast(OverlayContext ctx, string color)
        {
            TriggerAlert(ctx, color);
        }

        public static void CheckAndTriggerAlertsForWave(OverlayContext ctx, int activeWave)
        {
            var map = ctx.CurrentMap;
            if (map == null) return;

            ctx.UpdateStepByWaveNumber(map, activeWave);

            ScanTextForAlerts(ctx, map.TaskId, map.GeneralInfo, activeWave);

            for (int i = 0; i < map.Steps.Count; i++)
            {
                var s = map.Steps[i];
                ScanTextForAlerts(ctx, map.TaskId, s.Instruction, activeWave);
            }
        }

        private static void ScanTextForAlerts(OverlayContext ctx, string mapTaskId, string? text, int activeWave)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var matches = AlertTagRegex.Matches(text);
            foreach (Match match in matches)
            {
                string waveSpec = match.Groups[1].Value.Trim();
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
                    string rawType = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";
                    string customInside = match.Groups[3].Success ? match.Groups[3].Value.Trim() :
                        (match.Groups[4].Success ? match.Groups[4].Value.Trim() : "");

                    string normType = rawType.ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
                    if (string.IsNullOrEmpty(normType))
                    {
                        normType = "alert";
                    }

                    string alertKey = $"{mapTaskId}|w:{activeWave}|a:{normType}";
                    if (ctx.TriggeredDjAlerts.Add(alertKey))
                    {
                        TriggerAlert(ctx, normType, customInside);
                    }
                }
            }
        }
    }
}
