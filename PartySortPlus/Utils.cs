using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PartySortPlus.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using PartySortPlus.Checkers;

namespace PartySortPlus
{
    public static unsafe class Utils
    {
        public static Vector2 CellPadding => ImGui.GetStyle().CellPadding + new Vector2(0, 2);

        public static Profile GetProfile()
        {
            if (PartySortPlus.C != null)
            {
                return PartySortPlus.C.GlobalProfile;
            }
            else
            {
                throw new InvalidOperationException("PartySortPlus.C is null. Cannot retrieve profile.");
            }
        }

        public static string PrintRange(this IEnumerable<string> s, out string FullList, string noneStr = "Any")
        {
            FullList = null;
            var list = s.ToArray();
            if (list.Length == 0) return noneStr;
            if (list.Length == 1) return list[0].ToString();
            FullList = list.Select(x => x.ToString()).Join("\n");
            return $"{list.Length} selected";
        }
    }
}
