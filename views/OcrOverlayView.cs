using System;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;

namespace TdsOverlayImGui.Views
{
    public static class OcrOverlayView
    {
        private static bool _isScanning = false;

        public static void ProcessBackgroundOcr(OverlayContext ctx)
        {
            if (!ctx.Settings.EnableOcr || ctx.CurrentMap == null || _isScanning) return;

            if ((DateTime.Now - ctx.LastOcrScanTime).TotalSeconds >= 1.2)
            {
                ctx.LastOcrScanTime = DateTime.Now;
                _isScanning = true;

                Task.Run(async () =>
                {
                    try
                    {
                        int? wave = await OcrService.RecognizeWaveFromScreenAsync(
                            ctx.Settings.OcrX,
                            ctx.Settings.OcrY,
                            ctx.Settings.OcrW,
                            ctx.Settings.OcrH,
                            ctx.Settings.OcrPreprocess);

                        if (wave.HasValue && ctx.CurrentMap != null)
                        {
                            if (wave.Value < ctx.CurrentWaveNumber && wave.Value <= 2)
                            {
                                ctx.TriggeredDjAlerts.Clear();
                                ctx.TriggeredOcrSounds.Clear();
                            }

                            Components.ToastAndAudioService.OnWaveChanged(ctx, wave.Value);
                            ctx.DetectedWaveNumber = wave.Value;
                            ctx.CurrentWaveNumber = wave.Value;

                            ctx.UpdateStepByWaveNumber(ctx.CurrentMap, wave.Value);
                        }
                    }
                    finally
                    {
                        _isScanning = false;
                    }
                });
            }
        }

        public static void RenderSelectionOverlay(OverlayContext ctx)
        {
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(3840, 2160));
            ImGui.Begin("OcrSelectorWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground);

            var drawList = ImGui.GetForegroundDrawList();
            var io = ImGui.GetIO();

            drawList.AddRectFilled(Vector2.Zero, new Vector2(3840, 2160), 0x99000000);
            drawList.AddText(new Vector2(30, 30), 0xFF00FFFF, Loc.Tr("OcrOverlayInstruction"));

            if (ctx.OcrSelectionState == 0)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ctx.OcrDragStart = io.MousePos;
                    ctx.OcrSelectionState = 1;
                }
            }
            else if (ctx.OcrSelectionState == 1)
            {
                Vector2 min = Vector2.Min(ctx.OcrDragStart, io.MousePos);
                Vector2 max = Vector2.Max(ctx.OcrDragStart, io.MousePos);

                int w = (int)(max.X - min.X);
                int h = (int)(max.Y - min.Y);

                drawList.AddRect(min, max, 0xFF00FFFF, 0, ImDrawFlags.None, 2.5f);
                drawList.AddRectFilled(min, max, 0x3300FFFF);
                drawList.AddText(max + new Vector2(10, 10), 0xFFFFFFFF, $"{w} x {h} px");

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    if (w > 2 && h > 2)
                    {
                        ctx.Settings.OcrX = (int)min.X;
                        ctx.Settings.OcrY = (int)min.Y;
                        ctx.Settings.OcrW = w;
                        ctx.Settings.OcrH = h;
                        ctx.Settings.EnableOcr = true;

                        StrategyService.SaveSettings(ctx.Settings);

                        ctx.LastOcrScanTime = DateTime.MinValue;
                        ctx.DetectedWaveNumber = null;
                    }

                    ctx.IsSelectingOcrRegion = false;
                    ctx.OcrSelectionState = 0;
                }
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                ctx.IsSelectingOcrRegion = false;
                ctx.OcrSelectionState = 0;
            }

            ImGui.End();
        }
    }
}
