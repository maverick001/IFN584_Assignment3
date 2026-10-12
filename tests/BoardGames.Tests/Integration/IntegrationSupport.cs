using BoardGames.App;
using BoardGames.Core;
using BoardGames.Gomoku;

namespace BoardGames.Tests.Integration;

// Helpers shared by the cross-game integration tests (Gomoku and Reversi through the real App wiring).
internal static class IntegrationSupport
{
    internal static Game NewGame(string family, string variant, GameMode mode = GameMode.HvH, AiLevel level = AiLevel.Smarter)
    {
        Game game = CatalogSetup.Build().Resolve(family, variant).CreateGame(new GameSetup(mode, level));
        game.View.AutoRedraw = false;
        return game;
    }

    internal static string TempFile() =>
        Path.Combine(Path.GetTempPath(), "ifn584-integration-" + Guid.NewGuid().ToString("N") + ".json");

    // What the Fog view shows for the player whose turn it is, as ten strings of ten characters.
    internal static string[] FogGrid(Game game)
    {
        var writer = new StringWriter();
        var view = new FogView(writer) { AutoRedraw = false };
        view.Attach(game);
        view.Render();
        return ParseGrid(writer.ToString());
    }

    // Reads the board rows out of rendered text: a row is its number followed by one symbol per column.
    internal static string[] ParseGrid(string text) =>
        text.Split('\n')
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(tokens => tokens.Length == 11 && int.TryParse(tokens[0], out _) && !char.IsDigit(tokens[1][0]))
            .Select(tokens => string.Concat(tokens.Skip(1)))
            .ToArray();
}

// A human who plays the first suitable move from the game's own legal list, so a long game can run without a script.
// Every fourth move it uses a Heavy stone and every fifth an Eraser, whenever the game offers one.
internal sealed class AutoHumanIO : IGameIO
{
    private readonly Game _game;
    private readonly int _moves;
    private int _given;

    internal AutoHumanIO(Game game, int moves)
    {
        _game = game;
        _moves = moves;
    }

    public bool IsScripted => false;
    public List<string> Output { get; } = new();
    public void WriteLine(string text) => Output.Add(text);

    public string? ReadLine(string prompt)
    {
        Output.Add(prompt);
        if (_given++ >= _moves) return "quit";
        var legal = _game.GetLegalMoves(_game.State.Current);
        if (legal.Count == 0) return "PASS";
        string preferred = _given % 5 == 0 ? "E" : _given % 4 == 0 ? "H" : legal[0].Code;
        PlaceInput pick = legal.FirstOrDefault(m => m.Code == preferred) ?? legal[0];
        return $"{pick.Code}{pick.At.Row}:{pick.At.Col}";
    }
}
