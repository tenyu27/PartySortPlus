using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.Logging;
using ECommons.Schedulers;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PartySortPlus.Checkers;
using PartySortPlus.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Action = System.Action;

namespace PartySortPlus.GUI
{
    public static class GuiRules
    {
        private static Vector2 iconSize => new(24f);

        private static string[] Filters = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];
        private static bool[] OnlySelected = new bool[20];
        private static string? CurrentDrag = "";

        public static void Draw()
        {
            if (PartySortPlus.C != null)
            {
                var Profile = Utils.GetProfile();
                Profile.Rules.RemoveAll(x => x == null);

                if (ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    Profile.Rules.Add(new());
                    // Get the new rule's GUID and print it
                    PluginLog.Debug($"New rule: {Profile.Rules.Last().GUID}");
                    PluginLog.Debug($"Rule count: {Profile.Rules.Count}");
                }
                ImGuiEx.Tooltip("Add new rule");

                var active = (bool[])[
                    PartySortPlus.C.Cond_Territory,
                    PartySortPlus.C.Cond_Jobs,
                    PartySortPlus.C.Cond_PartyJobs,
                ];

                List<(Vector2 RowPos, Vector2 ButtonPos, Action BeginDraw, Action AcceptDraw)> MoveCommands = [];

                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);

                if (ImGui.BeginTable("##main", 3 + active.Count(x => x), ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Reorderable))
                {
                    ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                    if (PartySortPlus.C.Cond_Territory) ImGui.TableSetupColumn("Zone");
                    if (PartySortPlus.C.Cond_Jobs) ImGui.TableSetupColumn("Job(s)");
                    if (PartySortPlus.C.Cond_PartyJobs) ImGui.TableSetupColumn("Party Job(s)");
                    ImGui.TableSetupColumn("Preset");
                    ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableHeadersRow();

                    for (var i = 0; i < Profile.Rules.Count; i++)
                    {
                        var filterCnt = 0;
                        void FiltersSelection()
                        {
                            ImGui.SetWindowFontScale(0.8f);
                            ImGuiEx.SetNextItemFullWidth();
                            ImGui.InputTextWithHint($"##fltr{filterCnt}", "Filter...", ref Filters[filterCnt], 50);
                            ImGui.Checkbox($"Only selected##{filterCnt}", ref OnlySelected[filterCnt]);
                            ImGui.SetWindowFontScale(1f);
                            ImGui.Separator();
                        }

                        var rule = Profile.Rules[i];
                        var col = !rule.Enabled;

                        if (col) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);

                        ImGui.PushID(rule.GUID);
                        ImGui.TableNextRow();

                        if (CurrentDrag == rule.GUID)
                        {
                            var color = GradientColor.Get(EColor.Green, EColor.Green with { W = EColor.Green.W / 4 }, 500).ToUint();
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, color);
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, color);
                        }
                        ImGui.TableNextColumn();

                        //Sorting
                        var rowPos = ImGui.GetCursorPos();
                        ImGui.Checkbox("##enable", ref rule.Enabled);
                        ImGuiEx.Tooltip("Enable this rule");

                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        var cur = ImGui.GetCursorPos();
                        var size = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString());
                        ImGui.Dummy(size);
                        ImGui.PopFont();

                        var moveIndex = i;

                        MoveCommands.Add((rowPos, cur, delegate
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}##Move{rule.GUID}");
                            ImGui.PopFont();
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                            }
                            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
                            {
                                ImGuiDragDrop.SetDragDropPayload("MoveRule", rule.GUID);
                                CurrentDrag = rule.GUID;
                                InternalLog.Verbose($"DragDropSource = {rule.GUID}");
                                ImGui.EndDragDropSource();
                            }
                            else if (CurrentDrag == rule.GUID)
                            {
                                InternalLog.Verbose($"Current drag reset!");
                                CurrentDrag = null;
                            }
                        }, delegate { DragDropUtils.AcceptRuleDragDrop(Profile, moveIndex); }
                        ));

                        // Zones
                        if (PartySortPlus.C.Cond_Territory)
                        {
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                            var territoryNames = rule.Territories.Select(x => ExcelTerritoryHelper.GetName(x));
                            var printRangeText = territoryNames.PrintRange(out var fullList);

                            if (ImGui.BeginCombo("##zone", printRangeText, PartySortPlus.C.ComboSize))
                            {
                                new TerritorySelector(rule.Territories, (terr, s) => rule.Territories = [.. s])
                                {
                                    ActionDrawPlaceName = DrawPlaceName
                                };
                                ImGui.CloseCurrentPopup();
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }

                        // Jobs (That of the player)
                        if (PartySortPlus.C.Cond_Jobs)
                        {
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                            var jobNames = rule.Jobs.Select(x => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)x).Abbreviation.ToString());
                            var printRangeText = jobNames.PrintRange(out var fullList);

                            if (ImGui.BeginCombo("##job", printRangeText, PartySortPlus.C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var cond in Enum.GetValues<Job>().OrderByDescending(x => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)x).Role))
                                {
                                    if (cond == Job.ADV) continue;
                                    if (cond.IsUpgradeable()) continue;
                                    if (cond.IsDoh() || cond.IsDol()) continue; // Todo: See if people want these but personally I don't think they should be here.
                                    var name = cond.ToString().Replace("_", " ");
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Jobs.Contains(cond)) continue;
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)cond.GetIcon(), false, out var texture))
                                    {
                                        ImGui.Image(texture.Handle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    if (cond.IsUpgradeable()) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
                                    DrawSelector(name, cond, rule.Jobs, rule.Not.Jobs);
                                    if (cond.IsUpgradeable()) ImGui.PopStyleColor();
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        // Jobs (That of the party)
                        if (PartySortPlus.C.Cond_PartyJobs)
                        {
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            var jobNames = rule.PartyJobs.Select(x => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)x).Abbreviation.ToString());
                            var printRangeText = jobNames.PrintRange(out var fullList);
                            if (ImGui.BeginCombo("##partyjob", printRangeText, PartySortPlus.C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var cond in Enum.GetValues<Job>().OrderByDescending(x => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)x).Role))
                                {
                                    if (cond == Job.ADV) continue;
                                    if (cond.IsUpgradeable()) continue;
                                    if (cond.IsDoh() || cond.IsDol()) continue; // Todo: See if people want these but personally I don't think they should be here.
                                    var name = cond.ToString().Replace("_", " ");
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.PartyJobs.Contains(cond)) continue;
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)cond.GetIcon(), false, out var texture))
                                    {
                                        ImGui.Image(texture.Handle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    if (cond.IsUpgradeable()) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
                                    DrawSelector(name, cond, rule.PartyJobs, rule.Not.PartyJobs);
                                    if (cond.IsUpgradeable()) ImGui.PopStyleColor();
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        // Presets
                        ImGui.TableNextColumn();
                        {
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##glamour", rule.SelectedPresets.Count > 0 ? rule.SelectedPresets[0] : "- None -", PartySortPlus.C.ComboSize))
                            {
                                FiltersSelection();
                                var designs = Profile.Presets.OrderBy(x => x.Name);
                                foreach (var x in designs)
                                {
                                    var name = x.Name;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && rule.SelectedPresets.Count > 0 && rule.SelectedPresets[0] != name) continue;

                                    bool selected = rule.SelectedPresets.Count > 0 && rule.SelectedPresets[0] == name;
                                    if (ImGui.Selectable(name, selected))
                                    {
                                        rule.SelectedPresets.Clear();
                                        rule.SelectedPresets.Add(name);
                                    }
                                }
                                ImGui.EndCombo();
                            }
                            if (rule.SelectedPresets.Count > 0)
                                ImGuiEx.Tooltip(UI.AnyNotice + rule.SelectedPresets[0]);
                            filterCnt++;
                        }

                        // Delete
                        ImGui.TableNextColumn();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                        {
                            new TickScheduler(() => Profile.Rules.RemoveAll(x => x.GUID == rule.GUID));
                            PluginLog.Debug($"Deleted rule {rule.GUID}");
                        }
                        ImGuiEx.Tooltip("Hold CTRL+Click to delete");

                        if (col) ImGui.PopStyleColor();
                        ImGui.PopID();
                    }

                    ImGui.EndTable();

                    foreach (var x in MoveCommands)
                    {
                        ImGui.SetCursorPos(x.ButtonPos);
                        x.BeginDraw();
                        x.AcceptDraw();
                        ImGui.SetCursorPos(x.RowPos);
                        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGuiHelpers.GetButtonSize(" ").Y));
                        x.AcceptDraw();
                    }
                }
                ImGui.PopStyleVar();
            }
        }

        private static void DrawPlaceName(TerritoryType t, Vector4? nullable, string arg2)
        {
            var cond = t.FindBiome();
            var assemblyLocation = Svc.PluginInterface.AssemblyLocation?.DirectoryName;

            if (cond != Biome.No_biome && assemblyLocation != null &&
                ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(assemblyLocation, "images", "biome", $"{(int)cond}.png"), out var texture))
            {
                ImGui.Image(texture.Handle, iconSize);
                ImGui.SameLine();
            }
            ImGuiEx.Text(nullable, arg2);
        }

        private static void DrawSelector<T>(string name, T value, ICollection<T> values, ICollection<T> notValues) => DrawSelector(name, [value], values, notValues);

        private static void DrawSelector<T>(string name, IEnumerable<T> value, ICollection<T> values, ICollection<T> notValues)
        {
            if (PartySortPlus.C == null) return;

            var buttonSize = ImGuiHelpers.GetButtonSize(" ");
            var size = new Vector2(buttonSize.Y);
            sbyte s = 0;
            if (values.ContainsAny(value)) s = 1;
            if (notValues.ContainsAny(value)) s = -1;

            var checkbox = new TristateCheckboxEx();

            if (checkbox.Draw(name, s, out s))
            {
                if (!PartySortPlus.C.AllowNegativeConditions && s == -1)
                {
                    s = 0;
                }
                if (s == 1)
                {
                    foreach (var v in value)
                    {
                        notValues.Remove(v);
                        values.Add(v);
                    }
                }
                else if (s == 0)
                {
                    foreach (var v in value)
                    {
                        notValues.Remove(v);
                        values.Remove(v);
                    }
                }
                else
                {
                    foreach (var v in value)
                    {
                        notValues.Add(v);
                        values.Remove(v);
                    }
                }
            }
            if (s == -1)
            {
                ImGuiEx.Tooltip($"If matching any condition with cross, rule will not be applied.");
            }
        }
    }
}
