using BoardGames.Core;
using BoardGames.Reversi;

namespace BoardGames.Tests.Reversi;

public class SmarterReversiTests
{
    [Theory]
    [InlineData(ReversiObjective.Majority, true)]
    [InlineData(ReversiObjective.Misere, false)]
    public void Count_objectives_choose_maximum_or_minimum_flips(ReversiObjective objective, bool maximise)
    {
        Game game = ReversiTestSupport.NewGame();
        ReversiTestSupport.SetBoard(game,
            "........", "........", ".OOOX...", "........", "........", ".OX.....", "........", "........");
        var legal = game.GetLegalMoves(game.State.Current);
        var flips = legal.Select(m => ReversiRules.GetFlips(game.State.Board, game.State.Current, m.At).Count);
        PlaceInput choice = new SmarterReversiStrategy(objective).Choose(game.State, game.State.Current, legal);
        Assert.Contains(choice, legal);
        Assert.Equal(maximise ? flips.Max() : flips.Min(), ReversiRules.GetFlips(game.State.Board, game.State.Current, choice.At).Count);
    }

    [Fact]
    public void Corner_ai_prefers_a_corner_over_a_larger_greedy_capture()
    {
        Game game = ReversiTestSupport.NewGame("corner");
        ReversiTestSupport.SetBoard(game,
            ".OX.....", "........", ".OOOX...", "........", "........", "........", "........", "........");
        var legal = game.GetLegalMoves(game.State.Current);
        PlaceInput move = new SmarterReversiStrategy(ReversiObjective.Corners).Choose(game.State, game.State.Current, legal);
        Assert.Equal(new Position(1, 1), move.At);
    }

    [Fact]
    public void Anti_ai_avoids_a_corner_when_a_non_corner_move_is_available()
    {
        Game game = ReversiTestSupport.NewGame("anti");
        ReversiTestSupport.SetBoard(game,
            ".OX.....", "........", ".OOOX...", "........", "........", "........", "........", "........");
        var legal = game.GetLegalMoves(game.State.Current);
        PlaceInput move = new SmarterReversiStrategy(ReversiObjective.Misere).Choose(game.State, game.State.Current, legal);
        Assert.False(ReversiRules.IsCorner(game.State.Board, move.At));
    }

    [Fact]
    public void Corner_ai_takes_an_immediate_third_corner()
    {
        Game game = ReversiTestSupport.NewGame("corner");
        ReversiTestSupport.SetBoard(game,
            ".OX....X", "........", ".OOOX...", "........", "........", "........", "........", "X.......");
        var legal = game.GetLegalMoves(game.State.Current);
        PlaceInput move = new SmarterReversiStrategy(ReversiObjective.Corners).Choose(game.State, game.State.Current, legal);
        Assert.Equal(new Position(1, 1), move.At);
    }

    [Fact]
    public void Corner_ai_blocks_an_opponents_immediate_third_corner_threat()
    {
        Game game = ReversiTestSupport.NewGame("corner");
        // O could play (1,1) against X at (1,2), bounded by O at (1,3).
        // The threat is removed by X at (1,4): O at (1,3) is flipped to X,
        // bounded by X at (1,2). A competing move (4,1) flips three disks.
        ReversiTestSupport.SetBoard(game,
            ".XO....O", "........", "........", ".OOOX...", "........", "........", "........", "O.......");
        var legal = game.GetLegalMoves(game.State.Current);
        PlaceInput move = new SmarterReversiStrategy(ReversiObjective.Corners).Choose(game.State, game.State.Current, legal);
        Assert.Equal(new Position(1, 4), move.At);
        new ReversiPlaceCommand(game.State.Current, move.At).Execute(game.State);
        Assert.DoesNotContain(ReversiRules.GetLegalMoves(game.State.Board, game.State.Players[1]), m => m.At == new Position(1, 1));
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("anti")]
    [InlineData("corner")]
    public void Evaluation_does_not_mutate_live_board_inventory_or_events(string variant)
    {
        Game game = ReversiTestSupport.NewGame(variant);
        string[] before = game.State.Board.ToSnapshot();
        int events = 0;
        game.State.Board.BoardChanged += (_, _) => events++;
        IComputerStrategy strategy = BoardGames.App.CatalogSetup.Build()
            .Resolve("reversi", variant).CreateComputerStrategy(AiLevel.Smarter);
        var legal = game.GetLegalMoves(game.State.Current);
        Assert.Contains(strategy.Choose(game.State, game.State.Current, legal), legal);
        Assert.Equal(before, game.State.Board.ToSnapshot());
        Assert.Equal(0, events);
        Assert.Equal(0, game.State.MoveCount);
    }
}
