using BoardGames.App;
using BoardGames.Core;
using BoardGames.Gomoku;

namespace BoardGames.Tests.Gomoku;

public class SmarterGomokuTests
{
    private static Game NewGame()
    {
        Game game = CatalogSetup.Build().Resolve("gomoku", "plus").CreateGame(new GameSetup(GameMode.HvH, AiLevel.Smarter));
        game.View.AutoRedraw = false;
        return game;
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(1, -1)]
    public void Immediate_wins_are_detected_in_all_four_alignments(int dr, int dc)
    {
        Game game = NewGame();
        Player me = game.State.Current;
        game.State.Board.Apply(Enumerable.Range(0, 4).Select(i =>
            new CellChange(new Position(3 + i * dr, 6 + i * dc), null, new Piece(me, i == 2 ? PieceKind.Heavy : PieceKind.Ordinary))));
        var legal = game.GetLegalMoves(me);
        PlaceInput move = new SmarterGomokuStrategy().Choose(game.State, me, legal);
        Assert.Contains(move, legal);
        Assert.Contains(move.At, new[] { new Position(3 - dr, 6 - dc), new Position(3 + 4 * dr, 6 + 4 * dc) });
    }

    [Fact]
    public void An_opponent_four_at_the_edge_is_blocked()
    {
        Game game = NewGame();
        Player opponent = game.State.Players[1];
        game.State.Board.Apply(Enumerable.Range(1, 4).Select(col =>
            new CellChange(new Position(5, col), null, new Piece(opponent, PieceKind.Ordinary))));
        var legal = game.GetLegalMoves(game.State.Current);
        Assert.Equal(new Position(5, 5), new SmarterGomokuStrategy().Choose(game.State, game.State.Current, legal).At);
    }

    [Fact]
    public void A_gap_in_an_opponent_winning_alignment_is_blocked()
    {
        Game game = NewGame();
        Player opponent = game.State.Players[1];
        game.State.Board.Apply(new[] { 1, 2, 4, 5 }.Select(col =>
            new CellChange(new Position(5, col), null, new Piece(opponent, PieceKind.Ordinary))));
        Assert.Equal(new Position(5, 3), new SmarterGomokuStrategy().Choose(game.State, game.State.Current, game.GetLegalMoves(game.State.Current)).At);
    }

    [Fact]
    public void Own_immediate_win_takes_priority_over_an_opponent_threat()
    {
        Game game = NewGame();
        game.State.Board.Apply(Enumerable.Range(1, 4).SelectMany(col => new[]
        {
            new CellChange(new Position(3, col), null, new Piece(game.State.Players[0], PieceKind.Ordinary)),
            new CellChange(new Position(5, col), null, new Piece(game.State.Players[1], PieceKind.Ordinary)),
        }));
        Assert.Equal(new Position(3, 5), new SmarterGomokuStrategy().Choose(game.State, game.State.Current, game.GetLegalMoves(game.State.Current)).At);
    }

    [Fact]
    public void A_legal_eraser_can_neutralise_both_ends_of_an_open_four()
    {
        Game game = NewGame();
        game.State.Board.Apply(Enumerable.Range(3, 4).Select(col =>
            new CellChange(new Position(5, col), null, new Piece(game.State.Players[1], PieceKind.Ordinary))));
        var legal = game.GetLegalMoves(game.State.Current).ToList();
        var eraser = new PlaceInput("E", new Position(5, 4));
        legal.Add(eraser); // Task 2's future legal list supplies this input.
        string[] before = game.State.Board.ToSnapshot();
        int events = 0;
        game.State.Board.BoardChanged += (_, _) => events++;
        Assert.Equal(eraser, new SmarterGomokuStrategy().Choose(game.State, game.State.Current, legal));
        Assert.Equal(before, game.State.Board.ToSnapshot());
        Assert.Equal(2, game.State.Current.Left(PieceKind.Eraser));
        Assert.Equal(0, events);
    }

    [Fact]
    public void The_strategy_respects_the_supplied_legal_list_and_can_choose_heavy()
    {
        Game game = NewGame();
        game.State.Board.Apply(Enumerable.Range(1, 4).Select(col =>
            new CellChange(new Position(3, col), null, new Piece(game.State.Current, PieceKind.Heavy))));
        var legal = new[] { new PlaceInput("H", new Position(3, 5)), new PlaceInput("E", new Position(3, 1)) };
        Assert.Equal(legal[0], new SmarterGomokuStrategy().Choose(game.State, game.State.Current, legal));
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("plus")]
    [InlineData("fog")]
    public void All_three_factories_select_smarter_ai(string variant) =>
        Assert.IsType<SmarterGomokuStrategy>(CatalogSetup.Build().Resolve("gomoku", variant).CreateComputerStrategy(AiLevel.Smarter));
}
