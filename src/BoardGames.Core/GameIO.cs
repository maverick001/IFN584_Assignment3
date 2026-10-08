namespace BoardGames.Core;

// How the game talks to the outside: the console for a person, a list of commands for a script or a test.
// The same game loop serves interactive play, CLI test mode and unit tests.
public interface IGameIO
{
    // Shows the prompt (if interactive) and returns the next line, or null when input has ended.
    string? ReadLine(string prompt);

    void WriteLine(string text);

    // true for a script: invalid input stops the run, and in-game commands such as undo are not allowed.
    bool IsScripted { get; }
}

// Keyboard and screen.
public sealed class ConsoleGameIO : IGameIO
{
    public bool IsScripted => false;

    public string? ReadLine(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine();
    }

    public void WriteLine(string text) => Console.WriteLine(text);
}

// Feeds a fixed list of commands to the game (CLI test mode and tests) and collects what the game prints.
public sealed class ScriptedGameIO : IGameIO
{
    private readonly Queue<string> _commands;

    public ScriptedGameIO(IEnumerable<string> commands) => _commands = new Queue<string>(commands);

    // Builds the IO from the comma-separated script of the CLI: "O5:3,O6:3,O5:4".
    public static ScriptedGameIO FromCsv(string script) =>
        new(script.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public bool IsScripted => true;

    // Everything the game wrote, in order.
    public List<string> Output { get; } = new();

    // How many commands have been handed to the game, and the last one. Used to report the position of a bad command.
    public int CommandsRead { get; private set; }
    public string? LastCommand { get; private set; }

    public string? ReadLine(string prompt)
    {
        if (_commands.Count == 0) return null;
        CommandsRead++;
        LastCommand = _commands.Dequeue();
        return LastCommand;
    }

    public void WriteLine(string text) => Output.Add(text);
}
