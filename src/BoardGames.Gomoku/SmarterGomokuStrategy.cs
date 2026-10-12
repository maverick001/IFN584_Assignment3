using BoardGames.Core;

namespace BoardGames.Gomoku;

// Reads the true board (also in Fog) and returns an existing legal input.
// Heavy and Ordinary stones both contribute to alignment; Eraser simulations
// remove the target without changing the live board or either inventory.
public sealed class SmarterGomokuStrategy : IComputerStrategy
{
    private static readonly (int Row, int Col)[] Lines = [(1, 0), (0, 1), (1, 1), (1, -1)];

    public PlaceInput Choose(GameState state, Player me, IReadOnlyList<PlaceInput> legal)
    {
        if (legal.Count == 0) throw new ArgumentException("A strategy needs legal placements.", nameof(legal));
        Board board = state.Board;
        Player opponent = state.Players.Single(p => p != me);
        var candidates = legal.OrderBy(m => m.Code == "O" ? 0 : m.Code == "H" ? 1 : 2).ToList();

        foreach (PlaceInput move in candidates)
            if (move.Code is "O" or "H" && LongestLine(board, me, move.At, me, move) >= 5)
                return move;

        int threats = WinningCells(board, opponent, me, null);
        if (threats > 0)
        {
            var best = candidates.Select(m => (Move: m, Remaining: WinningCells(board, opponent, me, m)))
                .OrderBy(x => x.Remaining).First();
            if (best.Remaining < threats) return best.Move;
        }

        // With no urgent win or block, extend our longest line near the centre.
        var placements = candidates.Where(m => m.Code is "O" or "H").ToList();
        if (placements.Count == 0) return candidates[0];
        return placements.OrderByDescending(m => LongestLine(board, me, m.At, me, m))
            .ThenBy(m => Math.Abs(2 * m.At.Row - board.Rows - 1) + Math.Abs(2 * m.At.Col - board.Cols - 1))
            .First();
    }

    private static int WinningCells(Board board, Player owner, Player actor, PlaceInput? simulated) =>
        board.AllPositions().Count(at => OwnerAt(board, at, actor, simulated) == null
            && LongestLine(board, owner, at, actor, simulated) >= 5);

    private static int LongestLine(Board board, Player owner, Position at, Player actor, PlaceInput? simulated)
    {
        int longest = 1;
        foreach (var (dr, dc) in Lines)
        {
            int length = 1;
            foreach (int sign in new[] { -1, 1 })
            {
                var p = new Position(at.Row + sign * dr, at.Col + sign * dc);
                while (board.InBounds(p) && OwnerAt(board, p, actor, simulated) == owner)
                {
                    length++;
                    p = new Position(p.Row + sign * dr, p.Col + sign * dc);
                }
            }
            longest = Math.Max(longest, length);
        }
        return longest;
    }

    private static Player? OwnerAt(Board board, Position at, Player actor, PlaceInput? simulated) =>
        simulated?.At == at ? (simulated.Code == "E" ? null : actor) : board[at]?.Owner;
}
