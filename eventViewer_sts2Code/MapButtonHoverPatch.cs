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

        // Use the filtered "available" list instead of the full pool
        var availableEvents = MainFile.GetAvailableEvents();
        if (availableEvents == null || availableEvents.Count == 0)
        {
            hoverTips.Add(new HoverTip(
                new LocString("static_hover_tips", "EVENT_VIEWER-NO_EVENTS.title"),
                new LocString("static_hover_tips", "EVENT_VIEWER-NO_EVENTS.description")
            ));
            return hoverTips;
        }

        var eventListText = new System.Text.StringBuilder();

        var odds = MainFile.GetUnknownMapPointOdds();
        if (odds != null)
        {
            eventListText.AppendLine(
                $"[color=#66ff66]Event[/color] " +
                $"[color=#ffffff]{odds.EventOdds * 100f:F0}%[/color]\n" + 
                $"[color=#ff5555]Combat[/color] {odds.MonsterOdds * 100f:F0}%\n" +
                $"[color=#5599ff]Shop[/color] {odds.ShopOdds * 100f:F0}%\n" +
                $"[color=#ffcc44]Chest[/color] {odds.TreasureOdds * 100f:F0}%");
            eventListText.AppendLine();
        }
        
        eventListText.AppendLine(
            $"[color=#66ff66]Available Events:[/color] {availableEvents.Count}");

        for (int i = 0; i < availableEvents.Count; i++)
        {
            var eventModel = availableEvents[i];
            string displayName = MainFile.GetEventDisplayName(eventModel);

            eventListText.AppendLine($"  [color=#4a8a4a]{displayName}[/color]");
        }

        var desc = new LocString("static_hover_tips", "EVENT_VIEWER-POOL.description");
        desc.Add("EventList", eventListText.ToString());

        hoverTips.Add(new HoverTip(
            new LocString("static_hover_tips", "EVENT_VIEWER-POOL.title"),
            desc
        ));

        return hoverTips;
    }
}