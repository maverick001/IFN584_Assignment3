using System.Text.Json.Nodes;
using BoardGames.App;
using BoardGames.Core;
using BoardGames.Persistence;
using BoardGames.Tests.Support;
using BoardGames.Tests.Reversi;

namespace BoardGames.Tests.Persistence;

public sealed class JsonGameRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ifn584-task3-tests-" + Guid.NewGuid().ToString("N"));
    private readonly JsonGameRepository _repository = new();
    private string SavePath => Path.Combine(_directory, "game.json");

    public JsonGameRepositoryTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, recursive: true);

    public static IEnumerable<object[]> Setups =>
        from game in CatalogSetup.Build().Root.Paths("")
        from mode in new[] { GameMode.HvH, GameMode.HvC }
        from level in new[] { AiLevel.Dumb, AiLevel.Smarter }
        select new object[] { game.Split('/')[0], game.Split('/')[1], mode, level };

    [Theory]
    [MemberData(nameof(Setups))]
    public void Round_trip_restores_setup_board_inventory_turn_and_undo(string family, string variant, GameMode mode, AiLevel level)
    {
        var catalog = CatalogSetup.Build();
        Game original = catalog.Resolve(family, variant).CreateGame(new GameSetup(mode, level));
        original.View.AutoRedraw = false;
        string[] initial = original.State.Board.ToSnapshot();
        string first = family == "reversi" ? "P3:4" : "O1:1";
        string second = family == "reversi" ? "P3:3" : "O2:2";
        original.Play(mode == GameMode.HvH ? new TestIO(first, second, "quit") : new TestIO(first, "quit"));
        string[] before = original.State.Board.ToSnapshot();
        _repository.Save(original, SavePath);
        Game loaded = _repository.Load(SavePath, catalog);
        loaded.View.AutoRedraw = false;

        Assert.Equal(original.Descriptor, loaded.Descriptor);
        Assert.Equal(original.Setup, loaded.Setup);
        Assert.Equal(before, loaded.State.Board.ToSnapshot());
        Assert.Equal(original.State.MoveCount, loaded.State.MoveCount);
        Assert.Equal(original.State.TurnNumber, loaded.State.TurnNumber);
        Assert.Equal(original.State.Current.Index, loaded.State.Current.Index);
        Assert.Equal(original.History.Records(), loaded.History.Records());
        foreach (Player player in original.State.Players)
            Assert.Equal(player.Inventory.OrderBy(k => k.Key), loaded.State.Players[player.Index].Inventory.OrderBy(k => k.Key));
        Assert.Same(_repository, loaded.Repository);
        Assert.False(loaded.History.CanRedo);
        Assert.True(loaded.History.UndoTurn(loaded.State));
        Assert.Equal(initial, loaded.State.Board.ToSnapshot());
        Assert.Equal(0, loaded.State.Current.Index);
        Assert.True(loaded.History.RedoTurn(loaded.State));
        Assert.Equal(before, loaded.State.Board.ToSnapshot());
    }

    [Fact]
    public void Saving_after_undo_preserves_only_the_current_branch()
    {
        Game original = ReversiTestSupport.NewGame();
        original.Play(ScriptedGameIO.FromCsv("P3:4,P3:3,P4:3,P5:3"));
        Assert.True(original.History.UndoTurn(original.State));
        Assert.True(original.History.CanRedo);
        _repository.Save(original, SavePath);
        Game loaded = _repository.Load(SavePath, CatalogSetup.Build());
        Assert.Equal(2, loaded.History.Count);
        Assert.True(loaded.History.CanUndo);
        Assert.False(loaded.History.CanRedo);
        Assert.Equal(original.State.Board.ToSnapshot(), loaded.State.Board.ToSnapshot());
    }

    [Fact]
    public void An_empty_history_and_repeated_overwrite_are_supported()
    {
        Game game = ReversiTestSupport.NewGame();
        _repository.Save(game, SavePath);
        Assert.False(_repository.Load(SavePath, CatalogSetup.Build()).History.CanUndo);
        game.Play(ScriptedGameIO.FromCsv("P3:4,P3:3"));
        _repository.Save(game, SavePath);
        Assert.Equal(2, _repository.Load(SavePath, CatalogSetup.Build()).History.Count);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"Version\":1,\"Version\":1}")]
    public void Malformed_json_is_rejected(string content)
    {
        File.WriteAllText(SavePath, content);
        Assert.Throws<InvalidDataException>(() => _repository.Load(SavePath, CatalogSetup.Build()));
    }

    [Theory]
    [InlineData("Version")]
    [InlineData("FamilyKey")]
    [InlineData("VariantKey")]
    [InlineData("Mode")]
    [InlineData("Level")]
    [InlineData("BoardSnapshot")]
    [InlineData("Inventories")]
    [InlineData("Moves")]
    public void Missing_fields_are_rejected_instead_of_silently_defaulted(string field)
    {
        JsonObject json = SaveOpening();
        json.Remove(field);
        Write(json);
        Assert.Throws<InvalidDataException>(() => _repository.Load(SavePath, CatalogSetup.Build()));
    }

    [Theory]
    [InlineData("version")]
    [InlineData("variant")]
    [InlineData("mode")]
    [InlineData("level")]
    [InlineData("board")]
    [InlineData("inventory")]
    [InlineData("null-moves")]
    [InlineData("actor")]
    [InlineData("missing-actor")]
    [InlineData("illegal-placement")]
    [InlineData("pass-with-position")]
    public void Damaged_save_data_is_rejected(string damage)
    {
        JsonObject json = SaveOpening();
        switch (damage)
        {
            case "version": json["Version"] = 2; break;
            case "variant": json["VariantKey"] = "unknown"; break;
            case "mode": json["Mode"] = "Unknown"; break;
            case "level": json["Level"] = 99; break;
            case "board": json["BoardSnapshot"]![0] = "X......."; break;
            case "inventory": json["Inventories"]!["0"]!["Heavy"] = 1; break;
            case "null-moves": json["Moves"] = null; break;
            case "actor": json["Moves"]![0]!["PlayerIndex"] = 1; break;
            case "missing-actor": json["Moves"]![0]!.AsObject().Remove("PlayerIndex"); break;
            case "illegal-placement": json["Moves"]![0]!["Row"] = 1; break;
            case "pass-with-position": json["Moves"]![0]!["Code"] = "PASS"; break;
        }
        Write(json);
        Assert.Throws<InvalidDataException>(() => _repository.Load(SavePath, CatalogSetup.Build()));
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("anti")]
    [InlineData("corner")]
    public void Completed_matches_load_with_the_result_and_reject_extra_moves(string variant)
    {
        Game game = ReversiTestSupport.NewGame(variant);
        var random = new Random(7);
        for (int step = 0; step < 120 && !game.Result.IsOver; step++)
        {
            var legal = game.GetLegalMoves(game.State.Current);
            PlaceInput? move = legal.Count == 0 ? null : legal[random.Next(legal.Count)];
            game.Play(ScriptedGameIO.FromCsv(move == null ? "PASS" : $"P{move.At.Row}:{move.At.Col}"));
        }
        Assert.True(game.Result.IsOver);
        _repository.Save(game, SavePath);
        Game loaded = _repository.Load(SavePath, CatalogSetup.Build());
        Assert.Equal(game.Result.Kind, loaded.Result.Kind);
        Assert.Equal(game.Result.Winner?.Index, loaded.Result.Winner?.Index);
        Assert.Equal(game.State.Board.ToSnapshot(), loaded.State.Board.ToSnapshot());
        JsonObject json = JsonNode.Parse(File.ReadAllText(SavePath))!.AsObject();
        json["Moves"]!.AsArray().Add(new JsonObject
        {
            ["PlayerIndex"] = game.State.Current.Index, ["Code"] = "PASS", ["Row"] = null, ["Col"] = null,
        });
        Write(json);
        Assert.Throws<InvalidDataException>(() => _repository.Load(SavePath, CatalogSetup.Build()));
    }

    [Fact]
    public void Actual_menu_save_load_and_immediate_undo_use_the_json_repository()
    {
        var io = new TestIO("1", "2", "1", "1", "P3:4", "P3:3", $"save {SavePath}",
            "quit", "2", SavePath, "undo", "undo", "quit", "3");
        new MainMenu(CatalogSetup.Build(), io, _repository).Run();
        Assert.True(File.Exists(SavePath));
        Assert.Contains("Game saved", io.AllOutput);
        Assert.DoesNotContain("Could not", io.AllOutput);
        Assert.Single(io.Output, line => line.Contains("Nothing to undo"));
    }

    [Fact]
    public void A_failed_in_game_load_keeps_the_original_game_playable()
    {
        File.WriteAllText(SavePath, "damaged");
        var io = new TestIO("1", "2", "1", "1", "P3:4", $"load {SavePath}", "P3:3", "quit", "3");
        new MainMenu(CatalogSetup.Build(), io, _repository).Run();
        Assert.Contains("Continuing the current game", io.AllOutput);
        Assert.Contains("Could not load", io.AllOutput);
        Assert.DoesNotContain("must flank", io.AllOutput);
    }

    private JsonObject SaveOpening()
    {
        Game game = ReversiTestSupport.NewGame();
        game.Play(ScriptedGameIO.FromCsv("P3:4,P3:3"));
        _repository.Save(game, SavePath);
        return JsonNode.Parse(File.ReadAllText(SavePath))!.AsObject();
    }

    [Fact]
    public void Loading_a_completed_save_displays_its_result_without_requesting_another_move()
    {
        Game game = ReversiTestSupport.NewGame();
        var random = new Random(7);
        for (int step = 0; step < 120 && !game.Result.IsOver; step++)
        {
            var legal = game.GetLegalMoves(game.State.Current);
            PlaceInput? move = legal.Count == 0 ? null : legal[random.Next(legal.Count)];
            game.Play(ScriptedGameIO.FromCsv(move == null ? "PASS" : $"P{move.At.Row}:{move.At.Col}"));
        }
        Assert.True(game.Result.IsOver);
        _repository.Save(game, SavePath);
        var io = new TestIO("2", SavePath, "3");
        new MainMenu(CatalogSetup.Build(), io, _repository).Run();
        Assert.DoesNotContain(io.Output, line => line.Contains("(X) >") || line.Contains("(O) >"));
        Assert.Contains(io.Output, line => line.Contains("wins:") || line.Contains("draw:"));
        Assert.DoesNotContain("Could not load", io.AllOutput);
    }

    [Fact]
    public void A_real_forced_pass_is_replayed_and_can_be_undone_after_load()
    {
        Game? withPass = null;
        for (int seed = 0; seed < 30 && withPass == null; seed++)
        {
            Game candidate = ReversiTestSupport.NewGame();
            var random = new Random(seed);
            for (int step = 0; step < 120 && !candidate.Result.IsOver; step++)
            {
                var legal = candidate.GetLegalMoves(candidate.State.Current);
                PlaceInput? move = legal.Count == 0 ? null : legal[random.Next(legal.Count)];
                candidate.Play(ScriptedGameIO.FromCsv(move == null ? "PASS" : $"P{move.At.Row}:{move.At.Col}"));
                if (move == null) { withPass = candidate; break; }
            }
        }
        Assert.NotNull(withPass);
        _repository.Save(withPass, SavePath);
        Game loaded = _repository.Load(SavePath, CatalogSetup.Build());
        Assert.Equal("PASS", loaded.History.Records()[^1].Code);
        Assert.Equal(withPass.State.Board.ToSnapshot(), loaded.State.Board.ToSnapshot());
        Assert.True(withPass.History.UndoTurn(withPass.State));
        Assert.True(loaded.History.UndoTurn(loaded.State));
        Assert.Equal(withPass.State.Board.ToSnapshot(), loaded.State.Board.ToSnapshot());
        Assert.Equal(withPass.State.Current.Index, loaded.State.Current.Index);
    }

    private void Write(JsonObject json) => File.WriteAllText(SavePath, json.ToJsonString());
}
