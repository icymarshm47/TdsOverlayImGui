using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;

namespace TdsOverlayImGui.Views
{
    public static class OcrOverlayView
    {
        private static int _isScanning = 0;
        private static int _pendingDetectedWave = -1;

        public static void ProcessBackgroundOcr(OverlayContext ctx)
        {
            int waveVal = Interlocked.Exchange(ref _pendingDetectedWave, -1);
            if (waveVal > 0)
            {
                if (ctx.Settings.EnableOcr && ctx.CurrentMap != null)
                {
                    if (waveVal < ctx.CurrentWaveNumber && waveVal <= 2)
                    {
                        ctx.TriggeredDjAlerts.Clear();
                        ctx.TriggeredOcrSounds.Clear();
                    }

                    AudioService.OnWaveChanged(ctx, waveVal);
                    ctx.DetectedWaveNumber = waveVal;
                    ctx.CurrentWaveNumber = waveVal;

                    ctx.UpdateStepByWaveNumber(ctx.CurrentMap, waveVal);
                }
            }

            if (!ctx.Settings.EnableOcr || ctx.CurrentMap == null || _isScanning == 1) return;

            if ((DateTime.Now - ctx.LastOcrScanTime).TotalSeconds >= 1.2)
            {
                ctx.LastOcrScanTime = DateTime.Now;
                Interlocked.Exchange(ref _isScanning, 1);

                int x = ctx.Settings.OcrX;
                int y = ctx.Settings.OcrY;
                int w = ctx.Settings.OcrW;
                int h = ctx.Settings.OcrH;
                bool preprocess = ctx.Settings.OcrPreprocess;

                Task.Run(async () =>
                {
                    try
                    {
                        int? wave = await OcrService.RecognizeWaveFromScreenAsync(x, y, w, h, preprocess);
                        if (wave.HasValue)
                        {
                            Interlocked.Exchange(ref _pendingDetectedWave, wave.Value);
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isScanning, 0);
                    }
                });
            }
        }

        public static void RenderSelectionOverlay(OverlayContext ctx)
        {
            var io = ImGui.GetIO();
            Vector2 screenSize = io.DisplaySize.X > 0 && io.DisplaySize.Y > 0
                ? io.DisplaySize
                : new Vector2(3840, 2160);

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(screenSize);
            ImGui.Begin("OcrSelectorWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground);

            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddRectFilled(Vector2.Zero, screenSize, 0x99000000);
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
