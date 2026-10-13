using BoardGames.App;
using BoardGames.Core;
using BoardGames.Reversi;
using BoardGames.Tests.Support;

namespace BoardGames.Tests.Reversi;

public class DumbReversiTests
{
    [Fact]
    public void The_move_always_comes_from_the_legal_list()
    {
        Game game = ReversiTestSupport.NewGame(level: AiLevel.Dumb);
        var legal = game.GetLegalMoves(game.State.Current);
        var strategy = new DumbReversiStrategy();
        for (int i = 0; i < 100; i++)
            Assert.Contains(strategy.Choose(game.State, game.State.Current, legal), legal);
    }

    [Fact]
    public void The_moves_are_not_always_the_same()
    {
        // The opening has four legal moves, so 100 random picks that are all equal is practically impossible.
        Game game = ReversiTestSupport.NewGame(level: AiLevel.Dumb);
        var legal = game.GetLegalMoves(game.State.Current);
        var strategy = new DumbReversiStrategy();
        var picked = Enumerable.Range(0, 100).Select(_ => strategy.Choose(game.State, game.State.Current, legal)).Distinct();
        Assert.True(picked.Count() > 1);
    }

    [Fact]
    public void A_single_legal_move_is_chosen()
    {
        Game game = ReversiTestSupport.NewGame(level: AiLevel.Dumb);
        var legal = new[] { new PlaceInput("P", new Position(3, 4)) };
        Assert.Equal(legal[0], new DumbReversiStrategy().Choose(game.State, game.State.Current, legal));
    }

    [Fact]
    public void An_empty_legal_list_is_refused()
    {
        Game game = ReversiTestSupport.NewGame(level: AiLevel.Dumb);
        Assert.Throws<ArgumentException>(() =>
            new DumbReversiStrategy().Choose(game.State, game.State.Current, Array.Empty<PlaceInput>()));
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("anti")]
    [InlineData("corner")]
    public void All_three_factories_select_dumb_ai(string variant) =>
        Assert.IsType<DumbReversiStrategy>(CatalogSetup.Build().Resolve("reversi", variant).CreateComputerStrategy(AiLevel.Dumb));

    [Theory]
    [InlineData("standard")]
    [InlineData("anti")]
    [InlineData("corner")]
    public void The_computer_replies_to_the_human_move(string variant)
    {
        Game game = ReversiTestSupport.NewGame(variant, GameMode.HvC, AiLevel.Dumb);
        game.Play(new TestIO("P3:4", "quit"));
        Assert.Equal(2, game.State.MoveCount);
    }
}
