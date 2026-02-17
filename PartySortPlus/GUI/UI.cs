using Dalamud.Interface.Colors;
using ECommons;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.SimpleGui;
using System;
using System.Numerics;

namespace PartySortPlus.GUI
{
    public static unsafe class UI
    {
        public static string? RequestTab = null;
        public const string AnyNotice = "Meeting any of the following conditions will result in rule being triggered:\n";

        public static void DrawMain()
        {
            // Ensure 'resolution' is defined and initialized
            string resolution = "Configuration"; // Replace with actual logic to determine resolution if needed

            // Ensure PartySortPlus.P is not null before accessing its properties
            if (PartySortPlus.P != null)
            {
                var version = PartySortPlus.P.GetType().Assembly.GetName().Version?.ToString() ?? "?";
                var pluginName = DalamudReflector.GetPluginName() ?? "Party Sort+";
                EzConfigGui.Window!.WindowName = $"{pluginName} v{version} [{resolution}]###{pluginName}";
            }
            else
            {
                var pluginName = DalamudReflector.GetPluginName() ?? "Party Sort+";
                EzConfigGui.Window!.WindowName = $"{pluginName} [Plugin Not Initialized]";
            }

            EzConfigGui.Window.RespectCloseHotkey = true;
            EzConfigGui.Window.SetSizeConstraints(new Vector2(800, 900), new Vector2(1200, 1200));

            if (PartySortPlus.C != null)
            {
                ImGuiEx.EzTabBar("TabsNR2", tabs: new (string? name, Action function, Vector4? color, bool child)[]
                {
                        (PartySortPlus.C.ShowTutorial || PartySortPlus.C.Debug ? "Tutorial" : null, UITutorial.Draw, null, true),
                        ("Rules", GuiRules.Draw, ImGuiColors.DalamudViolet, true),
                        ("Presets", GuiPresets.Draw, ImGuiColors.DalamudViolet, true),
                        (PartySortPlus.C.Debug ? "Overrides" : null, GuiOverrides.Draw, ImGuiColors.DalamudYellow, true),
                        (PartySortPlus.C.Debug ? "Settings" : null, GuiSettings.Draw, null, true),
                        InternalLog.ImGuiTab(),
                        (PartySortPlus.C.Debug ? "Debug" : null, GuiDebug.Draw, ImGuiColors.DalamudGrey3, true),
                });
            }

            RequestTab = null;
        }
    }
}
