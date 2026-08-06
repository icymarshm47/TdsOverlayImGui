using System;
using System.IO;
using System.Numerics;
using ImGuiNET;

namespace TdsOverlayImGui.Components
{
    public static class ImageViewer
    {
        public static string? GetActiveImagePath(OverlayContext ctx, MapStrategy map)
        {
            if (map.ImagePaths.Count == 0) return null;
            if (ctx.CurrentImageIndex >= map.ImagePaths.Count) ctx.CurrentImageIndex = 0;
            return map.ImagePaths[ctx.CurrentImageIndex];
        }

        public static void RenderImageSelector(OverlayContext ctx, MapStrategy map)
        {
            if (map.ImagePaths.Count <= 1) return;

            if (ImGui.Button(Loc.Tr("PrevPhoto")))
            {
                ctx.CurrentImageIndex = (ctx.CurrentImageIndex - 1 + map.ImagePaths.Count) % map.ImagePaths.Count;
                ctx.ResetImageTransform();
            }

            ImGui.SameLine();
            ImGui.TextDisabled(string.Format(Loc.Tr("PhotoCount"), ctx.CurrentImageIndex + 1, map.ImagePaths.Count));
            ImGui.SameLine();

            if (ImGui.Button(Loc.Tr("NextPhoto")))
            {
                ctx.CurrentImageIndex = (ctx.CurrentImageIndex + 1) % map.ImagePaths.Count;
                ctx.ResetImageTransform();
            }
        }

        public static float DrawHeightResizeHandle(OverlayContext ctx, float currentHeight, float minH, float maxH, string id)
        {
            ImGui.PushID(id);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.18f, 0.20f, 0.22f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.35f, 0.39f, 0.95f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.45f, 0.49f, 1.00f, 1.0f));

            ImGui.Button(Loc.Tr("ResizeHandleText"), new Vector2(-1, 26));

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                float deltaY = ImGui.GetIO().MouseDelta.Y;
                if (Math.Abs(deltaY) > 0.1f)
                {
                    currentHeight += deltaY;
                    currentHeight = Math.Clamp(currentHeight, minH, maxH);
                    StrategyService.SaveSettings(ctx.Settings);
                }
            }

            ImGui.PopStyleColor(3);
            ImGui.PopID();

            return currentHeight;
        }

        public static void RenderImageCanvas(TdsImGuiOverlay overlay, OverlayContext ctx, string imagePath, float height, string canvasId, bool enableResizeGrip = false)
        {
            try
            {
                overlay.AddOrGetImagePointer(imagePath, false, out nint handle, out uint imgW, out uint imgH);

                if (handle != nint.Zero && imgW > 0 && imgH > 0)
                {
                    float availW = ImGui.GetContentRegionAvail().X;
                    float baseScale = availW / imgW;

                    Vector2 displaySize = new Vector2(imgW * baseScale * ctx.ImageScale, imgH * baseScale * ctx.ImageScale);

                    float parentScrollY = ImGui.GetScrollY();

                    ImGui.BeginChild($"ImageScrollRegion_{canvasId}", new Vector2(0, height), true, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);

                    Vector2 startPos = ImGui.GetCursorPos();
                    Vector2 drawPos = startPos + ctx.ImageOffset;

                    ImGui.SetCursorPos(drawPos);
                    ImGui.Image(handle, displaySize);

                    ImGui.SetCursorPos(drawPos);
                    ImGui.InvisibleButton($"##PanArea_{canvasId}", displaySize);

                    bool isHovered = ImGui.IsItemHovered() || ImGui.IsWindowHovered();
                    var io = ImGui.GetIO();

                    if (isHovered)
                    {
                        float wheel = io.MouseWheel;
                        if (wheel != 0)
                        {
                            if (wheel > 0)
                                ctx.ImageScale = Math.Min(5.0f, ctx.ImageScale + 0.15f);
                            else if (wheel < 0)
                                ctx.ImageScale = Math.Max(0.2f, ctx.ImageScale - 0.15f);

                            io.MouseWheel = 0;
                        }

                        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                        {
                            ctx.ImageOffset += io.MouseDelta;
                        }
                    }

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle) || (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Middle)))
                    {
                        ctx.ResetImageTransform();
                    }

                    ImGui.EndChild();

                    if (isHovered)
                    {
                        ImGui.SetScrollY(parentScrollY);
                    }

                    if (enableResizeGrip)
                    {
                        ctx.Settings.EmbeddedImageBoxHeight = DrawHeightResizeHandle(ctx, ctx.Settings.EmbeddedImageBoxHeight, 100.0f, 600.0f, $"EmbeddedImageHandle_{canvasId}");
                    }

                    ImGui.TextDisabled(string.Format(Loc.Tr("ZoomNotice"), (int)(ctx.ImageScale * 100)));
                }
            }
            catch
            {
                InstructionRenderer.SafeTextColored(new Vector4(1, 0.4f, 0.4f, 1), "[Error loading image]");
            }
        }

        public static void RenderSeparateImageWindow(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            var currentMap = ctx.CurrentMap;
            if (currentMap == null || currentMap.ImagePaths.Count == 0) return;

            string? activeImage = GetActiveImagePath(ctx, currentMap);
            if (string.IsNullOrWhiteSpace(activeImage) || !File.Exists(activeImage)) return;

            ImGui.SetNextWindowSize(new Vector2(400, 320), ImGuiCond.FirstUseEver);
            ImGui.Begin($"{Loc.Tr("SeparateImageTitle")}##SeparateImageWindow", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse);

            RenderImageSelector(ctx, currentMap);
            RenderImageCanvas(overlay, ctx, activeImage, 230, $"SeparateCanvas_{ctx.CurrentImageIndex}", enableResizeGrip: false);

            ImGui.End();
        }
    }
}