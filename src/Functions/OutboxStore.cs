using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Domain.Models;

namespace functions;

public static class OutboxStore
{
    private static readonly object _lock = new();
    private static readonly string Path =
        System.IO.Path.GetFullPath(System.IO.Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "outbox.json"
        ));
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new OutboxItemJsonConverter() }
    };
    public static void Add(OutboxItem item)
    {
        lock (_lock)
        {
            var list = GetAll();

            list.Add(item);
            File.WriteAllText(Path, JsonSerializer.Serialize(list, Options));
            System.Console.WriteLine($"OUTBOX ADD PATH = {Path}");
            System.Console.WriteLine($"OUTBOX ADD BODY = {JsonSerializer.Serialize(item.Body)}");
        }
    }
    public static List<OutboxItem> GetAll()
    {
        lock (_lock)
        {
            if (!File.Exists(Path)) return new List<OutboxItem>();

            var json = File.ReadAllText(Path);
            return JsonSerializer.Deserialize<List<OutboxItem>>(json, Options) ?? new List<OutboxItem>();
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            File.WriteAllText(Path, "[]");
        }
    }
}