using Infrastructure;
using Domain.Models;
using Domain.Events;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(sp =>
{
    // var endpoint = builder.Configuration["EventGrid:Endpoint"];
    // var key = builder.Configuration["EventGrid:Key"];
    return new EventGridPublisher();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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


app.Run();

