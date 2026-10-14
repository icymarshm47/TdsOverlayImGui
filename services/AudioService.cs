using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace TdsOverlayImGui.Services
{
    public static class AudioService
    {
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

        public static void OnWaveChanged(OverlayContext ctx, int newWave)
        {
            if (newWave < ctx.PreviousWaveNumber)
            {
                RemoveTriggersForWaveOrHigher(ctx.TriggeredOcrSounds, newWave);
                RemoveTriggersForWaveOrHigher(ctx.TriggeredDjAlerts, newWave);
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

        public static void TriggerAlert(OverlayContext ctx, string alertType)
        {
            ctx.AlertToastMaxDuration = 4.0f;
            ctx.AlertToastTimer = ctx.AlertToastMaxDuration;

            string type = alertType.Trim().ToLowerInvariant();
            switch (type)
            {
                case "red":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastRed");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastRedSub");
                    ctx.AlertToastColor = new Vector4(1.0f, 0.28f, 0.32f, 1.0f);
                    break;

                case "green":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastGreen");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastGreenSub");
                    ctx.AlertToastColor = new Vector4(0.25f, 1.0f, 0.45f, 1.0f);
                    break;

                case "purple":
                    ctx.AlertToastBadge = Loc.Tr("DjBadge");
                    ctx.AlertToastTitle = Loc.Tr("DjToastPurple");
                    ctx.AlertToastSubtitle = Loc.Tr("DjToastPurpleSub");
                    ctx.AlertToastColor = new Vector4(0.85f, 0.38f, 1.0f, 1.0f);
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

                default:
                    ctx.AlertToastBadge = "ALERT";
                    ctx.AlertToastTitle = alertType.ToUpperInvariant();
                    ctx.AlertToastSubtitle = "";
                    ctx.AlertToastColor = new Vector4(0.40f, 0.75f, 1.0f, 1.0f);
                    break;
            }
        }

        public static void TriggerDjToast(OverlayContext ctx, string color)
        {
            TriggerAlert(ctx, color);
        }
    }
}
