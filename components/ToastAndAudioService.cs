using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;

namespace TdsOverlayImGui.Components
{
    public static class ToastAndAudioService
    {
        public static void PlayOcrBeep()
        {
            Task.Run(() =>
            {
                try
                {
                    Console.Beep(880, 100);  // A5
                    Console.Beep(1175, 180); // D6
                }
                catch { }
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

        public static void RenderDjToast(OverlayContext ctx)
        {
            if (ctx.DjToastTimer > 0)
            {
                ctx.DjToastTimer -= ImGui.GetIO().DeltaTime;
                
                var io = ImGui.GetIO();
                ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.25f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
                
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.09f, 0.95f));
                ImGui.PushStyleColor(ImGuiCol.Border, ctx.DjToastColor);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(25, 18));

                ImGui.Begin("DjToastWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing);
                
                ImGui.SetWindowFontScale(1.4f);
                SafeTextColored(ctx.DjToastColor, ctx.DjToastMessage);
                ImGui.SetWindowFontScale(1.0f);

                ImGui.End();

                ImGui.PopStyleVar(3);
                ImGui.PopStyleColor(2);
            }
        }

        private static void SafeTextColored(Vector4 color, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
        }
    }
}