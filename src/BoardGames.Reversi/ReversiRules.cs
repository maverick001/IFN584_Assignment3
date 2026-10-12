using BoardGames.Core;

namespace BoardGames.Reversi;

// One source of truth for validation, command construction and AI simulation.
public static class ReversiRules
{
    private static readonly (int Row, int Col)[] Directions =
        [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)];

    public static IReadOnlyList<Position> GetFlips(Board board, Player actor, Position at)
    {
        if (!board.InBounds(at) || board[at] != null) return Array.Empty<Position>();
        var flips = new List<Position>();
        foreach (var (dr, dc) in Directions)
        {
            var line = new List<Position>();
            var next = new Position(at.Row + dr, at.Col + dc);
            while (board.InBounds(next) && board[next] is { } disk && disk.Owner != actor)
            {
                line.Add(next);
                next = new Position(next.Row + dr, next.Col + dc);
            }
            if (line.Count > 0 && board.InBounds(next) && board[next]?.Owner == actor)
                flips.AddRange(line);
        }
        return flips;
    }

    public static IReadOnlyList<PlaceInput> GetLegalMoves(Board board, Player actor) =>
        board.AllPositions().Where(at => GetFlips(board, actor, at).Count > 0)
            .Select(at => new PlaceInput("P", at)).ToList();

    public static bool IsCorner(Board board, Position at) =>
        (at.Row == 1 || at.Row == board.Rows) && (at.Col == 1 || at.Col == board.Cols);

    public static int CountDisks(Board board, Player actor) =>
        board.AllPositions().Count(at => board[at]?.Owner == actor);

    public static int CountCorners(Board board, Player actor) =>
        board.AllPositions().Count(at => IsCorner(board, at) && board[at]?.Owner == actor);

    // Simulation uses a separate board: no changes or events on the live game.
    internal static Board AfterMove(Board source, Player actor, Position at)
    {
        var copy = new Board(source.Rows, source.Cols);
        copy.Apply(source.AllPositions().Where(p => source[p] != null)
            .Select(p => new CellChange(p, null, source[p])));
        var changes = GetFlips(copy, actor, at)
            .Select(p => new CellChange(p, copy[p], new Piece(actor, PieceKind.Disk))).ToList();
        changes.Add(new CellChange(at, null, new Piece(actor, PieceKind.Disk)));
        copy.Apply(changes);
        return copy;
    }
}
