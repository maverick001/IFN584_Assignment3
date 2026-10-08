using BoardGames.App;
using BoardGames.Core;
using BoardGames.Tests.Support;

namespace BoardGames.Tests;

// The main menu, the CLI runner and the six placeholder variants wired together in the App project.
public class AppTests
{
    // ------------------------------------------------------------ the six registered variants

    public static TheoryData<string, string, string> Variants => new()
    {
        { "gomoku", "standard", "O" }, { "gomoku", "plus", "O" }, { "gomoku", "fog", "O" },
        { "reversi", "standard", "P" }, { "reversi", "anti", "P" }, { "reversi", "corner", "P" },
    };

    [Fact]
    public void The_catalog_lists_all_six_variants()
    {
        Assert.Equal(
            new[] { "gomoku/standard", "gomoku/plus", "gomoku/fog", "reversi/standard", "reversi/anti", "reversi/corner" },
            CatalogSetup.Build().Root.Paths(""));
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void Every_variant_plays_in_both_modes_and_rejects_the_other_familys_codes(string family, string variant, string code)
    {
        IGameFactory factory = CatalogSetup.Build().Resolve(family, variant);

        // Human vs Human
        Game hvh = factory.CreateGame(new GameSetup(GameMode.HvH, AiLevel.Dumb));
        Assert.Equal(ExitReason.ScriptEnded, hvh.Play(ScriptedGameIO.FromCsv($"{code}1:1,{code}1:2")).Reason);
        Assert.Equal(2, hvh.History.Count);

        // Human vs Computer, both levels: one human move gets one computer reply
        foreach (AiLevel level in new[] { AiLevel.Dumb, AiLevel.Smarter })
        {
            Game hvc = factory.CreateGame(new GameSetup(GameMode.HvC, level));
            hvc.Play(new TestIO($"{code}1:1", "quit"));
            Assert.Equal(2, hvc.History.Count);
        }

        // Gomoku takes O, Reversi takes P: the other family's code is refused
        string other = code == "O" ? "P" : "O";
        Game strict = factory.CreateGame(new GameSetup(GameMode.HvH, AiLevel.Dumb));
        Assert.Equal(ExitReason.InvalidScriptCommand, strict.Play(ScriptedGameIO.FromCsv($"{other}3:3")).Reason);
        Assert.Equal(0, strict.History.Count);
    }

    // ------------------------------------------------------------ the main menu

    private static (MainMenu menu, TestIO io) Menu(IGameRepository? repo, params string[] input)
    {
        var io = new TestIO(input);
        return (new MainMenu(CatalogSetup.Build(), io, repo), io);
    }

    [Fact]
    public void A_new_game_can_be_started_from_the_menu_and_left_with_quit()
    {
        // New game > Gomoku > Standard > Human vs Human > one move > quit > back at the menu > Quit
        (MainMenu menu, TestIO io) = Menu(null, "1", "1", "1", "1", "O1:1", "quit", "3");

        menu.Run();

        Assert.Contains("Standard Gomoku", io.AllOutput);
        Assert.Contains("Player 2 (O) >", io.AllOutput);   // after P1's move it was P2's turn
    }

    private sealed class LoadingRepository : IGameRepository
    {
        public int Loads;
        public bool Fail;
        public void Save(Game game, string path) { }
        public Game Load(string path, GameCatalog catalog)
        {
            Loads++;
            if (Fail) throw new InvalidDataException("the file is damaged");
            Game game = catalog.Resolve("gomoku", "standard").CreateGame(new GameSetup(GameMode.HvH, AiLevel.Dumb));
            game.Replay(new[] { new MoveRecord(0, "O", 5, 5), new MoveRecord(1, "O", 5, 6) });
            return game;
        }
    }

    [Fact]
    public void Loading_from_the_menu_starts_the_loaded_game_and_undo_works_at_once()
    {
        var repo = new LoadingRepository();
        (MainMenu menu, TestIO io) = Menu(repo, "2", "save1.json", "undo", "undo", "quit", "3");

        menu.Run();

        Assert.Equal(1, repo.Loads);
        // The loaded game has two moves: the first undo works at once, only the second one has nothing left.
        Assert.Single(io.Output, line => line.Contains("Nothing to undo"));
    }

    [Fact]
    public void A_failed_load_shows_the_error_and_keeps_the_current_game()   // PRD FR-S4
    {
        var repo = new LoadingRepository { Fail = true };
        (MainMenu menu, TestIO io) = Menu(repo, "1", "1", "1", "1", "O1:1", "load bad.json", "O2:2", "quit", "3");

        menu.Run();

        Assert.Contains("the file is damaged", io.AllOutput);
        int continuing = io.Output.FindIndex(line => line.Contains("Continuing the current game"));
        Assert.True(continuing >= 0);
        // The game carried on: P2 played O2:2 after the failed load, so P1 is asked for a move afterwards.
        Assert.True(io.Output.FindLastIndex(line => line.Contains("Player 1 (X) >")) > continuing);
    }

    // ------------------------------------------------------------ CLI test mode
    // Starter tests written by A (Task 1) in case Task 4 runs short. D adds the spec's six example scripts here
    // once the real games exist (see Readme.md).

    private static (int exit, string text) RunCli(params string[] args)
    {
        var writer = new StringWriter();
        int exit = CliRunner.Run(args, CatalogSetup.Build(), writer);
        return (exit, writer.ToString());
    }

    [Fact]
    public void The_CLI_plays_a_script_and_prints_only_the_final_board_and_status()
    {
        (int exit, string text) = RunCli("--game", "reversi", "--variant", "corner", "P3:4,P3:3,P3:2,P2:2,P1:2,P1:1");

        Assert.Equal(0, exit);
        Assert.Equal(1, text.Split('\n').Count(line => line.StartsWith("  1 ")));   // exactly one board (its row 1)
        Assert.Contains("Active player: Player 1 (X)", text);
        Assert.Contains("Counts: X=5  O=5", text);
    }

    [Fact]
    public void The_CLI_stops_at_the_first_invalid_command_and_reports_it()   // PRD FR-C2: stop at the first invalid command
    {
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "standard", "O5:3,O5:3,O6:3");

        Assert.Equal(1, exit);
        Assert.Contains("already taken", text);
        Assert.Contains("Stopped at command 2: 'O5:3'", text);
        Assert.Contains("Counts: X=1  O=0", text);
    }

    [Fact]
    public void The_CLI_explains_bad_arguments()
    {
        (int unknownExit, string unknown) = RunCli("--game", "chess", "--variant", "x", "O1:1");
        Assert.Equal(2, unknownExit);
        Assert.Contains("reversi/corner", unknown);   // it lists the valid games

        (int missingExit, string usage) = RunCli("--game", "gomoku");
        Assert.Equal(2, missingExit);
        Assert.Contains("Usage", usage);
    }
}
