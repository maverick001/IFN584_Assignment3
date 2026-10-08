using System.Text;

namespace BoardGames.Core;

// Draws the game (Observer pattern: the view observes the game). Attach subscribes to Game.TurnChanged,
// which the game raises once after every move, undo, redo and load, so one undo (two commands) redraws once.
// A fog view picks what to show from game.State.Current.
public interface IBoardView
{
    void Attach(Game game);

    // Draws the board now.
    void Render();

    // true (default): redraw on every TurnChanged. false: only draw when Render() is called.
    // CLI test mode turns it off and calls Render() once, for the final board.
    bool AutoRedraw { get; set; }
}

// Produces the text for the in-game help command, different for every variant.
public interface IHelpProvider
{
    string GetHelp(Game game);
}

// The plain console board. Reuse it for any variant that shows the true board;
// a fog variant writes its own IBoardView that hides cells.
// Pass a TextWriter to draw somewhere other than the console (tests do).
public sealed class ConsoleBoardView : IBoardView
{
    private readonly TextWriter? _output;
    private Game? _game;

    public ConsoleBoardView(TextWriter? output = null) => _output = output;

    public bool AutoRedraw { get; set; } = true;

    public void Attach(Game game)
    {
        _game = game;
        game.TurnChanged += (_, _) =>
        {
            if (AutoRedraw) Render();
        };
    }

    public void Render()
    {
        if (_game == null) return;

        Board board = _game.State.Board;
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.Append("   ");
        for (int c = 1; c <= board.Cols; c++) sb.Append($"{c,3}");
        sb.AppendLine();
        for (int r = 1; r <= board.Rows; r++)
        {
            sb.Append($"{r,3}");
            for (int c = 1; c <= board.Cols; c++)
                sb.Append($"{board[new Position(r, c)]?.Symbol ?? '.',3}");
            sb.AppendLine();
        }
        (_output ?? Console.Out).Write(sb.ToString());
    }
}

// A ready-made help text: family, variant and board size, the variant's rules (the text you pass in),
// the special stones left, and the commands that make sense for this family. Reuse it and just write the rules.
public sealed class BasicHelpProvider : IHelpProvider
{
    private readonly string _rules;

    public BasicHelpProvider(string rules) => _rules = rules;

    public string GetHelp(Game game)
    {
        var d = game.Descriptor;
        Board board = game.State.Board;
        var sb = new StringBuilder();

        sb.AppendLine($"{d.FamilyName}: {d.VariantName} ({board.Rows}x{board.Cols} board)");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine(_rules);

        foreach (Player p in game.State.Players.Where(p => p.Inventory.Count > 0))
        {
            string left = string.Join(", ", p.Inventory.Select(kv => $"{kv.Key}: {kv.Value}"));
            sb.AppendLine($"{p.Name} ({p.Symbol}) has left: {left}");
        }

        sb.AppendLine();
        sb.AppendLine("Commands (rows and columns start at 1):");
        if (d.FamilyKey == "reversi")
        {
            sb.AppendLine("  P3:4        place a disk at row 3, column 4");
            sb.AppendLine("  PASS        pass your turn (only when you have no legal move)");
        }
        else
        {
            sb.AppendLine("  O5:3        place an Ordinary stone at row 5, column 3");
            if (game.State.Players.Any(p => p.Inventory.ContainsKey(PieceKind.Heavy)))
                sb.AppendLine("  H3:3        place a Heavy stone (it cannot be erased)");
            if (game.State.Players.Any(p => p.Inventory.ContainsKey(PieceKind.Eraser)))
                sb.AppendLine("  E3:3        use an Eraser on an opponent Ordinary stone");
        }
        sb.AppendLine("  undo        take back your last turn (both players' moves)");
        sb.AppendLine("  redo        replay a turn you took back");
        sb.AppendLine("  save <file> save the game      load <file> load a saved game");
        sb.AppendLine("  help        show this text     quit        leave the game");
        return sb.ToString().TrimEnd();
    }
}
