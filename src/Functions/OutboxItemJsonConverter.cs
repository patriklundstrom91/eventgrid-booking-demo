using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Domain.Models;

public class OutboxItemJsonConverter : JsonConverter<OutboxItem>
{
    public override OutboxItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);

        var root = doc.RootElement;
        var functionName = root.GetProperty("FunctionName").GetString();
        var bodyElement = root.GetProperty("Body");
        object body = bodyElement.Clone();
        return new OutboxItem(functionName!, body);
    }
    public override void Write(Utf8JsonWriter writer, OutboxItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("FunctionName", value.FunctionName);

        writer.WritePropertyName("Body");
        JsonSerializer.Serialize(writer, value.Body, options);
        writer.WriteEndObject();
    }
}