using BoardGames.Core;

namespace BoardGames.Tests.Core;

public class BoardTests
{
    private static readonly Player X = new HumanPlayer(0, 'X', "P1");
    private static readonly Player O = new HumanPlayer(1, 'O', "P2");

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(3, 4, true)]
    [InlineData(0, 1, false)]
    [InlineData(4, 1, false)]
    public void Positions_are_one_based_and_bounded(int row, int col, bool expected)
    {
        var board = new Board(3, 4);
        Assert.Equal(expected, board.InBounds(new Position(row, col)));
    }

    [Fact]
    public void Apply_sets_and_clears_cells_and_raises_one_event_per_call()
    {
        var board = new Board(3, 3);
        var events = new List<BoardChangedEventArgs>();
        board.BoardChanged += (_, e) => events.Add(e);
        var piece = new Piece(X, PieceKind.Disk);

        board.Apply(new[]   // a three-cell change, like a Reversi flip, is ONE event
        {
            new CellChange(new Position(1, 1), null, piece),
            new CellChange(new Position(1, 2), null, piece),
            new CellChange(new Position(1, 3), null, new Piece(O, PieceKind.Disk)),
        });
        Assert.Single(events);
        Assert.Equal(3, events[0].Changes.Count);
        Assert.Equal(piece, board[new Position(1, 2)]);

        board.Apply(new[] { new CellChange(new Position(1, 2), piece, null) });
        Assert.Equal(2, events.Count);
        Assert.Null(board[new Position(1, 2)]);
    }

    [Fact]
    public void An_off_board_change_is_rejected_and_nothing_changes()
    {
        var board = new Board(3, 3);
        int events = 0;
        board.BoardChanged += (_, _) => events++;

        Assert.Throws<ArgumentOutOfRangeException>(() => board.Apply(new[]
        {
            new CellChange(new Position(1, 1), null, new Piece(X, PieceKind.Ordinary)),
            new CellChange(new Position(9, 9), null, new Piece(X, PieceKind.Ordinary)),
        }));

        Assert.Null(board[new Position(1, 1)]);   // all or nothing
        Assert.Equal(0, events);
    }

    [Fact]
    public void A_change_that_does_not_match_the_board_is_rejected()   // catches an Undo that is not the exact inverse
    {
        var board = new Board(3, 3);
        var wrongBefore = new Piece(X, PieceKind.Ordinary);

        Assert.Throws<InvalidOperationException>(() =>
            board.Apply(new[] { new CellChange(new Position(1, 1), wrongBefore, null) }));
    }
}
