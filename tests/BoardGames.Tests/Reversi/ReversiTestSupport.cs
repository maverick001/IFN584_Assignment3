using BoardGames.App;
using BoardGames.Core;

namespace BoardGames.Tests.Reversi;

internal static class ReversiTestSupport
{
    internal static Game NewGame(string variant = "standard", GameMode mode = GameMode.HvH, AiLevel level = AiLevel.Smarter)
    {
        Game game = CatalogSetup.Build().Resolve("reversi", variant).CreateGame(new GameSetup(mode, level));
        game.View.AutoRedraw = false;
        return game;
    }

    internal static void SetBoard(Game game, params string[] rows)
    {
        Assert.Equal(8, rows.Length);
        Assert.All(rows, row => Assert.Equal(8, row.Length));
        game.State.Board.Apply(game.State.Board.AllPositions().Select(p =>
            new CellChange(p, game.State.Board[p], rows[p.Row - 1][p.Col - 1] switch
            {
                'X' => new Piece(game.State.Players[0], PieceKind.Disk),
                'O' => new Piece(game.State.Players[1], PieceKind.Disk),
                _ => null,
            })));
    }

    // Independent reference: find a friendly endpoint aligned with an empty
    // origin, then require every intermediate cell to belong to the opponent.
    // This avoids using the production outward-ray scanner as the test oracle.
    internal static IReadOnlyList<Position> OracleLegal(Board board, Player player)
    {
        var legal = new List<Position>();
        foreach (Position start in board.AllPositions().Where(p => board[p] == null))
        foreach (Position end in board.AllPositions().Where(p => board[p]?.Owner == player))
        {
            int dr = end.Row - start.Row, dc = end.Col - start.Col;
            int distance = Math.Max(Math.Abs(dr), Math.Abs(dc));
            if (distance < 2 || !(dr == 0 || dc == 0 || Math.Abs(dr) == Math.Abs(dc))) continue;
            bool flanked = Enumerable.Range(1, distance - 1).All(i =>
            {
                Piece? disk = board[new Position(start.Row + i * Math.Sign(dr), start.Col + i * Math.Sign(dc))];
                return disk != null && disk.Owner != player;
            });
            if (flanked) { legal.Add(start); break; }
        }
        return legal;
    }
}
