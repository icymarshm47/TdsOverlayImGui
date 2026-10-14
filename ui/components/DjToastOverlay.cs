using System;
using System.Numerics;
using ImGuiNET;

namespace TdsOverlayImGui.UI.Components
{
    public static class DjToastOverlay
    {
        public static void Render(OverlayContext ctx)
        {
            if (ctx.AlertToastTimer <= 0.0f) return;

            ctx.AlertToastTimer -= ImGui.GetIO().DeltaTime;
            if (ctx.AlertToastTimer < 0.0f)
            {
                ctx.AlertToastTimer = 0.0f;
                return;
            }

            var io = ImGui.GetIO();
            float fadeIn = Math.Clamp((ctx.AlertToastMaxDuration - ctx.AlertToastTimer) / 0.20f, 0.0f, 1.0f);
            float fadeOut = Math.Clamp(ctx.AlertToastTimer / 0.35f, 0.0f, 1.0f);
            float alpha = Math.Min(fadeIn, fadeOut);

            float screenW = io.DisplaySize.X > 0 ? io.DisplaySize.X : 1920.0f;
            float screenH = io.DisplaySize.Y > 0 ? io.DisplaySize.Y : 1080.0f;
            float bannerWidth = Math.Min(480.0f, Math.Max(360.0f, screenW - 60.0f));

            ImGui.SetNextWindowPos(new Vector2(screenW * 0.5f, screenH * 0.12f), ImGuiCond.Always, new Vector2(0.5f, 0.0f));
            ImGui.SetNextWindowSizeConstraints(new Vector2(360.0f, -1.0f), new Vector2(bannerWidth, -1.0f));

            Vector4 bg = new Vector4(0.08f, 0.09f, 0.12f, 0.96f * alpha);
            Vector4 border = new Vector4(ctx.AlertToastColor.X, ctx.AlertToastColor.Y, ctx.AlertToastColor.Z, 0.90f * alpha);
            Vector4 accentColor = new Vector4(ctx.AlertToastColor.X, ctx.AlertToastColor.Y, ctx.AlertToastColor.Z, alpha);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, bg);
            ImGui.PushStyleColor(ImGuiCol.Border, border);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(18.0f, 14.0f));

            ImGui.Begin("AlertToastWindow",
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing);

            Vector2 winPos = ImGui.GetWindowPos();
            Vector2 winSize = ImGui.GetWindowSize();
            var drawList = ImGui.GetWindowDrawList();

            uint accentU32 = ImGui.ColorConvertFloat4ToU32(accentColor);
            drawList.AddRectFilled(
                new Vector2(winPos.X, winPos.Y),
                new Vector2(winPos.X + 5.0f, winPos.Y + winSize.Y),
                accentU32,
                4.0f,
                ImDrawFlags.RoundCornersLeft);

            string badge = string.IsNullOrWhiteSpace(ctx.AlertToastBadge) ? "ALERT" : ctx.AlertToastBadge;

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(ctx.AlertToastColor.X * 0.22f, ctx.AlertToastColor.Y * 0.22f, ctx.AlertToastColor.Z * 0.22f, 0.90f * alpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(ctx.AlertToastColor.X * 0.22f, ctx.AlertToastColor.Y * 0.22f, ctx.AlertToastColor.Z * 0.22f, 0.90f * alpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(ctx.AlertToastColor.X * 0.22f, ctx.AlertToastColor.Y * 0.22f, ctx.AlertToastColor.Z * 0.22f, 0.90f * alpha));
            ImGui.PushStyleColor(ImGuiCol.Text, accentColor);
            ImGui.SmallButton(badge);
            ImGui.PopStyleColor(4);

            ImGui.Spacing();

            SafeTextColored(new Vector4(1.0f, 1.0f, 1.0f, alpha), ctx.AlertToastTitle);

            if (!string.IsNullOrWhiteSpace(ctx.AlertToastSubtitle))
            {
                ImGui.Spacing();
                SafeTextColored(new Vector4(0.82f, 0.86f, 0.92f, 0.92f * alpha), ctx.AlertToastSubtitle);
            }

            ImGui.Spacing();

            float progress = Math.Clamp(ctx.AlertToastTimer / Math.Max(0.1f, ctx.AlertToastMaxDuration), 0.0f, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, accentColor);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.16f, 0.18f, 0.24f, 0.70f * alpha));
            ImGui.ProgressBar(progress, new Vector2(-1, 4), "");
            ImGui.PopStyleColor(2);

            ImGui.End();

            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(2);
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
