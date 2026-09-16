using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using System.Collections.Generic;

namespace eventViewer_sts2.eventViewer_sts2Code;

[HarmonyPatch(typeof(NTopBarMapButton))]
public static class MapButtonHoverPatch
{
    [HarmonyPatch("OnFocus")]
    [HarmonyPostfix]
    public static void OnFocusPostfix(NTopBarMapButton __instance)
    {
        try
        {
            NHoverTipSet.Remove(__instance);

            var hoverTips = BuildEventPoolHoverTips();
            if (hoverTips.Count == 0)
                return;

            var tipSet = NHoverTipSet.CreateAndShow(__instance, hoverTips);
            tipSet?.SetGlobalPosition(
                __instance.GlobalPosition +
                new Vector2(__instance.Size.X - tipSet.Size.X, __instance.Size.Y + 20f));
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"Error in NTopBarMapButton.OnFocus postfix: {ex.Message}");
        }
    }

    private static List<IHoverTip> BuildEventPoolHoverTips()
    {
        var hoverTips = new List<IHoverTip>();

        var eventPool = MainFile.GetCurrentEventPool();
        if (eventPool == null || eventPool.Count == 0)
        {
            hoverTips.Add(new HoverTip(
                new LocString("static_hover_tips", "EVENT_VIEWER-NO_EVENTS.title"),
                new LocString("static_hover_tips", "EVENT_VIEWER-NO_EVENTS.description")
            ));
            return hoverTips;
        }

        var eventListText = new System.Text.StringBuilder();
        var currentIndex = MainFile.GetCurrentEventIndex();
        var eventsVisited = MainFile.GetEventsVisited();

        eventListText.AppendLine(
            $"[color=#8888ff]Pool Size:[/color] {eventPool.Count}    " +
            $"[color=#8888ff]Visited:[/color] {eventsVisited}");
        eventListText.AppendLine();

        for (int i = 0; i < eventPool.Count; i++)
        {
            var eventModel = eventPool[i];
            string status = MainFile.GetEventStatus(eventModel);
            string prefix = (i == currentIndex) ? "[color=#ffcc00]→[/color] " : "  ";
            eventListText.AppendLine(
                $"{prefix}[color=#88ccff]{eventModel.Id.Entry}[/color]{status}");
        }

        eventListText.AppendLine();
        eventListText.AppendLine(
            "[color=#666666]→ = next event  •  ALLOWED / BLOCKED / SEEN[/color]");

        var desc = new LocString("static_hover_tips", "EVENT_VIEWER-POOL.description");
        desc.Add("EventList", eventListText.ToString());

        hoverTips.Add(new HoverTip(
            new LocString("static_hover_tips", "EVENT_VIEWER-POOL.title"),
            desc
        ));

        return hoverTips;
    }
}