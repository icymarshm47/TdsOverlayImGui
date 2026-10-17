using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;

namespace TdsOverlayImGui.UI.Components
{
    public static class ImageViewer
    {
        private static readonly HashSet<string> LoadedTextures = new(StringComparer.OrdinalIgnoreCase);

        public static void ClearLoadedTextures(TdsImGuiOverlay overlay)
        {
            foreach (var texPath in LoadedTextures)
            {
                try
                {
                    overlay.RemoveImage(texPath);
                }
                catch { }
            }
            LoadedTextures.Clear();
        }

        public static void RemoveTexture(TdsImGuiOverlay overlay, string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;
            if (LoadedTextures.Remove(imagePath))
            {
                try
                {
                    overlay.RemoveImage(imagePath);
                }
                catch { }
            }
        }

        public static string? GetActiveImagePath(OverlayContext ctx, PlayerStrategy player)
        {
            if (player.ImagePaths.Count == 0) return null;
            if (ctx.CurrentImageIndex >= player.ImagePaths.Count) ctx.CurrentImageIndex = 0;
            return player.ImagePaths[ctx.CurrentImageIndex];
        }

        public static string? GetActiveImagePath(OverlayContext ctx, MapStrategy map)
        {
            map.EnsureDefaultPlayer();
            return GetActiveImagePath(ctx, map.CurrentPlayer);
        }

        public static void RenderImageSelector(OverlayContext ctx, PlayerStrategy player)
        {
            if (player.ImagePaths.Count <= 1) return;

            if (ImGui.Button(Loc.Tr("PrevPhoto")))
            {
                ctx.CurrentImageIndex = (ctx.CurrentImageIndex - 1 + player.ImagePaths.Count) % player.ImagePaths.Count;
                ctx.ResetImageTransform();
            }

            ImGui.SameLine();
            ImGui.TextDisabled(string.Format(Loc.Tr("PhotoCount"), ctx.CurrentImageIndex + 1, player.ImagePaths.Count));
            ImGui.SameLine();

            if (ImGui.Button(Loc.Tr("NextPhoto")))
            {
                ctx.CurrentImageIndex = (ctx.CurrentImageIndex + 1) % player.ImagePaths.Count;
                ctx.ResetImageTransform();
            }
        }

        public static void RenderImageSelector(OverlayContext ctx, MapStrategy map)
        {
            map.EnsureDefaultPlayer();
            RenderImageSelector(ctx, map.CurrentPlayer);
        }

        public static float DrawHeightResizeHandle(OverlayContext ctx, float currentHeight, float minH, float maxH, string id)
        {
            ImGui.PushID(id);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.16f, 0.18f, 0.22f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.35f, 0.39f, 0.95f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.45f, 0.49f, 1.00f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 3));

            ImGui.Button(Loc.Tr("ResizeHandleText"), new Vector2(-1, 0));

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            bool isDragging = ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left);
            if (isDragging)
            {
                float deltaY = ImGui.GetIO().MouseDelta.Y;
                if (Math.Abs(deltaY) > 0.1f)
                {
                    currentHeight = Math.Clamp(currentHeight + deltaY, minH, maxH);
                }
            }

            if (ImGui.IsItemDeactivated())
            {
                StrategyService.SaveSettings(ctx.Settings);
            }

            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
            ImGui.PopID();

            return currentHeight;
        }

        public static void RenderImageCanvas(TdsImGuiOverlay overlay, OverlayContext ctx, string imagePath, float height, string canvasId, bool enableResizeGrip = false)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                InstructionRenderer.SafeTextColored(new Vector4(1, 0.4f, 0.4f, 1), "[Image file not found]");
                return;
            }

            if (!LoadedTextures.Contains(imagePath) && !File.Exists(imagePath))
            {
                InstructionRenderer.SafeTextColored(new Vector4(1, 0.4f, 0.4f, 1), "[Image file not found]");
                return;
            }

            try
            {
                overlay.AddOrGetImagePointer(imagePath, false, out nint handle, out uint imgW, out uint imgH);
                LoadedTextures.Add(imagePath);

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

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle) && isHovered)
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
            if (currentMap == null || !ctx.IsSeparateImageWindowOpen) return;

            currentMap.EnsureDefaultPlayer();
            var activePlayer = currentMap.CurrentPlayer;
            if (activePlayer.ImagePaths.Count == 0) return;

            string? activeImage = GetActiveImagePath(ctx, activePlayer);
            if (string.IsNullOrWhiteSpace(activeImage) || !File.Exists(activeImage)) return;

            overlay.ApplyImageWindowDragDelta();

            var wc = new ImGuiWindowClass();
            wc.DockNodeFlagsOverrideSet = ImGuiDockNodeFlags.AutoHideTabBar;
            unsafe
            {
                ImGui.SetNextWindowClass(new ImGuiWindowClassPtr(&wc));
            }

            ImGui.SetNextWindowSize(new Vector2(420, 340), ImGuiCond.FirstUseEver);
            bool isOpen = ctx.IsSeparateImageWindowOpen;
            ImGui.Begin($"{Loc.Tr("SeparateImageTitle")}###SeparateImageWindow", ref isOpen, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse);
            ctx.IsSeparateImageWindowOpen = isOpen;

            overlay.UpdateImageWindowPos();

            overlay.RenderCustomTitleBar(Loc.Tr("SeparateImageTitle"), isMainWindow: false, popupSuffix: "_img");

            RenderImageSelector(ctx, activePlayer);
            float availH = Math.Max(120, ImGui.GetContentRegionAvail().Y - 26);
            RenderImageCanvas(overlay, ctx, activeImage, availH, $"SeparateCanvas_{ctx.CurrentImageIndex}", enableResizeGrip: false);

            ImGui.End();
        }
    }
}
