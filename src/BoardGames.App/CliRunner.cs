using BoardGames.Core;
using BoardGames.Gomoku;

namespace BoardGames.App;

// CLI test mode: plays a comma-separated script of moves and PASS without prompts, then prints the final board and the status.
public static class CliRunner
{
    // Returns 0 if the whole script ran, 1 if a command was invalid, 2 if the arguments were wrong.
    public static int Run(string[] args, GameCatalog catalog, TextWriter output)
    {
        // The board views draw on Console.Out, so point it at `output` while the script runs.
        TextWriter previous = Console.Out;
        Console.SetOut(output);
        try { return RunScript(args, catalog, output); }
        finally { Console.SetOut(previous); }
    }

    private static int RunScript(string[] args, GameCatalog catalog, TextWriter output)
    {
        string? game = null, variant = null, script = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--game" && i + 1 < args.Length) game = args[++i];
            else if (args[i] == "--variant" && i + 1 < args.Length) variant = args[++i];
            else script = args[i];
        }

        if (game == null || variant == null || script == null)
        {
            output.WriteLine("Usage: dotnet run -- --game <gomoku|reversi> --variant <name> \"<comma-separated moves>\"");
            return 2;
        }

        IGameFactory factory;
        try { factory = catalog.Resolve(game, variant); }
        catch (ArgumentException ex)
        {
            output.WriteLine(ex.Message);
            return 2;
        }

        Game g = factory.CreateGame(new GameSetup(GameMode.HvH, AiLevel.Dumb));
        g.View.AutoRedraw = false;                       // only the final board is drawn
        // A finished Fog game would reveal every stone; the CLI always prints the active player's view.
        if (g.View is FogView fog) fog.RevealOnGameOver = false;
        var io = ScriptedGameIO.FromCsv(script);
        GameOutcome outcome = g.Play(io);

        foreach (string line in io.Output) output.WriteLine(line);
        if (outcome.Reason == ExitReason.InvalidScriptCommand)
            output.WriteLine($"Stopped at command {io.CommandsRead}: '{io.LastCommand}'.");

        g.View.Render();
        Board board = g.State.Board;
        string counts = string.Join("  ", g.State.Players.Select(p =>
            $"{p.Symbol}={board.AllPositions().Count(pos => board[pos]?.Owner == p)}"));
        output.WriteLine($"Active player: {g.State.Current.Name} ({g.State.Current.Symbol})   Counts: {counts}");
        output.WriteLine(DescribeResult(g.Result));
        output.WriteLine(g.Result.IsOver ? "Game over." : "Game in progress.");

        return outcome.Reason == ExitReason.InvalidScriptCommand ? 1 : 0;
    }

    private static string DescribeResult(GameResult result) => result.Kind switch
    {
        GameResultKind.Win => $"Result: {result.Winner!.Name} ({result.Winner.Symbol}) wins: {result.Reason}.",
        GameResultKind.Draw => $"Result: draw: {result.Reason}.",
        _ => "Result: in progress.",
    };
}
