using BoardGames.App;
using BoardGames.Core;
using BoardGames.Persistence;
using BoardGames.Tests.Support;
using static BoardGames.Tests.Integration.IntegrationSupport;

namespace BoardGames.Tests.Integration;

// GomokuPlus (Heavy and Eraser stones) working together with Core's undo, the win strategy and the save file.
public sealed class GomokuPlusIntegrationTests : IDisposable
{
    private readonly List<string> _files = new();
    private readonly JsonGameRepository _repository = new();

    public void Dispose()
    {
        foreach (string file in _files) File.Delete(file);
    }

    private string NewFile()
    {
        string path = TempFile();
        _files.Add(path);
        return path;
    }

    private static int Left(Game game, int player, PieceKind kind) => game.State.Players[player].Left(kind);

    // P1 heavy stone, P2 ordinary stone, P1 erases it, P2 replies: the position most tests start from.
    private static Game PlayEraserOpening()
    {
        Game game = NewGame("gomoku", "plus");
        game.Play(new TestIO("H5:5", "O5:6", "E5:6", "O1:1", "quit"));
        return game;
    }

    [Fact]
    public void Heavy_and_eraser_stones_use_up_the_inventory_and_the_erased_cell_becomes_empty()
    {
        Game game = PlayEraserOpening();

        Assert.Equal(1, Left(game, 0, PieceKind.Heavy));
        Assert.Equal(1, Left(game, 0, PieceKind.Eraser));
        Assert.Equal(2, Left(game, 1, PieceKind.Heavy));
        Assert.Equal(2, Left(game, 1, PieceKind.Eraser));
        Assert.Null(game.State.Board[new Position(5, 6)]);
        Assert.Equal(PieceKind.Heavy, game.State.Board[new Position(5, 5)]!.Kind);
        Assert.Equal(4, game.History.Count);
    }

    [Fact]
    public void An_eraser_move_is_undone_and_redone_exactly_including_the_inventory()
    {
        Game game = NewGame("gomoku", "plus");
        game.Play(new TestIO("O1:1", "O3:1", "quit"));
        string[] afterTwo = game.State.Board.ToSnapshot();

        // P1 erases P2's stone, P2 replies; undo takes back both moves of that turn.
        game.Play(new TestIO("E3:1", "O9:9", "undo", "quit"));
        Assert.Equal(afterTwo, game.State.Board.ToSnapshot());
        Assert.Equal(2, Left(game, 0, PieceKind.Eraser));
        Assert.Equal(2, game.History.Count);

        Assert.True(game.History.RedoTurn(game.State));
        Assert.Null(game.State.Board[new Position(3, 1)]);
        Assert.Equal(1, Left(game, 0, PieceKind.Eraser));
        Assert.Equal(4, game.History.Count);
    }

    [Fact]
    public void An_eraser_cannot_target_a_heavy_stone_an_own_stone_or_an_empty_cell()
    {
        Game game = NewGame("gomoku", "plus");
        var io = new TestIO("H5:5", "E5:5", "E9:9", "O1:1", "O1:2", "E1:1", "quit");

        game.Play(io);

        Assert.Contains("heavy stone", io.AllOutput);
        Assert.Contains("is empty", io.AllOutput);
        Assert.Contains("own stone", io.AllOutput);
        Assert.Equal(3, game.History.Count);   // H5:5, O1:1 and O1:2 only: every eraser attempt was refused
        Assert.Equal(2, Left(game, 0, PieceKind.Eraser));
        Assert.Equal(2, Left(game, 1, PieceKind.Eraser));
    }

    [Fact]
    public void A_third_eraser_or_a_third_heavy_stone_is_refused()
    {
        Game erasers = NewGame("gomoku", "plus");
        var eraserIo = new TestIO("O1:1", "O3:1", "E3:1", "O3:2", "E3:2", "O3:3", "E3:3", "quit");
        erasers.Play(eraserIo);
        Assert.Contains("no more erasers", eraserIo.AllOutput);
        Assert.Equal(0, Left(erasers, 0, PieceKind.Eraser));
        Assert.NotNull(erasers.State.Board[new Position(3, 3)]);

        Game heavies = NewGame("gomoku", "plus");
        var heavyIo = new TestIO("H1:1", "O9:1", "H1:3", "O9:3", "H1:5", "quit");
        heavies.Play(heavyIo);
        Assert.Contains("no more heavy", heavyIo.AllOutput);
        Assert.Null(heavies.State.Board[new Position(1, 5)]);
    }

    [Fact]
    public void Saving_and_loading_restores_heavy_and_eraser_stones_the_inventories_and_the_turn()
    {
        Game game = PlayEraserOpening();
        string path = NewFile();
        _repository.Save(game, path);

        Game loaded = _repository.Load(path, CatalogSetup.Build());

        Assert.Equal(game.State.Board.ToSnapshot(), loaded.State.Board.ToSnapshot());
        Assert.Equal(game.History.Records(), loaded.History.Records());
        Assert.Equal(game.State.Current.Index, loaded.State.Current.Index);
        foreach (PieceKind kind in new[] { PieceKind.Heavy, PieceKind.Eraser })
            foreach (int player in new[] { 0, 1 })
                Assert.Equal(Left(game, player, kind), Left(loaded, player, kind));
    }

    [Fact]
    public void Undo_works_straight_after_a_load_and_redo_history_starts_empty()
    {
        Game game = PlayEraserOpening();
        string path = NewFile();
        _repository.Save(game, path);
        Game loaded = _repository.Load(path, CatalogSetup.Build());
        loaded.View.AutoRedraw = false;
        Assert.False(loaded.History.CanRedo);

        var io = new TestIO("redo", "undo", "redo", "quit");
        loaded.Play(io);

        Assert.Single(io.Output, line => line.Contains("Nothing to redo"));
        Assert.DoesNotContain("Nothing to undo", io.AllOutput);
        // The undo took back the eraser turn and the redo applied it again, so the game is where it was saved.
        Assert.Equal(game.State.Board.ToSnapshot(), loaded.State.Board.ToSnapshot());
        Assert.Equal(1, Left(loaded, 0, PieceKind.Eraser));
    }

    [Fact]
    public void Undoing_the_eraser_turn_after_a_load_brings_back_the_erased_stone_and_the_eraser()
    {
        Game game = PlayEraserOpening();
        string path = NewFile();
        _repository.Save(game, path);
        Game loaded = _repository.Load(path, CatalogSetup.Build());

        Assert.True(loaded.History.UndoTurn(loaded.State));

        Piece restored = loaded.State.Board[new Position(5, 6)]!;
        Assert.Equal(1, restored.Owner.Index);
        Assert.Equal(PieceKind.Ordinary, restored.Kind);
        Assert.Equal(2, Left(loaded, 0, PieceKind.Eraser));
        Assert.Null(loaded.State.Board[new Position(1, 1)]);
    }

    [Fact]
    public void A_heavy_stone_counts_toward_five_in_a_row()
    {
        Game game = NewGame("gomoku", "plus");

        GameOutcome outcome = game.Play(ScriptedGameIO.FromCsv("H5:1,O1:1,H5:2,O1:2,O5:3,O1:3,O5:4,O1:4,O5:5"));

        Assert.Equal(ExitReason.Finished, outcome.Reason);
        Assert.Equal(GameResultKind.Win, game.Result.Kind);
        Assert.Equal(0, game.Result.Winner!.Index);
    }

    [Fact]
    public void Six_in_a_row_also_wins()
    {
        Game game = NewGame("gomoku", "standard");

        GameOutcome outcome = game.Play(ScriptedGameIO.FromCsv("O5:1,O1:1,O5:2,O1:3,O5:3,O1:5,O5:5,O1:7,O5:6,O1:9,O5:4"));

        Assert.Equal(ExitReason.Finished, outcome.Reason);
        Assert.Equal(0, game.Result.Winner!.Index);
    }

    [Fact]
    public void An_eraser_breaks_up_a_line_so_the_opponent_cannot_complete_it()
    {
        Game game = NewGame("gomoku", "plus");
        // P2 builds four in a row on row 5; P1 erases one of them, so P2's next stone does not make five.
        game.Play(new TestIO("O1:1", "O5:1", "O1:3", "O5:2", "O1:5", "O5:3", "O1:7", "O5:4", "E5:3", "O5:5", "quit"));

        Assert.False(game.Result.IsOver);
        Assert.Null(game.State.Board[new Position(5, 3)]);
    }

    [Fact]
    public void A_finished_save_is_shown_with_its_result_when_loaded_from_the_menu()
    {
        Game game = NewGame("gomoku", "standard");
        game.Play(ScriptedGameIO.FromCsv("O5:1,O1:1,O5:2,O1:2,O5:3,O1:3,O5:4,O1:4,O5:5"));
        string path = NewFile();
        _repository.Save(game, path);
        var io = new TestIO("2", path, "3");

        new MainMenu(CatalogSetup.Build(), io, _repository).Run();

        Assert.Contains("wins", io.AllOutput);
        Assert.DoesNotContain("Player 2 (O) >", io.AllOutput);   // no further turn was started
    }

    [Fact]
    public void A_game_saved_with_the_in_game_save_command_loads_from_the_menu()
    {
        string path = NewFile();
        // New game > Gomoku > GomokuPlus > Human vs Human, play, save, quit; then Load game, undo at once, quit.
        var io = new TestIO("1", "1", "2", "1", "H5:5", "O5:6", $"save {path}", "quit",
                            "2", path, "undo", "quit", "3");

        new MainMenu(CatalogSetup.Build(), io, _repository).Run();

        Assert.True(File.Exists(path));
        Assert.Contains("GomokuPlus", io.AllOutput);
        Assert.DoesNotContain("Could not", io.AllOutput);
        Assert.DoesNotContain("Nothing to undo", io.AllOutput);
    }
}
