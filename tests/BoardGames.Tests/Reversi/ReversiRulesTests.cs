using BoardGames.Core;
using BoardGames.Reversi;
using BoardGames.Tests.Support;

namespace BoardGames.Tests.Reversi;

public class ReversiRulesTests
{
    [Theory]
    [InlineData("standard")]
    [InlineData("anti")]
    [InlineData("corner")]
    public void Opening_has_the_specified_disks_and_four_legal_moves(string variant)
    {
        Game game = ReversiTestSupport.NewGame(variant);
        Assert.Equal(new[] { "........", "........", "........", "...OX...", "...XO...", "........", "........", "........" }, game.State.Board.ToSnapshot());
        Assert.Equal(new[] { new Position(3, 4), new Position(4, 3), new Position(5, 6), new Position(6, 5) },
            game.GetLegalMoves(game.State.Current).Select(m => m.At));
        Assert.False(game.Validate(new PassInput()).IsValid);
    }

    [Theory]
    [InlineData("P", 1, 1)]
    [InlineData("P", 4, 4)]
    [InlineData("P", 0, 4)]
    [InlineData("P", 9, 4)]
    [InlineData("O", 3, 4)]
    [InlineData("H", 3, 4)]
    [InlineData("E", 3, 4)]
    public void Invalid_moves_do_not_change_the_state(string code, int row, int col)
    {
        Game game = ReversiTestSupport.NewGame();
        string[] before = game.State.Board.ToSnapshot();
        Assert.False(game.Validate(new PlaceInput(code, new Position(row, col))).IsValid);
        Assert.Equal(before, game.State.Board.ToSnapshot());
        Assert.Equal(0, game.History.Count);
    }

    [Fact]
    public void One_command_flips_all_eight_directions_and_undo_redo_restore_exact_colours()
    {
        Game game = ReversiTestSupport.NewGame();
        ReversiTestSupport.SetBoard(game,
            "........", ".X.X.X..", "..OOO...", ".XO.OX..", "..OOO...", ".X.X.X..", "........", "........");
        Player actor = game.State.Current;
        var command = new ReversiPlaceCommand(actor, new Position(4, 4));
        int events = 0;
        game.State.Board.BoardChanged += (_, e) => { events++; Assert.Equal(9, e.Changes.Count); };
        string[] original = game.State.Board.ToSnapshot();
        CommandAssert.ExecuteThenUndoRestoresState(game.State, command);
        Assert.Equal(2, events);
        command.Execute(game.State);
        Assert.Equal(17, ReversiRules.CountDisks(game.State.Board, actor));
        Assert.Equal(0, ReversiRules.CountDisks(game.State.Board, game.State.Players[1]));
        command.Undo(game.State);
        Assert.Equal(original, game.State.Board.ToSnapshot());
        Assert.Equal(new MoveRecord(0, "P", 4, 4), command.ToRecord());
    }

    [Theory]
    [InlineData(".OX.....", 1)]
    [InlineData(".OOOX...", 3)]
    [InlineData(".OOO....", 0)]
    [InlineData(".O.OX...", 0)]
    [InlineData(".XOX....", 0)]
    public void A_ray_requires_adjacent_opponents_and_a_friendly_endpoint(string row, int flips)
    {
        Game game = ReversiTestSupport.NewGame();
        ReversiTestSupport.SetBoard(game, row, "........", "........", "........", "........", "........", "........", "........");
        Assert.Equal(flips, ReversiRules.GetFlips(game.State.Board, game.State.Current, new Position(1, 1)).Count);
    }

    [Fact]
    public void An_invalid_command_throws_without_mutating_the_board()
    {
        Game game = ReversiTestSupport.NewGame();
        string[] before = game.State.Board.ToSnapshot();
        Assert.Throws<InvalidOperationException>(() => new ReversiPlaceCommand(game.State.Current, new Position(1, 1)).Execute(game.State));
        Assert.Equal(before, game.State.Board.ToSnapshot());
    }

    [Fact]
    public void A_forced_pass_is_undoable_as_part_of_a_full_turn()
    {
        Game game = ReversiTestSupport.NewGame();
        // P1 has no move; P2 can fill the only empty cell, flipping X at (1,2).
        ReversiTestSupport.SetBoard(game, ".XOOOOOO", "OOOOOOOO", "OOOOOOOO", "OOOOOOOO", "OOOOOOOO", "OOOOOOOO", "OOOOOOOO", "OOOOOOOO");
        string[] before = game.State.Board.ToSnapshot();
        Assert.True(game.Validate(new PassInput()).IsValid);
        Assert.Empty(game.GetLegalMoves(game.State.Current));
        GameOutcome outcome = game.Play(ScriptedGameIO.FromCsv("PASS,P1:1"));
        Assert.Equal(ExitReason.Finished, outcome.Reason);
        Assert.Equal(2, game.History.Count);
        Assert.Equal("PASS", game.History.Records()[0].Code);
        Assert.True(game.History.UndoTurn(game.State));
        Assert.Equal(before, game.State.Board.ToSnapshot());
        Assert.Equal(0, game.State.Current.Index);
        Assert.True(game.History.RedoTurn(game.State));
        Assert.Equal(64, ReversiRules.CountDisks(game.State.Board, game.State.Players[1]));
    }

    [Fact]
    public void The_computer_passes_automatically_when_it_has_no_legal_move()
    {
        Game game = ReversiTestSupport.NewGame(mode: GameMode.HvC);
        game.History.Do(new PassCommand(game.State.Current), game.State);
        ReversiTestSupport.SetBoard(game, ".OXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX");
        var io = new TestIO("quit");
        game.Play(io);
        Assert.Equal(2, game.History.Count);
        Assert.Equal("PASS", game.History.Records()[1].Code);
        Assert.Contains("Computer plays PASS", io.AllOutput);
        Assert.Equal(0, game.State.Current.Index);
    }

    [Fact]
    public void Multi_turn_undo_redo_and_branching_preserve_board_and_current_player()
    {
        Game game = ReversiTestSupport.NewGame();
        string[] initial = game.State.Board.ToSnapshot();
        game.Play(ScriptedGameIO.FromCsv("P3:4,P3:3,P4:3,P5:3"));
        string[] played = game.State.Board.ToSnapshot();
        game.Play(new TestIO("undo", "undo", "quit"));
        Assert.Equal(initial, game.State.Board.ToSnapshot());
        Assert.Equal(0, game.State.Current.Index);
        game.Play(new TestIO("redo", "redo", "quit"));
        Assert.Equal(played, game.State.Board.ToSnapshot());
        game.Play(new TestIO("undo", "undo", "P4:3", "redo", "quit"));
        Assert.Equal(1, game.History.Count);
        Assert.False(game.History.CanRedo);
    }
}
