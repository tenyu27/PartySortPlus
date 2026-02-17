using ECommons.ImGuiMethods;
using ECommons.Logging;
using Dalamud.Bindings.ImGui;
using PartySortPlus.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartySortPlus.GUI
{
    public static class DragDropUtils
    {
        public static void AcceptRuleDragDrop(Profile currentProfile, int i)
        {
            if (ImGui.BeginDragDropTarget())
            {
                if (ImGuiDragDrop.AcceptDragDropPayload("MoveRule", out var payload, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                {
                    MoveItemToPosition(currentProfile.Rules, (x) => x.GUID == payload, i);
                }
                ImGui.EndDragDropTarget();
            }
        }

        private static void MoveItemToPosition<T>(List<T> list, Func<T, bool> match, int newIndex)
        {
            var item = list.FirstOrDefault(match);
            if (item == null) return;

            int oldIndex = list.IndexOf(item);
            if (oldIndex == -1 || newIndex < 0 || newIndex > list.Count) return;

            list.RemoveAt(oldIndex);

            // Adjust target index if removing from earlier in list
            if (oldIndex < newIndex) newIndex--;

            // Clamp insert position to avoid out-of-bounds crash
            newIndex = Math.Clamp(newIndex, 0, list.Count);
            list.Insert(newIndex, item);
        }


        public static void AcceptDragDropGeneric<T>(List<T> list, Func<T, bool> match, int newIndex, string payloadName)
        {
            if (ImGui.BeginDragDropTarget())
            {
                if (ImGuiDragDrop.AcceptDragDropPayload(payloadName, out var payload, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                {
                    MoveItemToPosition(list, match, newIndex);
                }
                ImGui.EndDragDropTarget();
            }
        }
    }
}
