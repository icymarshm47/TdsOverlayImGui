using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using TdsOverlayImGui.Services;

namespace TdsOverlayImGui.UI.Views
{
    public static class OcrOverlayView
    {
        private static int _isScanning = 0;
        private static int _pendingDetectedWave = -1;
        private static string? _pendingRawText = null;

        public static void ProcessBackgroundOcr(OverlayContext ctx)
        {
            if (_pendingRawText != null)
            {
                ctx.LastOcrRawText = _pendingRawText;
                _pendingRawText = null;

                if (string.IsNullOrWhiteSpace(ctx.LastOcrRawText))
                {
                    ctx.LastOcrStatus = Loc.Tr("OcrScanningTag");
                }
                else
                {
                    ctx.LastOcrStatus = $"\"{ctx.LastOcrRawText}\"";
                }
            }

            int waveVal = Interlocked.Exchange(ref _pendingDetectedWave, -1);
            if (waveVal > 0)
            {
                if (ctx.Settings.EnableOcr && ctx.CurrentMap != null)
                {
                    ctx.LastOcrSuccessTime = DateTime.Now;
                    ctx.LastOcrStatus = $"{Loc.Tr("Wave")} {waveVal}";

                    AudioService.OnWaveChanged(ctx, waveVal);
                    ctx.DetectedWaveNumber = waveVal;
                    ctx.CurrentWaveNumber = waveVal;

                    AudioService.CheckAndTriggerAlertsForWave(ctx, waveVal);
                }
            }

            bool shouldOneShot = ctx.TriggerOneShotOcr;
            if (shouldOneShot)
            {
                ctx.TriggerOneShotOcr = false;
            }

            if ((!ctx.Settings.EnableOcr && !shouldOneShot) || ctx.CurrentMap == null || _isScanning == 1) return;

            if (shouldOneShot || (DateTime.Now - ctx.LastOcrScanTime).TotalSeconds >= 1.0)
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
                        var (wave, rawText) = await OcrService.RecognizeWaveFromScreenAsync(x, y, w, h, preprocess);
                        _pendingRawText = rawText;
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
                : new Vector2(1920, 1080);

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(screenSize);
            ImGui.Begin("OcrSelectorWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground);

            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddRectFilled(Vector2.Zero, screenSize, 0x88000000);
            drawList.AddText(new Vector2(40, 40), 0xFF00FFFF, Loc.Tr("OcrOverlayInstruction"));

            if (ctx.OcrSelectionState == 0)
            {
                drawList.AddLine(new Vector2(io.MousePos.X - 15, io.MousePos.Y), new Vector2(io.MousePos.X + 15, io.MousePos.Y), 0xFF00FFFF, 1.5f);
                drawList.AddLine(new Vector2(io.MousePos.X, io.MousePos.Y - 15), new Vector2(io.MousePos.X, io.MousePos.Y + 15), 0xFF00FFFF, 1.5f);

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
                    if (w > 4 && h > 4)
                    {
                        ctx.Settings.OcrX = (int)min.X;
                        ctx.Settings.OcrY = (int)min.Y;
                        ctx.Settings.OcrW = w;
                        ctx.Settings.OcrH = h;
                        ctx.Settings.EnableOcr = true;

                        StrategyService.SaveSettings(ctx.Settings);

                        ctx.LastOcrScanTime = DateTime.MinValue;
                        ctx.DetectedWaveNumber = null;
                        ctx.TriggerOneShotOcr = true;
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
