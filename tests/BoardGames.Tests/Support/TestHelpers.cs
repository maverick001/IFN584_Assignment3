using BoardGames.Core;

namespace BoardGames.Tests.Support;

// An interactive (not scripted) IO fed from a list: for testing undo, redo, help, save and load.
public sealed class TestIO : IGameIO
{
    private readonly Queue<string> _lines;
    public TestIO(params string[] lines) => _lines = new Queue<string>(lines);
    public bool IsScripted => false;
    public List<string> Output { get; } = new();
    public string? ReadLine(string prompt)
    {
        Output.Add(prompt);   // prompts are kept so tests can see whose turn it is
        return _lines.Count > 0 ? _lines.Dequeue() : null;
    }
    public void WriteLine(string text) => Output.Add(text);
    public string AllOutput => string.Join("\n", Output);
}

// Shared helper: every IMoveCommand needs a test that Execute then Undo restores the exact state.
public static class CommandAssert
{
    public static void ExecuteThenUndoRestoresState(GameState state, IMoveCommand command)
    {
        string[] boardBefore = state.Board.ToSnapshot();
        string inventoryBefore = Inventories(state);

        command.Execute(state);
        command.Undo(state);

        Assert.Equal(boardBefore, state.Board.ToSnapshot());
        Assert.Equal(inventoryBefore, Inventories(state));
    }

    private static string Inventories(GameState state) =>
        string.Join(";", state.Players.Select(p => string.Join(",", p.Inventory.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))));
}
