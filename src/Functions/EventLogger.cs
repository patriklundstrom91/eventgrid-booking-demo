using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace functions;

public static class EventLogger
{
    private static readonly string LogPath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "events.json"));

    public static void LogEvent(object evt)
    {
        List<object> events;

        if (File.Exists(LogPath))
        {
            var json = File.ReadAllText(LogPath);
            events = JsonSerializer.Deserialize<List<object>>(json)
                     ?? new List<object>();
        }
        else
        {
            events = new List<object>();
        }

        events.Add(evt);

        File.WriteAllText(LogPath,
            JsonSerializer.Serialize(events, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
    }
}
