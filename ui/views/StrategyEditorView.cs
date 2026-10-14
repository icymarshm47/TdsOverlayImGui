using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace TdsOverlayImGui.UI.Views
{
    public static class StrategyEditorView
    {
        public static void Render(TdsImGuiOverlay overlay, OverlayContext ctx)
        {
            var currentMap = ctx.CurrentMap;
            if (currentMap == null) return;

            InstructionRenderer.SafeTextColored(new Vector4(1f, 0.8f, 0.2f, 1f), Loc.Tr("EditingTitle"));
            ImGui.Spacing();

            ImGui.Text($"{Loc.Tr("MapName")}:");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##EditMapName", ref ctx.EditMapName, 512);

            ImGui.Text($"{Loc.Tr("Difficulty")}:");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##EditDifficulty", ref ctx.EditDifficulty, 256);

            ImGui.Text($"{Loc.Tr("StrategyVariant")}:");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##EditStrategyName", ref ctx.EditStrategyName, 256);

            ImGui.Spacing();
            ImGui.Text(Loc.Tr("GeneralInfoLabel"));
            ImGui.InputTextMultiline("##EditGeneralInfo", ref ctx.EditGeneralInfo, 65536, new Vector2(-1, 65), ImGuiInputTextFlags.AllowTabInput);

            ImGui.Separator();
            InstructionRenderer.SafeTextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), Loc.Tr("ImagesHeader"));

            for (int imgIdx = 0; imgIdx < ctx.EditImagePaths.Count; imgIdx++)
            {
                ImGui.PushID($"mapImg_{imgIdx}");
                string imgPath = ctx.EditImagePaths[imgIdx];

                ImGui.Text(string.Format(Loc.Tr("PhotoNum"), imgIdx + 1));
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 210);
                if (ImGui.InputText($"##ImgPath_{imgIdx}", ref imgPath, 512))
                {
                    ctx.EditImagePaths[imgIdx] = imgPath;
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Tr("SelectFileBtn")))
                {
                    string? picked = ImagePickerHelper.OpenImageFileDialog(ctx.Settings.AlwaysOnTop);
                    if (!string.IsNullOrEmpty(picked))
                    {
                        string oldPath = ctx.EditImagePaths[imgIdx];
                        if (!string.IsNullOrEmpty(oldPath))
                        {
                            ImageViewer.RemoveTexture(overlay, oldPath);
                        }
                        ctx.EditImagePaths[imgIdx] = picked;
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("X"))
                {
                    string oldPath = ctx.EditImagePaths[imgIdx];
                    ctx.EditImagePaths.RemoveAt(imgIdx);
                    if (!string.IsNullOrEmpty(oldPath))
                    {
                        ImageViewer.RemoveTexture(overlay, oldPath);
                    }
                    ImGui.PopID();
                    break;
                }

                ImGui.PopID();
            }

            if (ImGui.Button(Loc.Tr("AddPhotoBtn")))
            {
                string? picked = ImagePickerHelper.OpenImageFileDialog(ctx.Settings.AlwaysOnTop);
                ctx.EditImagePaths.Add(picked ?? "");
            }

            ImGui.SameLine();
            if (ImGui.Button(Loc.Tr("PasteClipboardBtn")))
            {
                string? saved = ImagePickerHelper.SaveImageFromClipboard(ctx.EditMapName, ctx.EditImagePaths.Count + 1);
                if (!string.IsNullOrEmpty(saved))
                {
                    ctx.EditImagePaths.Add(saved);
                    ctx.EditorToastMessage = Loc.Tr("PhotoAddedToast");
                    ctx.EditorToastTimer = 2.5f;
                }
                else
                {
                    ctx.EditorToastMessage = Loc.Tr("ClipboardNoImageToast");
                    ctx.EditorToastTimer = 2.5f;
                }
            }

            if (ctx.EditorToastTimer > 0.0f && !string.IsNullOrEmpty(ctx.EditorToastMessage))
            {
                ctx.EditorToastTimer -= ImGui.GetIO().DeltaTime;
                ImGui.SameLine();
                InstructionRenderer.SafeTextColored(new Vector4(1.0f, 0.85f, 0.3f, 1.0f), ctx.EditorToastMessage);
            }

            ImGui.Separator();
            ImGui.Text(Loc.Tr("StepsHeader"));

            for (int i = 0; i < ctx.EditSteps.Count; i++)
            {
                var s = ctx.EditSteps[i];
                ImGui.PushID(i);

                ImGui.BeginChild($"StepEditCard_{i}", new Vector2(0, 142), true);

                ImGui.TextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), $"{Loc.Tr("Step")} #{i + 1}");

                ImGui.SameLine();
                if (i > 0 && ImGui.SmallButton("^"))
                {
                    (ctx.EditSteps[i - 1], ctx.EditSteps[i]) = (ctx.EditSteps[i], ctx.EditSteps[i - 1]);
                }
                ImGui.SameLine();
                if (i < ctx.EditSteps.Count - 1 && ImGui.SmallButton("v"))
                {
                    (ctx.EditSteps[i + 1], ctx.EditSteps[i]) = (ctx.EditSteps[i], ctx.EditSteps[i + 1]);
                }

                int start = s.StartWave;
                int end = s.EndWave;
                string inst = s.Instruction;

                ImGui.SameLine();
                ImGui.SetNextItemWidth(75);
                if (ImGui.InputInt(Loc.Tr("FromWave"), ref start, 0)) s.StartWave = Math.Max(1, start);
                ImGui.SameLine();

                ImGui.SetNextItemWidth(75);
                if (ImGui.InputInt(Loc.Tr("ToWave"), ref end, 0)) s.EndWave = Math.Max(1, end);

                if (ctx.EditSteps.Count > 1)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 38);
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.20f, 0.22f, 1.0f));
                    if (ImGui.SmallButton("X"))
                    {
                        ctx.EditSteps.RemoveAt(i);
                        ImGui.PopStyleColor();
                        ImGui.EndChild();
                        ImGui.PopID();
                        break;
                    }
                    ImGui.PopStyleColor();
                }

                if (ImGui.InputTextMultiline($"##StepInst_{i}", ref inst, 65536, new Vector2(-1, 82), ImGuiInputTextFlags.AllowTabInput))
                {
                    s.Instruction = inst;
                }

                ImGui.EndChild();
                ImGui.PopID();
            }

            if (ImGui.Button(Loc.Tr("AddStepBtn"), new Vector2(-1, 0)))
            {
                int lastEnd = ctx.EditSteps.Count > 0 ? ctx.EditSteps[^1].EndWave : 0;
                ctx.EditSteps.Add(new StrategyStep
                {
                    StartWave = lastEnd + 1,
                    EndWave = lastEnd + 10,
                    Instruction = "- [ ] ..."
                });
            }

            ImGui.Spacing();

            float editAvailW = ImGui.GetContentRegionAvail().X;
            float editSpacing = ImGui.GetStyle().ItemSpacing.X;
            float editBtnW = (editAvailW - editSpacing) / 2.0f;

            if (ImGui.Button(Loc.Tr("Save"), new Vector2(editBtnW, 0)))
            {
                ImageViewer.ClearLoadedTextures(overlay);

                string mapName = string.IsNullOrWhiteSpace(ctx.EditMapName) ? "Map" : ctx.EditMapName.Trim();
                string difficulty = string.IsNullOrWhiteSpace(ctx.EditDifficulty) ? "Normal" : ctx.EditDifficulty.Trim();
                string strategyName = string.IsNullOrWhiteSpace(ctx.EditStrategyName) ? "Main" : ctx.EditStrategyName.Trim();

                foreach (var step in ctx.EditSteps)
                {
                    step.StartWave = Math.Clamp(step.StartWave, 1, 200);
                    step.EndWave = Math.Clamp(step.EndWave, step.StartWave, 200);
                }

                currentMap.MapName = mapName;
                currentMap.Difficulty = difficulty;
                currentMap.StrategyName = strategyName;
                currentMap.GeneralInfo = ctx.EditGeneralInfo;
                currentMap.ImagePaths = new List<string>(ctx.EditImagePaths);
                currentMap.Steps = new List<StrategyStep>(ctx.EditSteps);

                StrategyService.SaveStrategy(currentMap);
                ctx.IsEditing = false;
            }

            ImGui.SameLine();

            if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(editBtnW, 0)))
            {
                ImageViewer.ClearLoadedTextures(overlay);
                ctx.IsEditing = false;
            }

            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.20f, 0.22f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.80f, 0.25f, 0.28f, 1.0f));
            if (ImGui.Button(Loc.Tr("DeleteEntireStrategyBtn"), new Vector2(-1, 0)))
            {
                ctx.ShowDeleteConfirmModal = true;
            }
            ImGui.PopStyleColor(2);
        }
    }
}
