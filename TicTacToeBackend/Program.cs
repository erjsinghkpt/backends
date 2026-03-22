using System.Collections.Concurrent;
using TicTacToeBackend.Hubs;
using TicTacToeBackend.Models;

var builder = WebApplication.CreateBuilder(args);
// 1. Add CORS (so our test HTML file can connect)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true) // Open for testing
              .AllowCredentials();
    });
});

// 1. Register our "Database" as a Singleton in the DI Container
builder.Services.AddSingleton<ConcurrentDictionary<Guid, Game>>();
builder.Services.AddSignalR(); // 2. Add SignalR to the DI Container

var app = builder.Build();
app.UseCors(); // 3. Enable CORS
// 2. Define Endpoints
var games = app.Services.GetRequiredService<ConcurrentDictionary<Guid, Game>>();

// Create a new game
app.MapPost("/games", () =>
{
    var game = new Game();
    games.TryAdd(game.Id, game);
    return Results.Created($"/games/{game.Id}", game);
});

// Get game state
app.MapGet("/games/{id:guid}", (Guid id) =>
{
    return games.TryGetValue(id, out var game) 
        ? Results.Ok(game) 
        : Results.NotFound("Game not found.");
});

// Make a move
app.MapPost("/games/{id:guid}/move", (Guid id, MoveRequest request) =>
{
    if (!games.TryGetValue(id, out var game))
        return Results.NotFound("Game not found.");

    if (!game.MakeMove(request.Position))
        return Results.BadRequest("Invalid move or game is over.");

    return Results.Ok(game);
});
// Add this line right here!
app.MapHub<GameHub>("/gamehub");
app.Run();