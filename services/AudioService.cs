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
            @"(?:<ocr\s+(\d+(?:-\d+)?)\s+([\w\-]+)>(.*?)</ocr(?:\s+\1\s+\2)?\s*>|\[ocr\s+(\d+(?:-\d+)?)\s+([\w\-]+)\](.*?)\[/ocr(?:\s+\4\s+\5)?\s*\])",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public static void PlayOcrBeep()
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
            ctx.LastCommanderAlertTime = DateTime.MinValue;
            ctx.LastMedicAlertTime = DateTime.MinValue;
            ctx.CommanderRotationIndex = 0;
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

        public static bool ParseAlertMatch(Match match, out int startW, out int endW, out string alertType, out string innerText)
        {
            startW = 0;
            endW = 0;
            alertType = "";
            innerText = "";

            if (!match.Success) return false;

            string waveSpec = match.Groups[1].Success ? match.Groups[1].Value : (match.Groups[4].Success ? match.Groups[4].Value : "");
            string rawType = match.Groups[2].Success ? match.Groups[2].Value : (match.Groups[5].Success ? match.Groups[5].Value : "");
            innerText = match.Groups[3].Success ? match.Groups[3].Value : (match.Groups[6].Success ? match.Groups[6].Value : "");

            if (!ParseWaveSpec(waveSpec, out startW, out endW))
            {
                return false;
            }

            alertType = NormalizeAlertType(rawType);
            return true;
        }

        public static bool ParseWaveSpec(string waveSpec, out int startW, out int endW)
        {
            startW = 0;
            endW = 0;
            if (string.IsNullOrWhiteSpace(waveSpec)) return false;

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

            return startW > 0;
        }

        public static string NormalizeAlertType(string raw)
        {
            string t = raw.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
            return t switch
            {
                "dj_red" or "djred" or "dj" or "red" => "red",
                "dj_green" or "djgreen" or "green" => "green",
                "dj_purple" or "djpurple" or "purple" => "purple",
                "commander" or "chain" or "cta" or "chain_commander" => "chain_commander",
                "cleanse" or "medic_cleanse" or "medic" => "medic",
                _ => t
            };
        }

        public static void TriggerAlert(OverlayContext ctx, string alertType, string customTitle = "")
        {
            ctx.AlertToastMaxDuration = 4.0f;
            ctx.AlertToastTimer = ctx.AlertToastMaxDuration;

            string type = NormalizeAlertType(alertType);
            switch (type)
            {
                case "red":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastRed");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastRedSub");
                    ctx.AlertToastColor = new Vector4(1.0f, 0.28f, 0.32f, 1.0f);
                    PlayCommanderTone();
                    break;

                case "green":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastGreen");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastGreenSub");
                    ctx.AlertToastColor = new Vector4(0.25f, 1.0f, 0.45f, 1.0f);
                    PlayCommanderTone();
                    break;

                case "purple":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastPurple");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastPurpleSub");
                    ctx.AlertToastColor = new Vector4(0.85f, 0.38f, 1.0f, 1.0f);
                    PlayCommanderTone();
                    break;

                case "chain_commander":
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
                if (!ParseAlertMatch(match, out int startW, out int endW, out string normType, out string innerText))
                {
                    continue;
                }

                if (activeWave >= startW && activeWave <= endW && startW > 0)
                {
                    string alertKey = $"{mapTaskId}|w:{activeWave}|a:{normType}";
                    if (ctx.TriggeredDjAlerts.Add(alertKey))
                    {
                        if (normType == "chain_commander")
                        {
                            ctx.LastCommanderAlertTime = DateTime.Now;
                            ctx.CommanderRotationIndex = 1;
                            ctx.AlertToastBadge = Loc.Tr("CommanderBadge");
                            ctx.AlertToastTitle = $"{Loc.Tr("CommanderToastTitle")} [#1]";
                            ctx.AlertToastSubtitle = string.Format(Loc.Tr("CommanderRotateNotice"), 1);
                            ctx.AlertToastColor = new Vector4(1.0f, 0.72f, 0.15f, 1.0f);
                            ctx.AlertToastMaxDuration = 4.0f;
                            ctx.AlertToastTimer = ctx.AlertToastMaxDuration;
                            PlayCommanderTone();
                        }
                        else if (normType == "medic")
                        {
                            ctx.LastMedicAlertTime = DateTime.Now;
                            TriggerAlert(ctx, normType, innerText);
                        }
                        else
                        {
                            TriggerAlert(ctx, normType, innerText);
                        }
                    }
                }
            }
        }

        public static void ProcessPeriodicAlerts(OverlayContext ctx)
        {
            var map = ctx.CurrentMap;
            if (map == null) return;

            int activeWave = (ctx.Settings.EnableOcr && ctx.DetectedWaveNumber.HasValue)
                ? ctx.DetectedWaveNumber.Value
                : ctx.CurrentWaveNumber;

            bool isCommanderActive = false;
            bool isMedicActive = false;

            ScanActiveAlerts(map.GeneralInfo, activeWave, ref isCommanderActive, ref isMedicActive);

            for (int i = 0; i < map.Steps.Count; i++)
            {
                ScanActiveAlerts(map.Steps[i].Instruction, activeWave, ref isCommanderActive, ref isMedicActive);
            }

            if (isCommanderActive)
            {
                if (ctx.LastCommanderAlertTime == DateTime.MinValue)
                {
                    ctx.LastCommanderAlertTime = DateTime.Now;
                    ctx.CommanderRotationIndex = 1;
                }
                else if ((DateTime.Now - ctx.LastCommanderAlertTime).TotalSeconds >= 10.0)
                {
                    ctx.LastCommanderAlertTime = DateTime.Now;
                    int commNum = (ctx.CommanderRotationIndex % 3) + 1;
                    ctx.CommanderRotationIndex++;

                    ctx.AlertToastBadge = Loc.Tr("CommanderBadge");
                    ctx.AlertToastTitle = $"{Loc.Tr("CommanderToastTitle")} [#{commNum}]";
                    ctx.AlertToastSubtitle = string.Format(Loc.Tr("CommanderRotateNotice"), commNum);
                    ctx.AlertToastColor = new Vector4(1.0f, 0.72f, 0.15f, 1.0f);
                    ctx.AlertToastMaxDuration = 4.0f;
                    ctx.AlertToastTimer = ctx.AlertToastMaxDuration;
                    PlayCommanderTone();
                }
            }
            else
            {
                ctx.LastCommanderAlertTime = DateTime.MinValue;
                ctx.CommanderRotationIndex = 0;
            }

            if (isMedicActive)
            {
                if (ctx.LastMedicAlertTime == DateTime.MinValue)
                {
                    ctx.LastMedicAlertTime = DateTime.Now;
                }
                else if ((DateTime.Now - ctx.LastMedicAlertTime).TotalSeconds >= 30.0)
                {
                    ctx.LastMedicAlertTime = DateTime.Now;
                    ctx.AlertToastBadge = "MEDIC ABILITY";
                    ctx.AlertToastTitle = Loc.Tr("MedicToastTitle");
                    ctx.AlertToastSubtitle = Loc.Tr("MedicToastSubtitle");
                    ctx.AlertToastColor = new Vector4(0.20f, 0.85f, 1.0f, 1.0f);
                    ctx.AlertToastMaxDuration = 4.0f;
                    ctx.AlertToastTimer = ctx.AlertToastMaxDuration;
                    PlayCommanderTone();
                }
            }
            else
            {
                ctx.LastMedicAlertTime = DateTime.MinValue;
            }
        }

        private static void ScanActiveAlerts(string? text, int activeWave, ref bool isCommanderActive, ref bool isMedicActive)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var matches = AlertTagRegex.Matches(text);
            foreach (Match match in matches)
            {
                if (ParseAlertMatch(match, out int startW, out int endW, out string normType, out _))
                {
                    if (activeWave >= startW && activeWave <= endW && startW > 0)
                    {
                        if (normType == "chain_commander")
                        {
                            isCommanderActive = true;
                        }
                        else if (normType == "medic")
                        {
                            isMedicActive = true;
                        }
                    }
                }
            }
        }
    }
}
