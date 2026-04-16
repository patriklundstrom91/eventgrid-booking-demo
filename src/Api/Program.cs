using Infrastructure;
using Domain.Models;
using Domain.Events;
using Scalar.AspNetCore;
using System.Text.Json.Schema;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddSingleton(sp =>
{
    return new EventGridPublisher();
});

var corsPolicy = "_allowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

app.UseCors(corsPolicy);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapPost("/bookings", async (BookingRequest request, EventGridPublisher publisher) =>
{
    var evt = new BookingCreatedEvent(
        BookingId: Guid.NewGuid(),
        CustomerName: request.CustomerName,
        CreatedAt: DateTime.UtcNow
    );

    await publisher.PublishBookingCreatedAsync(evt);

    return Results.Ok(new { Message = "Booking Created", evt.BookingId });
});

app.MapGet("/api/events", () =>
{

    var path = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "events.json")
    );

    if (!File.Exists(path))
        return Results.Ok(new List<object>());

    var json = File.ReadAllText(path);
    return Results.Ok(JsonSerializer.Deserialize<List<object>>(json));
});

app.Run();
