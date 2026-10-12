using BoardGames.Core;
using BoardGames.Reversi;

namespace BoardGames.Tests.Reversi;

public class HumanComputerTests
{
    [Theory]
    [InlineData("standard", 1)]
    [InlineData("standard", 2)]
    [InlineData("standard", 3)]
    [InlineData("anti", 1)]
    [InlineData("anti", 2)]
    [InlineData("anti", 3)]
    [InlineData("corner", 1)]
    [InlineData("corner", 2)]
    [InlineData("corner", 3)]
    public void Smarter_computer_completes_a_full_match_through_the_shared_loop(string variant, int seed)
    {
        Game game = ReversiTestSupport.NewGame(variant, GameMode.HvC);
        var io = new OracleHumanIO(game, seed);
        GameOutcome result = game.Play(io);
        Assert.Equal(ExitReason.Finished, result.Reason);
        Assert.True(result.Result.IsOver);
        Assert.Contains(io.Output, line => line.StartsWith("Computer plays"));
        Assert.Equal(game.History.Count, game.State.MoveCount);
        Assert.InRange(game.State.MoveCount, 1, 120);
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("anti")]
    [InlineData("corner")]
    public void Undo_and_redo_restore_the_human_move_and_smarter_reply(string variant)
    {
        Game game = ReversiTestSupport.NewGame(variant, GameMode.HvC);
        string[] opening = game.State.Board.ToSnapshot();
        game.Play(new Support.TestIO("P3:4", "quit"));
        string[] response = game.State.Board.ToSnapshot();
        Assert.Equal(2, game.State.MoveCount);
        game.Play(new Support.TestIO("undo", "quit"));
        Assert.Equal(opening, game.State.Board.ToSnapshot());
        Assert.Equal(0, game.State.Current.Index);
        game.Play(new Support.TestIO("redo", "quit"));
        Assert.Equal(response, game.State.Board.ToSnapshot());
        Assert.Equal(0, game.State.Current.Index);
    }

    private sealed class OracleHumanIO(Game game, int seed) : IGameIO
    {
        private readonly Random _random = new(seed);
        public List<string> Output { get; } = new();
        public bool IsScripted => false;
        public string? ReadLine(string prompt)
        {
            Assert.True(game.State.MoveCount < 120, "A Reversi match must terminate.");
            Assert.Equal(0, game.State.Current.Index);
            var legal = ReversiTestSupport.OracleLegal(game.State.Board, game.State.Current);
            if (legal.Count == 0) return "PASS";
            Position at = legal[_random.Next(legal.Count)];
            return $"P{at.Row}:{at.Col}";
        }
        public void WriteLine(string text) => Output.Add(text);
    }
}
