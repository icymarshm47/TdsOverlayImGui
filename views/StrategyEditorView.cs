using System;
using System.Numerics;
using ImGuiNET;
using TdsOverlayImGui.Components;

namespace TdsOverlayImGui.Views
{
    public static class StrategyEditorView
    {
        public static void Render(OverlayContext ctx)
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

            for (int imgIdx = 0; imgIdx < currentMap.ImagePaths.Count; imgIdx++)
            {
                ImGui.PushID($"mapImg_{imgIdx}");
                string imgPath = currentMap.ImagePaths[imgIdx];

                ImGui.Text(string.Format(Loc.Tr("PhotoNum"), imgIdx + 1));
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 210);
                if (ImGui.InputText($"##ImgPath_{imgIdx}", ref imgPath, 512))
                {
                    currentMap.ImagePaths[imgIdx] = imgPath;
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Tr("SelectFileBtn")))
                {
                    string? picked = ImagePickerHelper.OpenImageFileDialog();
                    if (!string.IsNullOrEmpty(picked))
                    {
                        currentMap.ImagePaths[imgIdx] = picked;
                    }
                }

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.20f, 0.22f, 1.0f));
                if (ImGui.Button("X"))
                {
                    currentMap.ImagePaths.RemoveAt(imgIdx);
                    ImGui.PopStyleColor();
                    ImGui.PopID();
                    break;
                }
                ImGui.PopStyleColor();

                ImGui.PopID();
            }

            if (ImGui.Button(Loc.Tr("AddPhotoBtn")))
            {
                string? picked = ImagePickerHelper.OpenImageFileDialog();
                currentMap.ImagePaths.Add(picked ?? "");
            }

            ImGui.SameLine();
            if (ImGui.Button(Loc.Tr("PasteClipboardBtn")))
            {
                string? saved = ImagePickerHelper.SaveImageFromClipboard(ctx.EditMapName, currentMap.ImagePaths.Count + 1);
                if (!string.IsNullOrEmpty(saved))
                {
                    currentMap.ImagePaths.Add(saved);
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
            ImGui.TextDisabled(Loc.Tr("MarkdownHint"));

            for (int i = 0; i < currentMap.Steps.Count; i++)
            {
                var s = currentMap.Steps[i];
                ImGui.PushID(i);

                ImGui.BeginChild($"StepEditCard_{i}", new Vector2(0, 142), true);

                ImGui.TextColored(new Vector4(0.45f, 0.52f, 1.0f, 1.0f), $"{Loc.Tr("Step")} #{i + 1}");

                ImGui.SameLine();
                if (i > 0 && ImGui.SmallButton("^"))
                {
                    (currentMap.Steps[i - 1], currentMap.Steps[i]) = (currentMap.Steps[i], currentMap.Steps[i - 1]);
                }
                ImGui.SameLine();
                if (i < currentMap.Steps.Count - 1 && ImGui.SmallButton("v"))
                {
                    (currentMap.Steps[i + 1], currentMap.Steps[i]) = (currentMap.Steps[i], currentMap.Steps[i + 1]);
                }

                int start = s.StartWave;
                int end = s.EndWave;
                string inst = s.Instruction;

                ImGui.SameLine();
                ImGui.SetNextItemWidth(75);
                if (ImGui.InputInt(Loc.Tr("FromWave"), ref start, 0)) s.StartWave = start;
                ImGui.SameLine();

                ImGui.SetNextItemWidth(75);
                if (ImGui.InputInt(Loc.Tr("ToWave"), ref end, 0)) s.EndWave = end;

                if (currentMap.Steps.Count > 1)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 38);
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.20f, 0.22f, 1.0f));
                    if (ImGui.SmallButton("X"))
                    {
                        currentMap.Steps.RemoveAt(i);
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
                int lastEnd = currentMap.Steps.Count > 0 ? currentMap.Steps[^1].EndWave : 0;
                currentMap.Steps.Add(new StrategyStep
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
                currentMap.MapName = ctx.EditMapName;
                currentMap.Difficulty = ctx.EditDifficulty;
                currentMap.StrategyName = ctx.EditStrategyName;
                currentMap.GeneralInfo = ctx.EditGeneralInfo;

                StrategyService.SaveStrategy(currentMap);
                ctx.IsEditing = false;
            }

            ImGui.SameLine();

            if (ImGui.Button(Loc.Tr("Cancel"), new Vector2(editBtnW, 0)))
            {
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
