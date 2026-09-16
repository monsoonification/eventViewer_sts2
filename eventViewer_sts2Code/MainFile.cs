using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;

namespace eventViewer_sts2.eventViewer_sts2Code;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "eventViewer_sts2";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        Logger.Info("Event Viewer mod initialized!");
    }

    /// <summary>
    /// Returns the current UnknownMapPointOdds, which tell us the chance
    /// of rolling each room type when entering a "?" map node.
    /// </summary>
    public static MegaCrit.Sts2.Core.Odds.UnknownMapPointOdds? GetUnknownMapPointOdds()
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null) return null;

        return runState.Odds?.UnknownMapPoint;
    }

    /// <summary>
    /// Returns the full event pool for the current act, in queue order.
    /// </summary>
    public static List<MegaCrit.Sts2.Core.Models.EventModel>? GetCurrentEventPool()
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null) return null;

        var act = runState.Act;
        if (act == null) return null;

        var roomsField = typeof(MegaCrit.Sts2.Core.Models.ActModel).GetField("_rooms",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var rooms = roomsField?.GetValue(act) as MegaCrit.Sts2.Core.Rooms.RoomSet;
        if (rooms == null) return null;

        return rooms.events;
    }

    /// <summary>
    /// Returns only events that can actually be encountered:
    /// - pass the IsAllowed check
    /// - haven't already been visited
    /// </summary>
    public static List<MegaCrit.Sts2.Core.Models.EventModel>? GetAvailableEvents()
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null) return null;

        var pool = GetCurrentEventPool();
        if (pool == null) return null;

        var available = new List<MegaCrit.Sts2.Core.Models.EventModel>();
        foreach (var eventModel in pool)
        {
            if (!eventModel.IsAllowed(runState))
                continue;
            if (runState.VisitedEventIds.Contains(eventModel.Id))
                continue;
            available.Add(eventModel);
        }
        return available;
    }

    /// <summary>
    /// Returns the localized display name of an event (e.g. "The Slippery Bridge")
    /// or falls back to the ID if the title is missing.
    /// </summary>
    public static string GetEventDisplayName(MegaCrit.Sts2.Core.Models.EventModel eventModel)
    {
        var title = eventModel.Title;
        if (title != null)
        {
            var text = title.GetFormattedText();
            if (!string.IsNullOrEmpty(text))
                return text;
        }
        return eventModel.Id.Entry;
    }

    // /// <summary>
    // /// Walks the pool the same way RoomSet.EnsureNextEventIsValid() does,
    // /// I dont think this works but it might be useful later.
    // /// </summary>
    // public static MegaCrit.Sts2.Core.Models.EventModel? GetNextEvent()
    // {
    //     var runState = RunManager.Instance?.DebugOnlyGetState();
    //     if (runState == null) return null;
    //
    //     var act = runState.Act;
    //     if (act == null) return null;
    //
    //     var roomsField = typeof(MegaCrit.Sts2.Core.Models.ActModel).GetField("_rooms",
    //         BindingFlags.NonPublic | BindingFlags.Instance);
    //     var rooms = roomsField?.GetValue(act) as MegaCrit.Sts2.Core.Rooms.RoomSet;
    //     if (rooms == null || rooms.events.Count == 0) return null;
    //
    //     int visited = rooms.eventsVisited;
    //     int count = rooms.events.Count;
    //
    //     for (int i = 0; i < count; i++)
    //     {
    //         var candidate = rooms.events[(visited + i) % count];
    //         if (candidate.IsAllowed(runState) && !runState.VisitedEventIds.Contains(candidate.Id))
    //             return candidate;
    //     }
    //
    //     return null; 
    // }
}