namespace TicTacToeBackend.Models;
public class Game
{
    public Guid Id { get; } = Guid.NewGuid();
    public char[] Board { get; private set; } = new char[9]; // Empty spots are '\0'
    public char CurrentTurn { get; private set; } = 'X';
    public string Status { get; private set; } = "InProgress"; // InProgress, XWins, OWins, Draw

    public bool MakeMove(int position)
    {
        if (Status != "InProgress" || position < 0 || position > 8 || Board[position] != '\0')
            return false;

        Board[position] = CurrentTurn;
        CheckWinOrDraw();
        
        if (Status == "InProgress")
            CurrentTurn = CurrentTurn == 'X' ? 'O' : 'X';

        return true;
    }

    private void CheckWinOrDraw()
    {
        int[][] winConditions = {
            new[] {0, 1, 2}, new[] {3, 4, 5}, new[] {6, 7, 8}, // Rows
            new[] {0, 3, 6}, new[] {1, 4, 7}, new[] {2, 5, 8}, // Cols
            new[] {0, 4, 8}, new[] {2, 4, 6}                   // Diagonals
        };

        foreach (var condition in winConditions)
        {
            if (Board[condition[0]] != '\0' &&
                Board[condition[0]] == Board[condition[1]] &&
                Board[condition[1]] == Board[condition[2]])
            {
                Status = $"{Board[condition[0]]}Wins";
                return;
            }
        }

        if (!Board.Contains('\0')) Status = "Draw";
    }
}

// Data Transfer Object for incoming requests
public record MoveRequest(int Position);