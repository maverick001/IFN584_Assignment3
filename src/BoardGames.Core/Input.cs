using System.Text.RegularExpressions;

namespace BoardGames.Core;

// What the parser returns for one line the player typed.
public abstract record ParsedInput;

// A move that takes a turn: either a placement or a pass.
public abstract record MoveInput(string Code) : ParsedInput;

// A placement. Code is O, H, E (Gomoku family) or P (Reversi family): O5:3, H3:3, E3:3, P3:4.
public sealed record PlaceInput(string Code, Position At) : MoveInput(Code);

// PASS. It has no position, so it is its own type instead of a placement with a dummy position.
public sealed record PassInput() : MoveInput("PASS");

public enum ControlKind { Undo, Redo, Save, Load, Help, Quit }

// An in-game command. It never takes a turn. Arg is the file name for save and load.
public sealed record ControlInput(ControlKind Kind, string? Arg = null) : ParsedInput;

// Turns one typed line into a ParsedInput. Parsing is separate from the game rules, so another
// syntax could be plugged in without touching a game.
public interface IInputParser
{
    // Returns the input, or null with a message in error. Never throws on bad text.
    ParsedInput? Parse(string line, out string error);
}

// The spec's syntax: O5:3  H3:3  E3:3  P3:4  PASS  undo  redo  save <file>  load <file>  help  quit.
// Case does not matter and extra spaces are fine. It only checks the SHAPE of a move (letter, row, column);
// whether the game accepts that letter, or the cell, is the game's job (Game.Validate).
public sealed class StandardInputParser : IInputParser
{
    private static readonly Regex MovePattern =
        new(@"^([OHEP])\s*(\d+)\s*:\s*(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public ParsedInput? Parse(string line, out string error)
    {
        error = "";
        string text = (line ?? "").Trim();
        if (text.Length == 0)
        {
            error = "Please type a command (type help to see them).";
            return null;
        }

        string[] words = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string word = words[0].ToLowerInvariant();
        string? arg = words.Length > 1 ? words[1] : null;

        switch (word)
        {
            case "pass": return NoArgument(word, arg, new PassInput(), out error);
            case "undo": return NoArgument(word, arg, new ControlInput(ControlKind.Undo), out error);
            case "redo": return NoArgument(word, arg, new ControlInput(ControlKind.Redo), out error);
            case "help": return NoArgument(word, arg, new ControlInput(ControlKind.Help), out error);
            case "quit": return NoArgument(word, arg, new ControlInput(ControlKind.Quit), out error);
            case "save": return WithFileName(word, arg, ControlKind.Save, out error);
            case "load": return WithFileName(word, arg, ControlKind.Load, out error);
        }

        Match m = MovePattern.Match(text);
        if (m.Success)
        {
            if (!int.TryParse(m.Groups[2].Value, out int row) || !int.TryParse(m.Groups[3].Value, out int col) || row < 1 || col < 1)
            {
                error = "Rows and columns start at 1 (example: O5:3).";
                return null;
            }
            return new PlaceInput(m.Groups[1].Value.ToUpperInvariant(), new Position(row, col));
        }

        error = $"I do not understand '{text}'. Try O5:3 (Gomoku), P3:4 (Reversi), PASS, undo, redo, save <file>, load <file>, help or quit.";
        return null;
    }

    private static ParsedInput? NoArgument(string word, string? arg, ParsedInput result, out string error)
    {
        error = "";
        if (arg == null) return result;
        error = $"'{word}' does not take anything after it.";
        return null;
    }

    private static ParsedInput? WithFileName(string word, string? arg, ControlKind kind, out string error)
    {
        error = "";
        if (arg != null) return new ControlInput(kind, arg);
        error = $"'{word}' needs a file name, for example: {word} game1.json";
        return null;
    }
}
