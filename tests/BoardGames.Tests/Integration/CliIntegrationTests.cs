using BoardGames.App;
using BoardGames.Core;
using static BoardGames.Tests.Integration.IntegrationSupport;

namespace BoardGames.Tests.Integration;

// CLI test mode against the real Gomoku and Reversi variants. The runner redirects Console.Out while it runs,
// so these tests share a collection with the other CLI tests and never run at the same time.
[Collection(ConsoleCollection.Name)]
public class CliIntegrationTests
{
    private static (int exit, string text) RunCli(params string[] args)
    {
        var writer = new StringWriter();
        int exit = CliRunner.Run(args, CatalogSetup.Build(), writer);
        return (exit, writer.ToString());
    }

    [Fact]
    public void A_GomokuPlus_script_can_use_heavy_and_eraser_stones()
    {
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "plus", "H5:5,O5:6,E5:6,O1:1");

        Assert.Equal(0, exit);
        string[] grid = ParseGrid(text);
        Assert.Equal(10, grid.Length);
        Assert.Equal("....x.....", grid[4]);   // the heavy stone is shown in lower case, the erased cell is empty
        Assert.Equal("O.........", grid[0]);
        Assert.Contains("Game in progress.", text);
    }

    [Fact]
    public void A_script_that_completes_five_in_a_row_ends_the_game_with_the_winner()
    {
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "standard",
            "O5:1,O1:1,O5:2,O1:2,O5:3,O1:3,O5:4,O1:4,O5:5");

        Assert.Equal(0, exit);
        Assert.Contains("Game over.", text);
        Assert.Contains("Result: Player 1 (X) wins: five stones in a row.", text);
    }

    [Fact]
    public void The_status_block_lists_the_active_player_the_counts_and_the_result_in_that_order()
    {
        (_, string text) = RunCli("--game", "reversi", "--variant", "standard", "P3:4,P3:3,P4:3,P5:3,P4:2");

        string[] lines = text.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        int status = Array.FindIndex(lines, line => line.StartsWith("Active player:"));
        Assert.True(status >= 0);
        Assert.Equal("Active player: Player 2 (O)   Counts: X=5  O=4", lines[status]);
        Assert.Equal("Result: in progress.", lines[status + 1]);
        Assert.Equal("Game in progress.", lines[status + 2]);
    }

    [Fact]
    public void A_finished_fog_game_still_prints_the_active_players_fogged_view()
    {
        // P1 completes five in a row on row 5; it is then P2's turn, and P2 has stones only around row 1.
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "fog",
            "O5:1,O1:1,O5:2,O1:2,O5:3,O1:3,O5:4,O1:4,O5:5");

        Assert.Equal(0, exit);
        string[] grid = ParseGrid(text);
        Assert.DoesNotContain('X', grid[4]);   // P1's winning row stays hidden from P2
        Assert.Equal("OOOO.?????", grid[0]);
        Assert.Contains("Active player: Player 2 (O)", text);
        Assert.Contains("Result: Player 1 (X) wins: five stones in a row.", text);
        Assert.Contains("Game over.", text);
    }

    [Fact]
    public void The_standard_Gomoku_example_script_is_won_by_Player_1_on_row_5()
    {
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "standard",
            "O5:3,O6:3,O5:4,O6:4,O5:5,O6:5,O5:6,O6:6,O5:7");

        Assert.Equal(0, exit);
        string[] grid = ParseGrid(text);
        Assert.Equal(10, grid.Length);
        Assert.Equal("..XXXXX...", grid[4]);   // P1 fills 5:3 to 5:7
        Assert.Equal("..OOOO....", grid[5]);   // P2 answers on row 6 and never gets five
        Assert.Contains("Active player: Player 2 (O)   Counts: X=5  O=4", text);
        Assert.Contains("Result: Player 1 (X) wins: five stones in a row.", text);
        Assert.Contains("Game over.", text);
    }

    [Fact]
    public void The_GomokuPlus_example_script_erases_a_stone_and_puts_a_heavy_one_on_the_same_cell()
    {
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "plus",
            "O3:2,O3:3,O3:1,O4:3,O3:4,O5:3,E3:3,H3:3");

        Assert.Equal(0, exit);
        string[] grid = ParseGrid(text);
        Assert.Equal("XXoX......", grid[2]);   // E3:3 erased P2's stone, H3:3 put P2's heavy one there
        Assert.Equal("..O.......", grid[3]);
        Assert.Equal("..O.......", grid[4]);
        // one stone erased and one added, so both players still have three
        Assert.Contains("Active player: Player 1 (X)   Counts: X=3  O=3", text);
        Assert.Contains("Result: in progress.", text);
        Assert.Contains("Game in progress.", text);
    }

    [Fact]
    public void The_GomokuFog_example_script_prints_only_what_the_active_player_can_see()
    {
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "fog",
            "O5:5,O2:2,O5:6,O2:3,O6:5,O8:8");

        Assert.Equal(0, exit);
        string[] grid = ParseGrid(text);
        Assert.Equal(10, grid.Length);
        // P1 holds 5:5, 5:6 and 6:5, so it sees those cells and the ones next to them
        Assert.Equal("???....???", grid[3]);
        Assert.Equal("???.XX.???", grid[4]);
        Assert.Equal("???.X..???", grid[5]);
        Assert.Equal("???...????", grid[6]);
        // P2's stones at 2:2, 2:3 and 8:8 are not next to a P1 stone, so they stay hidden
        Assert.Equal("??????????", grid[1]);
        Assert.Equal("??????????", grid[7]);
        Assert.DoesNotContain('O', string.Concat(grid));
        Assert.Contains("Active player: Player 1 (X)   Counts: X=3  O=3", text);
        Assert.Contains("Game in progress.", text);
    }

    [Fact]
    public void A_script_cannot_use_undo_save_or_help()
    {
        foreach (string control in new[] { "undo", "redo", "help", "save x.json", "quit" })
        {
            (int exit, string text) = RunCli("--game", "gomoku", "--variant", "standard", $"O5:5,{control},O6:6");

            Assert.Equal(1, exit);
            Assert.Contains("not allowed in a script", text);
            Assert.Contains("Stopped at command 2", text);
            Assert.DoesNotContain("O6:6", text.Replace("Stopped at command 2", ""));
        }
    }

    [Fact]
    public void A_fog_script_prints_the_final_board_from_the_active_players_view()
    {
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "fog", "O5:5,O1:1");

        Assert.Equal(0, exit);
        string[] grid = ParseGrid(text);
        Assert.Equal("??????????", grid[0]);   // P2's stone at 1:1 is hidden from the active player, P1
        Assert.Equal("???.X.????", grid[4]);
        Assert.Contains("Active player: Player 1 (X)", text);
    }

    [Fact]
    public void A_fog_script_stops_at_a_move_on_a_cell_that_is_occupied()
    {
        (int exit, string text) = RunCli("--game", "gomoku", "--variant", "fog", "O5:5,O5:5,O6:6");

        Assert.Equal(1, exit);
        Assert.Contains("Stopped at command 2: 'O5:5'", text);
        Assert.Contains("already taken", text);
    }

    [Theory]
    [InlineData("standard", "P3:4,P3:3,P4:3,P5:3,P4:2", "Player 2 (O)", "X=5  O=4")]
    [InlineData("anti", "P4:3,P3:3,P3:4,P5:3,P6:3", "Player 2 (O)", "X=5  O=4")]
    [InlineData("corner", "P3:4,P3:3,P3:2,P2:2,P1:2,P1:1", "Player 1 (X)", "X=5  O=5")]
    public void The_Reversi_example_scripts_run_to_the_end_on_every_variant(
        string variant, string script, string activePlayer, string counts)
    {
        (int exit, string text) = RunCli("--game", "reversi", "--variant", variant, script);

        Assert.Equal(0, exit);
        Assert.DoesNotContain("Stopped at command", text);

        // four opening disks plus one per move: a placement adds a disk, flips never change the total
        string[] rows = ParseReversiRows(text);
        Assert.Equal(8, rows.Length);
        Assert.Equal(4 + script.Split(',').Length, string.Concat(rows).Count(symbol => symbol != '.'));

        Assert.Contains($"Active player: {activePlayer}   Counts: {counts}", text);
        Assert.Contains("Result: in progress.", text);
        Assert.Contains("Game in progress.", text);
    }

    [Fact]
    public void A_Reversi_script_refuses_a_placement_that_flips_nothing()
    {
        (int exit, string text) = RunCli("--game", "reversi", "--variant", "standard", "P1:1,P3:4");

        Assert.Equal(1, exit);
        Assert.Contains("flank", text);
        Assert.Contains("Stopped at command 1: 'P1:1'", text);
    }

    [Fact]
    public void A_Reversi_script_refuses_PASS_while_a_legal_move_exists()
    {
        (int exit, string text) = RunCli("--game", "reversi", "--variant", "standard", "PASS");

        Assert.Equal(1, exit);
        Assert.Contains("only pass when you have no legal move", text);
    }

    // The Reversi board uses the same layout as the Gomoku one: a row number then one symbol per column.
    private static string[] ParseReversiRows(string text) =>
        text.Split('\n')
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(tokens => tokens.Length == 9 && int.TryParse(tokens[0], out _) && !char.IsDigit(tokens[1][0]))
            .Select(tokens => string.Concat(tokens.Skip(1)))
            .ToArray();
}

// One name shared by every test class that redirects Console.Out.
public static class ConsoleCollection
{
    public const string Name = "Console output";
}
