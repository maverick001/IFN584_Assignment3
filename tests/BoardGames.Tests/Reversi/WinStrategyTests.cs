using BoardGames.Core;
using BoardGames.Reversi;

namespace BoardGames.Tests.Reversi;

public class WinStrategyTests
{
    [Theory]
    [InlineData("standard", 0)]
    [InlineData("anti", 1)]
    [InlineData("corner", 0)]
    public void Majority_and_misere_use_opposite_terminal_objectives(string variant, int winner)
    {
        Game game = ReversiTestSupport.NewGame(variant);
        ReversiTestSupport.SetBoard(game, "XXXXOOOO", "XXXXOOOO", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "OOOOOOOO", "OOOOXXXX");
        Assert.False(game.Win.Evaluate(game.State, false).IsOver);
        GameResult result = game.Win.Evaluate(game.State, true);
        Assert.Equal(GameResultKind.Win, result.Kind);
        Assert.Equal(winner, result.Winner!.Index);
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("anti")]
    [InlineData("corner")]
    public void Equal_terminal_counts_are_a_draw(string variant)
    {
        Game game = ReversiTestSupport.NewGame(variant);
        Assert.Equal(GameResultKind.Draw, game.Win.Evaluate(game.State, true).Kind);
        Assert.False(game.Win.Evaluate(game.State, false).IsOver);
    }

    [Fact]
    public void Corner_dominance_wins_on_the_third_corner_even_with_fewer_disks()
    {
        Game game = ReversiTestSupport.NewGame("corner");
        ReversiTestSupport.SetBoard(game,
            ".OXOOOOX", "OOOO.OOO", "OOOOOOOO", "OOOOOOOO", "OOOOOOOO", "OOOOOOOO", "OOOOOOOO", "XOOOOOOO");
        Assert.False(game.Win.Evaluate(game.State, false).IsOver);
        GameOutcome outcome = game.Play(ScriptedGameIO.FromCsv("P1:1,P2:2"));
        Assert.Equal(ExitReason.Finished, outcome.Reason);
        Assert.Equal(0, outcome.Result.Winner!.Index);
        Assert.Equal(1, game.History.Count); // the next command was never consumed
        Assert.Equal(11, ReversiRules.CountDisks(game.State.Board, game.State.Players[0]));
        Assert.Equal(52, ReversiRules.CountDisks(game.State.Board, game.State.Players[1]));
        Assert.NotEmpty(game.GetLegalMoves(game.State.Players[0])); // play could otherwise continue
        Assert.Contains("three-corner", outcome.Result.Reason);
    }

    [Fact]
    public void Neither_player_can_move_ends_a_game_even_with_empty_cells()
    {
        Game game = ReversiTestSupport.NewGame("anti");
        ReversiTestSupport.SetBoard(game, ".OXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXX.");
        GameOutcome outcome = game.Play(ScriptedGameIO.FromCsv("P1:1"));
        Assert.Equal(ExitReason.Finished, outcome.Reason);
        Assert.Null(game.State.Board[new Position(8, 8)]);
        Assert.Equal(1, outcome.Result.Winner!.Index); // zero disks wins in misere
    }
}
