using Microsoft.AspNetCore.SignalR;
using TicTacToeBackend.Models;
using System.Collections.Concurrent;

namespace TicTacToeBackend.Hubs;

public class GameHub : Hub
{
    private readonly ConcurrentDictionary<Guid, Game> _games;

    // Inject our in-memory "database"
    public GameHub(ConcurrentDictionary<Guid, Game> games)
    {
        _games = games;
    }

    // 1. Player connects and joins a specific game "room"
    public async Task JoinGame(Guid gameId)
    {
        // Add this specific WebSocket connection to a group named after the Game ID
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
        
        if (_games.TryGetValue(gameId, out var game))
        {
            // Instantly push the current game state back to the player who just joined
            await Clients.Caller.SendAsync("GameStateUpdated", game);
        }
    }

    // 2. Player makes a move via WebSocket instead of HTTP POST
    public async Task MakeMove(Guid gameId, int position)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            // If the move is valid, broadcast the new state to EVERYONE in the room
            if (game.MakeMove(position))
            {
                await Clients.Group(gameId.ToString()).SendAsync("GameStateUpdated", game);
            }
        }
    }
}