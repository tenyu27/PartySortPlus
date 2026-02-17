using ECommons.ImGuiMethods;
using ECommons.Logging;
using Dalamud.Bindings.ImGui;
using PartySortPlus;
using PartySortPlus.Configuration;

namespace PartySortPlus.GUI
{
    public static unsafe class GuiDebug
    {
        public static void Draw()
        {
            if (PartySortPlus.C == null) return;

            ImGui.TextColored(new System.Numerics.Vector4(0.2f, 0.6f, 1.0f, 1.0f), "Debugging options for developers. Use with caution."); // Header with colored text
            ImGui.Separator();

            if (ImGui.Button("Wipe All Rules"))
            {
                WipeAllRules();
            }
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 0, 0, 1)); // Red text
            ImGuiEx.TextWrapped("WARNING: This action will wipe all rules and cannot be undone!");
            ImGui.PopStyleColor();

            ImGui.Separator();

            if (ImGui.Button("Wipe All Presets"))
            {
                WipeAllPresets();
            }
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 0, 0, 1)); // Red text
            ImGuiEx.TextWrapped("WARNING: This action will wipe all presets and cannot be undone!");
            ImGui.PopStyleColor();
            ImGui.Separator();

            // Print current window size
            var windowSize = ImGui.GetWindowSize();
            ImGui.Text($"Current Window Size: {windowSize.X} x {windowSize.Y}");
        }

        private static void WipeAllRules()
        {
            // Logic to wipe all rules from the profile
            PartySortPlus.C?.GlobalProfile.Rules.Clear();
            PluginLog.Debug("All rules wiped");
        }

        private static void WipeAllPresets()
        {
            // Logic to wipe all presets from the profile
            PartySortPlus.C?.GlobalProfile.Presets.Clear();
            PluginLog.Debug("All presets wiped");
        }
    }
}
