using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Balance
{
    /// <summary>
    /// Centralised developer console for tracking gameplay events and balance probes.
    /// </summary>
    public static class EventConsole
    {
        private const int MaxEntries = 200;
        private static readonly List<EventConsoleEntry> entries = new List<EventConsoleEntry>();

        public static event Action<EventConsoleEntry> EntryAdded;

        public static IReadOnlyList<EventConsoleEntry> Entries => entries;

        public static void Log(string category, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var entry = new EventConsoleEntry(category ?? "General", message, Time.time, DateTime.Now);
            entries.Add(entry);
            if (entries.Count > MaxEntries)
                entries.RemoveAt(0);
            EntryAdded?.Invoke(entry);
            EventLogUI.AddEntry($"<b>[{entry.Category}]</b> {entry.Message}");
            Debug.Log($"[EventConsole] {entry.Category}: {entry.Message}");
        }
    }

    /// <summary>
    /// Data snapshot for a single console line.
    /// </summary>
    public readonly struct EventConsoleEntry
    {
        public readonly string Category;
        public readonly string Message;
        public readonly float GameTime;
        public readonly DateTime LocalTime;

        public EventConsoleEntry(string category, string message, float gameTime, DateTime localTime)
        {
            Category = category;
            Message = message;
            GameTime = gameTime;
            LocalTime = localTime;
        }
    }
}
