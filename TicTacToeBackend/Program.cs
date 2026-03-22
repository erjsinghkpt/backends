using System.Collections.Concurrent;
using TicTacToeBackend.Hubs;
using TicTacToeBackend.Models;

// 1. THE HOST BUILDER PHASE
// WebApplication.CreateBuilder does a massive amount of heavy lifting behind the scenes.
// It initializes the Kestrel web server, loads appsettings.json, sets up environment variables,
// and prepares the IServiceCollection (the DI container) before the app is built.
var builder = WebApplication.CreateBuilder(args);

// 2. SERVICE REGISTRATION (Configuring the DI Container)
// CORS is notoriously tricky with SignalR. Because WebSockets require an initial HTTP handshake 
// that passes connection IDs and potentially auth cookies, the browser enforces strict CORS rules.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              // Under normal circumstances, you'd use AllowAnyOrigin(). However, the W3C spec 
              // strictly forbids using AllowAnyOrigin combined with AllowCredentials. 
              // SetIsOriginAllowed(_ => true) is a clever runtime bypass for local development.
              .SetIsOriginAllowed(_ => true) 
              // AllowCredentials is required by SignalR's /negotiate endpoint.
              .AllowCredentials(); 
    });
});

// We register the ConcurrentDictionary as a Singleton. 
// A Singleton is instantiated exactly once per application lifetime. 
// Because .NET handles multiple concurrent HTTP requests on a ThreadPool, standard Dictionaries 
// will throw exceptions or corrupt data if mutated simultaneously. ConcurrentDictionary uses 
// fine-grained locking (lock striping) to ensure thread-safe reads and writes across multiple players.
builder.Services.AddSingleton<ConcurrentDictionary<Guid, Game>>();

// AddSignalR injects the underlying WebSocket managers, the JSON protocol serializers, 
// and the HubContext into the DI container so they can be resolved later.
builder.Services.AddSignalR(); 


// 3. THE PIPELINE BUILD PHASE
// builder.Build() locks the IServiceCollection. You can no longer add new services to DI after this.
// It returns the 'app' object, which is used to configure the HTTP Request Pipeline (Middleware).
var app = builder.Build();

// 4. MIDDLEWARE PIPELINE (Order is absolute!)
// ASP.NET Core middleware executes in the exact order it is added. 
// UseCors MUST be called before any endpoints are mapped. If a request reaches an endpoint 
// before passing through the CORS middleware, the browser's preflight OPTIONS request will be rejected.
app.UseCors(); 


// 5. ENDPOINT ROUTING (Minimal APIs)
// We resolve our Singleton "database" directly from the locked DI container (app.Services).
var games = app.Services.GetRequiredService<ConcurrentDictionary<Guid, Game>>();

// Minimal APIs (app.MapPost, app.MapGet) bypass the heavy allocation overhead of traditional 
// MVC Controllers. Under the hood, .NET 8 uses Source Generators and expression trees to compile 
// these lambdas into highly optimized request delegates at startup.
app.MapPost("/games" , (ILogger<Program> logger) =>
{
    var game = new Game();
    // TryAdd is atomic. It safely handles the microscopic chance of a Guid collision.
    games.TryAdd(game.Id, game);
    logger.LogInformation("🆕 REST: Created new game with ID: {GameId}", game.Id);
    // Results.Created returns an implementation of IResult. 
    // It automatically formats the HTTP 201 response and serializes the 'game' object to JSON.
    return Results.Created($"/games/{game.Id}", game);
});

// The {id:guid} syntax is an inline route constraint. If a user passes a string that isn't a valid GUID, 
// the routing engine short-circuits and returns a 404 before this code block even executes, saving CPU cycles.
app.MapGet("/games/{id:guid}", (Guid id, ILogger<Program> logger) =>
{
    if (games.TryGetValue(id, out var game))
    {
        logger.LogInformation("👀 REST: Fetched state for game ID: {GameId}", id);
        return Results.Ok(game);
    }
    
    logger.LogWarning("⚠️ REST: Attempted to fetch missing game ID: {GameId}", id);
    return Results.NotFound("Game not found.");
});

// The .NET model binder automatically deserializes the incoming JSON body into the 'MoveRequest' record.
app.MapPost("/games/{id:guid}/move", (Guid id, MoveRequest request, ILogger<Program> logger) =>
{
    if (!games.TryGetValue(id, out var game))
    {
        logger.LogWarning("⚠️ REST: Move attempted on missing game ID: {GameId}", id);
        return Results.NotFound("Game not found.");
    }

    if (!game.MakeMove(request.Position))
    {
        logger.LogWarning("❌ REST: Invalid move at position {Position} for game ID: {GameId}. Status: {Status}", request.Position, id, game.Status);
        return Results.BadRequest("Invalid move or game is over.");
    }
    
    logger.LogInformation("✅ REST: Valid move at position {Position} for game ID: {GameId}. Next turn: {CurrentTurn}", request.Position, id, game.CurrentTurn);
    return Results.Ok(game);
});

// 6. PROTOCOL UPGRADE ROUTING
// MapHub hooks into the endpoint routing system. When a client requests /gamehub, 
// the server intercepts it. If the client requests an upgrade to WebSockets (via the "Connection: Upgrade" header),
// Kestrel drops the standard HTTP context and transitions the TCP socket into a persistent, bidirectional stream.
app.MapHub<GameHub>("/gamehub");

// 💡 INJECTED: A final startup log before the blocking Run() call
app.Logger.LogInformation("🚀 Tic-Tac-Toe Backend is starting up and ready for connections!");

// 7. EXECUTION
// app.Run() blocks the main thread and starts the Kestrel web server listening on the configured ports 
// (defaulting to 5000/5001 or whatever is defined in launchSettings.json).
app.Run();