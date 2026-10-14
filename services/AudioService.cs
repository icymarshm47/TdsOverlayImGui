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

        public static void TriggerDjToast(OverlayContext ctx, string color)
        {
            ctx.DjToastTimer = 3.0f;
            if (color == "red")
            {
                ctx.DjToastMessage = Loc.Tr("DjToastRed");
                ctx.DjToastColor = new Vector4(1.0f, 0.2f, 0.2f, 1.0f);
            }
            else if (color == "green")
            {
                ctx.DjToastMessage = Loc.Tr("DjToastGreen");
                ctx.DjToastColor = new Vector4(0.2f, 1.0f, 0.2f, 1.0f);
            }
            else if (color == "purple")
            {
                ctx.DjToastMessage = Loc.Tr("DjToastPurple");
                ctx.DjToastColor = new Vector4(0.7f, 0.2f, 1.0f, 1.0f);
            }
        }
    }
}
