using System.Numerics;
using ImGuiNET;

namespace TdsOverlayImGui.Components
{
    public static class DjToastOverlay
    {
        public static void Render(OverlayContext ctx)
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
