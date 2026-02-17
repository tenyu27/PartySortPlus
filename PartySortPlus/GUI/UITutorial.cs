using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using System.IO;

namespace PartySortPlus.GUI
{
    public static class UITutorial
    {
        public static void Draw()
        {
            if (PartySortPlus.C != null)
            {
                ImGuiEx.CheckboxInverted("Hide Tutorial!", ref PartySortPlus.C.ShowTutorial);
            }
            ImGui.Separator();
            ImGuiEx.TextWrapped("Welcome to Party Sort+!");
            ImGuiEx.TextWrapped("This is a tutorial to help you get started with the plugin. This plugin allows you to dynamically sort your party according to a given preset, while limiting the sorting to take place only if you are in the correct instance, job or have the correct party member jobs for the preset to be applicable.");
            ImGui.Separator();

            if (ImGui.CollapsingHeader("Rules"))
            {
                ImGui.TextWrapped("The rules tab is used to define scenarios in which a party sorting will be applicable.");
                ImGui.TextWrapped("You can define a rule to be applied when you are in a specific instance, or when you are on a specific job.");
                ImGui.TextWrapped("You can also have rules which apply when a given job is in your party.");
                ImGui.TextWrapped("Rules are processed top-down in the ordering and stop when the first processed rule is completed. So be sure to have the correct order!");

                var assemblyLocation = Svc.PluginInterface.AssemblyLocation?.DirectoryName;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    var imagePath = Path.Combine(assemblyLocation, "images", "tutorial", "rules.png");
                    if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var tex))
                    {
                        var availableWidth = ImGui.GetContentRegionAvail().X;
                        var aspectRatio = (float)tex.Height / tex.Width;
                        var imageSize = new System.Numerics.Vector2(availableWidth, availableWidth * aspectRatio);
                        ImGui.Image(tex.Handle, imageSize);
                    }
                }
            }

            if (ImGui.CollapsingHeader("Presets"))
            {
                ImGui.TextWrapped("The presets tab is used to define the sorting order of your party members.");
                ImGui.TextWrapped("Currently your job will NOT be at the top of the list, but it will be sorted according to the preset instead. But that is currently a work in progress!");

                var assemblyLocation = Svc.PluginInterface.AssemblyLocation?.DirectoryName;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    var imagePath = Path.Combine(assemblyLocation, "images", "tutorial", "presets.png");
                    if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var tex))
                    {
                        var availableWidth = ImGui.GetContentRegionAvail().X;
                        var aspectRatio = (float)tex.Height / tex.Width;
                        var imageSize = new System.Numerics.Vector2(availableWidth, availableWidth * aspectRatio);
                        ImGui.Image(tex.Handle, imageSize);
                    }
                }
            }
        }
    }
}
