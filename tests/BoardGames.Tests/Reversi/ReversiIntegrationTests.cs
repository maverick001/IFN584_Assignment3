using BoardGames.Core;
using BoardGames.Reversi;

namespace BoardGames.Tests.Reversi;

public class ReversiIntegrationTests
{
    public static IEnumerable<object[]> Matches =>
        from variant in new[] { "standard", "anti", "corner" }
        from seed in Enumerable.Range(0, 8)
        select new object[] { variant, seed };

    [Theory]
    [MemberData(nameof(Matches))]
    public void Complete_seeded_games_agree_with_an_independent_legality_oracle(string variant, int seed)
    {
        Game game = ReversiTestSupport.NewGame(variant);
        var random = new Random(seed);
        for (int step = 0; step < 120 && !game.Result.IsOver; step++)
        {
            Player actor = game.State.Current;
            var expected = ReversiTestSupport.OracleLegal(game.State.Board, actor);
            Assert.Equal(expected, game.GetLegalMoves(actor).Select(m => m.At));
            string input;
            if (expected.Count == 0) input = "PASS";
            else { Position at = expected[random.Next(expected.Count)]; input = $"P{at.Row}:{at.Col}"; }
            Assert.NotEqual(ExitReason.InvalidScriptCommand, game.Play(ScriptedGameIO.FromCsv(input)).Reason);
            Assert.Equal(game.History.Count, game.State.MoveCount);
        }
        Assert.True(game.Result.IsOver);
        int x = ReversiRules.CountDisks(game.State.Board, game.State.Players[0]);
        int o = ReversiRules.CountDisks(game.State.Board, game.State.Players[1]);
        Player? cornerWinner = variant == "corner"
            ? game.State.Players.FirstOrDefault(p => ReversiRules.CountCorners(game.State.Board, p) >= 3) : null;
        if (cornerWinner != null) Assert.Same(cornerWinner, game.Result.Winner);
        else if (x == o) Assert.Equal(GameResultKind.Draw, game.Result.Kind);
        else Assert.Equal(variant == "anti" ? (x < o ? 0 : 1) : (x > o ? 0 : 1), game.Result.Winner!.Index);
    }
}
