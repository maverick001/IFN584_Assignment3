using BoardGames.App;
using BoardGames.Core;
using BoardGames.Persistence;
using BoardGames.Tests.Support;
using static BoardGames.Tests.Integration.IntegrationSupport;

namespace BoardGames.Tests.Integration;

// GomokuFog: each player sees only the cells next to their own stones (all eight neighbours).
public sealed class GomokuFogIntegrationTests : IDisposable
{
    private readonly List<string> _files = new();

    public void Dispose()
    {
        foreach (string file in _files) File.Delete(file);
    }

    [Fact]
    public void A_player_with_no_stones_sees_nothing()
    {
        Game game = NewGame("gomoku", "fog");
        game.Play(new TestIO("O5:5", "quit"));   // it is now P2's turn and P2 has no stone

        Assert.All(FogGrid(game), row => Assert.Equal("??????????", row));
    }

    [Fact]
    public void Each_player_sees_their_own_stones_and_the_eight_cells_around_them()
    {
        Game game = NewGame("gomoku", "fog");
        game.Play(new TestIO("O5:5", "O1:1", "quit"));   // P1's turn again

        string[] p1 = FogGrid(game);
        Assert.Equal("??????????", p1[0]);   // P2's stone at 1:1 is hidden from P1
        Assert.Equal("???...????", p1[3]);   // row 4: the diagonal neighbours 4:4 and 4:6 are visible
        Assert.Equal("???.X.????", p1[4]);
        Assert.Equal("???...????", p1[5]);
        Assert.Equal("?", p1[2][2].ToString());   // 3:3 is two cells away

        game.Play(new TestIO("O9:9", "quit"));   // now P2's turn
        string[] p2 = FogGrid(game);
        Assert.Equal("O.????????", p2[0]);
        Assert.Equal("..????????", p2[1]);
        Assert.All(p2.Skip(2).Take(7), row => Assert.DoesNotContain('X', row));   // P1's 5:5 stays hidden
    }

    [Fact]
    public void Placing_on_a_hidden_occupied_cell_is_refused_and_the_player_tries_again()
    {
        Game game = NewGame("gomoku", "fog");
        var io = new TestIO("O5:5", "O5:5", "O5:6", "quit");

        game.Play(io);

        Assert.Contains("already taken", io.AllOutput);
        Assert.Equal(2, game.History.Count);
        Assert.Equal(0, game.State.Board[new Position(5, 5)]!.Owner.Index);
        Assert.Equal(1, game.State.Board[new Position(5, 6)]!.Owner.Index);
    }

    [Fact]
    public void A_loaded_fog_game_shows_the_same_view_as_the_game_that_was_saved()
    {
        Game game = NewGame("gomoku", "fog");
        game.Play(new TestIO("O5:5", "O1:1", "O5:6", "O2:2", "quit"));
        string path = TempFile();
        _files.Add(path);
        var repository = new JsonGameRepository();
        repository.Save(game, path);

        Game loaded = repository.Load(path, CatalogSetup.Build());

        Assert.Equal(FogGrid(game), FogGrid(loaded));
        // And the other player's view too, after one undo on each side.
        Assert.True(game.History.UndoTurn(game.State));
        Assert.True(loaded.History.UndoTurn(loaded.State));
        Assert.Equal(FogGrid(game), FogGrid(loaded));
    }

    [Fact]
    public void Undo_in_a_fog_game_hides_the_cells_that_are_no_longer_next_to_a_stone()
    {
        Game game = NewGame("gomoku", "fog");
        game.Play(new TestIO("O5:5", "O1:1", "O5:6", "O2:2", "quit"));
        Assert.Equal("???.XX.???", FogGrid(game)[4]);

        Assert.True(game.History.UndoTurn(game.State));

        Assert.Equal("???.X.????", FogGrid(game)[4]);
    }

    [Theory]
    [InlineData(AiLevel.Dumb)]
    [InlineData(AiLevel.Smarter)]
    public void The_computer_plays_from_the_true_board_not_from_its_own_fog(AiLevel level)
    {
        Game game = NewGame("gomoku", "fog", GameMode.HvC, level);
        // P1 already has four in a row far away from any computer stone; only the true board shows the threat.
        game.State.Board.Apply(Enumerable.Range(1, 4).Select(col =>
            new CellChange(new Position(3, col), null, new Piece(game.State.Players[0], PieceKind.Ordinary))));

        game.Play(new TestIO("O9:9", "quit"));

        Assert.Equal(2, game.History.Count);
        Position reply = game.History.Records().Select(r => new Position(r.Row!.Value, r.Col!.Value)).Last();
        Assert.Equal(1, game.State.Board[reply]!.Owner.Index);   // the reply is a P2 stone
        if (level == AiLevel.Smarter)
            Assert.Equal(new Position(3, 5), reply);   // it blocks the only winning cell
    }

    [Theory]
    [InlineData("standard", AiLevel.Dumb)]
    [InlineData("standard", AiLevel.Smarter)]
    [InlineData("plus", AiLevel.Dumb)]
    [InlineData("plus", AiLevel.Smarter)]
    [InlineData("fog", AiLevel.Dumb)]
    [InlineData("fog", AiLevel.Smarter)]
    public void The_computer_never_makes_a_move_the_game_rejects(string variant, AiLevel level)
    {
        Game game = NewGame("gomoku", variant, GameMode.HvC, level);
        var io = new AutoHumanIO(game, moves: 25);

        GameOutcome outcome = game.Play(io);

        Assert.True(outcome.Reason is ExitReason.Quit or ExitReason.Finished);
        Assert.DoesNotContain(io.Output, line => line.Contains("not valid") || line.Contains("already taken"));
        // Every human move got exactly one computer reply, unless the human's move ended the game.
        if (outcome.Reason == ExitReason.Quit) Assert.Equal(0, game.History.Count % 2);
        Assert.True(game.History.Count >= 2);
    }
}
