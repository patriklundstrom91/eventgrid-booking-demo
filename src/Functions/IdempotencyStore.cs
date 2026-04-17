
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace functions;

public static class IdempotencyStore
{
    private static readonly string Path =
        System.IO.Path.GetFullPath(System.IO.Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "processedEvents.json"));
    public static bool HasProcessed(string eventId)
    {
        if (!File.Exists(Path)) return false;

        var json = File.ReadAllText(Path);
        var list = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
        return list.Contains(eventId);
    }

    public static void MarkProcessed(string eventId)
    {
        HashSet<string> list;
        if (File.Exists(Path))
        {
            var json = File.ReadAllText(Path);
            list = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
        }
        else
        {
            list = new HashSet<string>();
        }
        list.Add(eventId);

        File.WriteAllText(Path, JsonSerializer.Serialize(list, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}