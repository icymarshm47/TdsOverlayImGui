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
            float alpha = Math.Clamp(ctx.AlertToastTimer / 0.35f, 0.0f, 1.0f);
            float bannerWidth = Math.Min(480.0f, io.DisplaySize.X - 40.0f);

            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.12f), ImGuiCond.Always, new Vector2(0.5f, 0.0f));
            ImGui.SetNextWindowSize(new Vector2(bannerWidth, 0.0f), ImGuiCond.Always);

            Vector4 bg = new Vector4(0.07f, 0.08f, 0.11f, 0.96f * alpha);
            Vector4 border = new Vector4(ctx.AlertToastColor.X, ctx.AlertToastColor.Y, ctx.AlertToastColor.Z, 0.85f * alpha);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, bg);
            ImGui.PushStyleColor(ImGuiCol.Border, border);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1.5f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20.0f, 14.0f));

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

            uint accentU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(ctx.AlertToastColor.X, ctx.AlertToastColor.Y, ctx.AlertToastColor.Z, alpha));

            drawList.AddRectFilled(
                new Vector2(winPos.X, winPos.Y + 4.0f),
                new Vector2(winPos.X + 4.5f, winPos.Y + winSize.Y - 4.0f),
                accentU32,
                2.0f);

            string badge = string.IsNullOrWhiteSpace(ctx.AlertToastBadge) ? "ALERT" : ctx.AlertToastBadge;
            Vector2 badgeTextSize = ImGui.CalcTextSize(badge);
            Vector2 badgeScreenPos = ImGui.GetCursorScreenPos();

            drawList.AddRectFilled(
                badgeScreenPos - new Vector2(5.0f, 2.0f),
                badgeScreenPos + badgeTextSize + new Vector2(5.0f, 2.0f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(ctx.AlertToastColor.X * 0.20f, ctx.AlertToastColor.Y * 0.20f, ctx.AlertToastColor.Z * 0.20f, 0.9f * alpha)),
                4.0f);

            drawList.AddRect(
                badgeScreenPos - new Vector2(5.0f, 2.0f),
                badgeScreenPos + badgeTextSize + new Vector2(5.0f, 2.0f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(ctx.AlertToastColor.X, ctx.AlertToastColor.Y, ctx.AlertToastColor.Z, 0.65f * alpha)),
                4.0f,
                ImDrawFlags.None,
                1.0f);

            SafeTextColored(new Vector4(ctx.AlertToastColor.X, ctx.AlertToastColor.Y, ctx.AlertToastColor.Z, alpha), badge);

            ImGui.Spacing();

            SafeTextColored(new Vector4(1.0f, 1.0f, 1.0f, alpha), ctx.AlertToastTitle);

            if (!string.IsNullOrWhiteSpace(ctx.AlertToastSubtitle))
            {
                ImGui.Spacing();
                SafeTextColored(new Vector4(0.78f, 0.82f, 0.88f, 0.85f * alpha), ctx.AlertToastSubtitle);
            }

            float progress = Math.Clamp(ctx.AlertToastTimer / Math.Max(0.1f, ctx.AlertToastMaxDuration), 0.0f, 1.0f);
            float barY = winPos.Y + winSize.Y - 3.0f;
            uint trackBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.18f, 0.24f, 0.6f * alpha));

            drawList.AddLine(new Vector2(winPos.X + 8.0f, barY), new Vector2(winPos.X + winSize.X - 8.0f, barY), trackBg, 3.0f);
            drawList.AddLine(new Vector2(winPos.X + 8.0f, barY), new Vector2(winPos.X + 8.0f + (winSize.X - 16.0f) * progress, barY), accentU32, 3.0f);

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
