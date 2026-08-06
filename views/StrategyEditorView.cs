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
            ImGui.InputText("##EditMapName", ref ctx.EditMapName, 500000);

            ImGui.Text($"{Loc.Tr("Difficulty")}:");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##EditDifficulty", ref ctx.EditDifficulty, 500000);

            ImGui.Text($"{Loc.Tr("StrategyVariant")}:");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##EditStrategyName", ref ctx.EditStrategyName, 500000);

            ImGui.Spacing();
            ImGui.Text(Loc.Tr("GeneralInfoLabel"));
            ImGui.InputTextMultiline("##EditGeneralInfo", ref ctx.EditGeneralInfo, 500000, new Vector2(-1, 60), ImGuiInputTextFlags.AllowTabInput);

            ImGui.Separator();
            InstructionRenderer.SafeTextColored(new Vector4(0.35f, 0.39f, 0.95f, 1.0f), Loc.Tr("ImagesHeader"));

            for (int imgIdx = 0; imgIdx < currentMap.ImagePaths.Count; imgIdx++)
            {
                ImGui.PushID($"mapImg_{imgIdx}");
                string imgPath = currentMap.ImagePaths[imgIdx];

                ImGui.Text(string.Format(Loc.Tr("PhotoNum"), imgIdx + 1));
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 110);
                if (ImGui.InputText($"##ImgPath_{imgIdx}", ref imgPath, 260))
                {
                    currentMap.ImagePaths[imgIdx] = imgPath;
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Tr("DeletePhotoBtn")))
                {
                    currentMap.ImagePaths.RemoveAt(imgIdx);
                    ImGui.PopID();
                    break;
                }
                ImGui.PopID();
            }

            if (ImGui.Button(Loc.Tr("AddPhotoBtn")))
            {
                currentMap.ImagePaths.Add("");
            }

            ImGui.Separator();
            ImGui.Text(Loc.Tr("StepsHeader"));
            ImGui.TextDisabled(Loc.Tr("MarkdownHint"));

            for (int i = 0; i < currentMap.Steps.Count; i++)
            {
                var s = currentMap.Steps[i];
                ImGui.PushID(i);

                ImGui.TextDisabled($"{Loc.Tr("Step")} #{i + 1}");

                int start = s.StartWave;
                int end = s.EndWave;
                string inst = s.Instruction;

                ImGui.SetNextItemWidth(70);
                if (ImGui.InputInt(Loc.Tr("FromWave"), ref start, 0)) s.StartWave = start;
                ImGui.SameLine();

                ImGui.SetNextItemWidth(70);
                if (ImGui.InputInt(Loc.Tr("ToWave"), ref end, 0)) s.EndWave = end;

                ImGui.Text(Loc.Tr("StepInstructionLabel"));
                if (ImGui.InputTextMultiline($"##StepInst_{i}", ref inst, 500000, new Vector2(-1, 60), ImGuiInputTextFlags.AllowTabInput))
                {
                    s.Instruction = inst;
                }

                if (currentMap.Steps.Count > 1)
                {
                    if (ImGui.Button(Loc.Tr("DeleteStepBtn"), new Vector2(-1, 0)))
                    {
                        currentMap.Steps.RemoveAt(i);
                        ImGui.PopID();
                        break;
                    }
                }

                ImGui.Separator();
                ImGui.PopID();
            }

            if (ImGui.Button(Loc.Tr("AddStepBtn"), new Vector2(-1, 0)))
            {
                int lastEnd = currentMap.Steps.Count > 0 ? currentMap.Steps[^1].EndWave : 1;
                currentMap.Steps.Add(new StrategyStep
                {
                    StartWave = lastEnd + 1,
                    EndWave = lastEnd + 10,
                    Instruction = "..."
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

            if (ImGui.Button(Loc.Tr("DeleteEntireStrategyBtn"), new Vector2(-1, 0)))
            {
                ctx.ShowDeleteConfirmModal = true;
            }
        }
    }
}