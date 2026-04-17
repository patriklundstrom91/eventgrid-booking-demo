using System;
using System.Text.Json;

namespace functions;

public static class OutboxStore
{
    private static readonly string Path =
        System.IO.Path.GetFullPath(System.IO.Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "outbox.json"
        ));

    public static void Add(object evt)
    {

    }
}