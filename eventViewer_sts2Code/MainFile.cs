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

    public static List<MegaCrit.Sts2.Core.Models.EventModel>? GetCurrentEventPool()
    {
        try
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
        catch (System.Exception ex)
        {
            Logger.Error($"Error getting event pool: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Returns only events that can actually be encountered:
    /// - pass the IsAllowed check
    /// - haven't already been visited
    /// </summary>
    public static List<MegaCrit.Sts2.Core.Models.EventModel>? GetAvailableEvents()
    {
        try
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
        catch (System.Exception ex)
        {
            Logger.Error($"Error getting available events: {ex.Message}");
            return null;
        }
    }

    public static int GetCurrentEventIndex()
    {
        try
        {
            var runState = RunManager.Instance?.DebugOnlyGetState();
            if (runState == null) return 0;

            var act = runState.Act;
            if (act == null) return 0;

            var roomsField = typeof(MegaCrit.Sts2.Core.Models.ActModel).GetField("_rooms",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var rooms = roomsField?.GetValue(act) as MegaCrit.Sts2.Core.Rooms.RoomSet;
            if (rooms == null || rooms.events.Count == 0) return 0;

            return rooms.eventsVisited % rooms.events.Count;
        }
        catch { return 0; }
    }

    public static int GetEventsVisited()
    {
        try
        {
            var runState = RunManager.Instance?.DebugOnlyGetState();
            if (runState == null) return 0;

            var act = runState.Act;
            if (act == null) return 0;

            var roomsField = typeof(MegaCrit.Sts2.Core.Models.ActModel).GetField("_rooms",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var rooms = roomsField?.GetValue(act) as MegaCrit.Sts2.Core.Rooms.RoomSet;
            if (rooms == null) return 0;

            return rooms.eventsVisited;
        }
        catch { return 0; }
    }

    /// <summary>
    /// Returns the localized display name of an event (e.g. "The Slippery Bridge")
    /// or falls back to the ID if the title is missing.
    /// </summary>
    public static string GetEventDisplayName(MegaCrit.Sts2.Core.Models.EventModel eventModel)
    {
        try
        {
            var title = eventModel.Title;
            if (title != null)
            {
                var text = title.GetFormattedText();
                if (!string.IsNullOrEmpty(text))
                    return text;
            }
        }
        catch { /* fall through */ }

        return eventModel.Id.Entry;
    }

    public static string GetEventStatus(MegaCrit.Sts2.Core.Models.EventModel eventModel)
    {
        try
        {
            var runState = RunManager.Instance?.DebugOnlyGetState();
            if (runState == null) return "";

            string status = "";
            var currentIndex = GetCurrentEventIndex();
            var pool = GetCurrentEventPool();

            if (pool != null && pool.IndexOf(eventModel) == currentIndex)
                status += " [color=#ffcc00]→ NEXT[/color]";

            if (eventModel.IsAllowed(runState))
                status += " [color=#66ff66]ALLOWED[/color]";
            else
                status += " [color=#ff6666]BLOCKED[/color]";

            if (runState.VisitedEventIds.Contains(eventModel.Id))
                status += " [color=#6666ff]SEEN[/color]";

            return status;
        }
        catch { return ""; }
    }
    
    /// <summary>
    /// Walks the pool the same way RoomSet.EnsureNextEventIsValid() does,
    /// returning the actual next event the game will pull.
    /// </summary>
    public static MegaCrit.Sts2.Core.Models.EventModel? GetNextEvent()
    {
        try
        {
            var runState = RunManager.Instance?.DebugOnlyGetState();
            if (runState == null) return null;

            var act = runState.Act;
            if (act == null) return null;

            var roomsField = typeof(MegaCrit.Sts2.Core.Models.ActModel).GetField("_rooms",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var rooms = roomsField?.GetValue(act) as MegaCrit.Sts2.Core.Rooms.RoomSet;
            if (rooms == null || rooms.events.Count == 0) return null;

            int visited = rooms.eventsVisited;
            int count = rooms.events.Count;

            // Walk forward from eventsVisited, skipping blocked/seen events
            for (int i = 0; i < count; i++)
            {
                var candidate = rooms.events[(visited + i) % count];
                if (candidate.IsAllowed(runState) && !runState.VisitedEventIds.Contains(candidate.Id))
                    return candidate;
            }

            return null; // All events exhausted
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Error finding next event: {ex.Message}");
            return null;
        }
    }
}