using BoardGames.App;
using BoardGames.Core;
using BoardGames.Gomoku;
using BoardGames.Tests.Support;

namespace BoardGames.Tests.Gomoku;

public class DumbGomokuTests
{
    private static Game NewGame(string variant = "plus", GameMode mode = GameMode.HvH)
    {
        Game game = CatalogSetup.Build().Resolve("gomoku", variant).CreateGame(new GameSetup(mode, AiLevel.Dumb));
        game.View.AutoRedraw = false;
        return game;
    }

    [Fact]
    public void The_move_always_comes_from_the_legal_list()
    {
        Game game = NewGame();
        var legal = game.GetLegalMoves(game.State.Current);
        var strategy = new DumbGomokuStrategy();
        for (int i = 0; i < 100; i++)
            Assert.Contains(strategy.Choose(game.State, game.State.Current, legal), legal);
    }

    [Fact]
    public void The_moves_are_not_always_the_same()
    {
        Game game = NewGame();
        var legal = game.GetLegalMoves(game.State.Current);
        var strategy = new DumbGomokuStrategy();
        var picked = Enumerable.Range(0, 100).Select(_ => strategy.Choose(game.State, game.State.Current, legal)).Distinct();
        Assert.True(picked.Count() > 1);
    }

    [Fact]
    public void A_single_legal_move_is_chosen()
    {
        Game game = NewGame();
        var legal = new[] { new PlaceInput("O", new Position(4, 4)) };
        Assert.Equal(legal[0], new DumbGomokuStrategy().Choose(game.State, game.State.Current, legal));
    }

    [Fact]
    public void An_empty_legal_list_is_refused()
    {
        Game game = NewGame();
        Assert.Throws<ArgumentException>(() =>
            new DumbGomokuStrategy().Choose(game.State, game.State.Current, Array.Empty<PlaceInput>()));
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("plus")]
    [InlineData("fog")]
    public void All_three_factories_select_dumb_ai(string variant) =>
        Assert.IsType<DumbGomokuStrategy>(CatalogSetup.Build().Resolve("gomoku", variant).CreateComputerStrategy(AiLevel.Dumb));

    [Theory]
    [InlineData("standard")]
    [InlineData("plus")]
    [InlineData("fog")]
    public void The_computer_replies_to_the_human_move(string variant)
    {
        Game game = NewGame(variant, GameMode.HvC);
        game.Play(new TestIO("O5:5", "quit"));
        Assert.Equal(2, game.State.MoveCount);
    }
}
