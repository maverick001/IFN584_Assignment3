namespace BoardGames.Core;

// A board cell. Row and Col are 1-based, exactly as in the spec: "O5:3" = row 5, column 3.
public readonly record struct Position(int Row, int Col)
{
    public override string ToString() => $"{Row}:{Col}";
}

// What a piece is. Eraser is used for inventories and commands (an Eraser removes a stone).
public enum PieceKind { Ordinary, Heavy, Eraser, Disk }

// One piece on the board. Immutable: a Reversi flip does not change a disk, it replaces it
// (see CellChange), which keeps undo trivial.
public sealed record Piece(Player Owner, PieceKind Kind)
{
    // The character the console view and the save-file snapshot use for this piece.
    public char Symbol => Kind switch
    {
        PieceKind.Heavy => char.ToLowerInvariant(Owner.Symbol),   // Heavy shows as x / o
        PieceKind.Eraser => 'E',
        _ => Owner.Symbol,                                        // Ordinary and Disk show as X / O
    };
}

// A player. Index 0 = P1 = X (moves first), index 1 = P2 = O.
public abstract class Player
{
    protected Player(int index, char symbol, string name, Dictionary<PieceKind, int>? inventory)
    {
        Index = index;
        Symbol = symbol;
        Name = name;
        Inventory = inventory ?? new Dictionary<PieceKind, int>();
    }

    public int Index { get; }
    public char Symbol { get; }
    public string Name { get; }

    // Special stones left, e.g. GomokuPlus: Heavy = 2, Eraser = 2. Kinds that are not listed are unlimited.
    public Dictionary<PieceKind, int> Inventory { get; }

    public abstract bool IsHuman { get; }

    // How many stones of this kind are left (0 if the kind is not tracked).
    public int Left(PieceKind kind) => Inventory.TryGetValue(kind, out int n) ? n : 0;
}

public sealed class HumanPlayer : Player
{
    public HumanPlayer(int index, char symbol, string name, Dictionary<PieceKind, int>? inventory = null)
        : base(index, symbol, name, inventory) { }

    public override bool IsHuman => true;
}

// A computer player. It owns an IComputerStrategy (Dumb or Smarter) that picks its moves.
public sealed class ComputerPlayer : Player
{
    public ComputerPlayer(int index, char symbol, string name, IComputerStrategy strategy,
                          Dictionary<PieceKind, int>? inventory = null)
        : base(index, symbol, name, inventory)
    {
        Strategy = strategy;
    }

    public IComputerStrategy Strategy { get; }

    public override bool IsHuman => false;
}

// One cell changing from Before to After (null = empty). Commands describe their effect as a list
// of CellChanges, so Undo is just applying Inverse() of each change.
public sealed record CellChange(Position At, Piece? Before, Piece? After)
{
    public CellChange Inverse() => new(At, After, Before);
}

public sealed class BoardChangedEventArgs : EventArgs
{
    public BoardChangedEventArgs(IReadOnlyList<CellChange> changes) => Changes = changes;
    public IReadOnlyList<CellChange> Changes { get; }
}

// The grid. All changes go through Apply, which raises BoardChanged once per call
// (so a 6-disk Reversi flip is one event, not six). This is the Observer subject for cell-level changes.
public sealed class Board
{
    private readonly Piece?[,] _cells;

    public Board(int rows, int cols)
    {
        if (rows < 1 || cols < 1) throw new ArgumentOutOfRangeException(nameof(rows), "A board needs at least 1 row and 1 column.");
        Rows = rows;
        Cols = cols;
        _cells = new Piece?[rows, cols];
    }

    public int Rows { get; }
    public int Cols { get; }

    public event EventHandler<BoardChangedEventArgs>? BoardChanged;

    public bool InBounds(Position p) => p.Row >= 1 && p.Row <= Rows && p.Col >= 1 && p.Col <= Cols;

    // The piece at p, or null if the cell is empty. Throws if p is off the board (check InBounds first).
    public Piece? this[Position p]
    {
        get
        {
            if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p), $"{p} is outside the {Rows}x{Cols} board.");
            return _cells[p.Row - 1, p.Col - 1];
        }
    }

    public IEnumerable<Position> AllPositions()
    {
        for (int r = 1; r <= Rows; r++)
            for (int c = 1; c <= Cols; c++)
                yield return new Position(r, c);
    }

    // Applies the changes all-or-nothing. Every change must start from what is really on the board
    // (Before), which catches an Undo that does not exactly reverse its Execute.
    public void Apply(IEnumerable<CellChange> changes)
    {
        List<CellChange> list = changes.ToList();

        foreach (CellChange change in list)
        {
            if (!InBounds(change.At))
                throw new ArgumentOutOfRangeException(nameof(changes), $"{change.At} is outside the {Rows}x{Cols} board.");
            if (!Equals(_cells[change.At.Row - 1, change.At.Col - 1], change.Before))
                throw new InvalidOperationException($"The board is out of sync at {change.At}: the change expected a different piece.");
        }

        foreach (CellChange change in list)
            _cells[change.At.Row - 1, change.At.Col - 1] = change.After;

        if (list.Count > 0)
            BoardChanged?.Invoke(this, new BoardChangedEventArgs(list));
    }

    // One string per row, '.' for an empty cell, otherwise Piece.Symbol. Used by the save file to verify a replay.
    public string[] ToSnapshot()
    {
        var rows = new string[Rows];
        for (int r = 1; r <= Rows; r++)
        {
            var chars = new char[Cols];
            for (int c = 1; c <= Cols; c++)
                chars[c - 1] = _cells[r - 1, c - 1]?.Symbol ?? '.';
            rows[r - 1] = new string(chars);
        }
        return rows;
    }
}

// Everything a command or a strategy may look at or change: the board and the two players.
// The active player is derived from MoveCount, so undo, redo, load and PASS can never get out of step.
public sealed class GameState
{
    public GameState(Board board, IReadOnlyList<Player> players)
    {
        if (players.Count != 2) throw new ArgumentException("A game has exactly two players.", nameof(players));
        Board = board;
        Players = players;
    }

    public Board Board { get; }
    public IReadOnlyList<Player> Players { get; }

    // Number of commands applied so far. Only CommandHistory changes it.
    public int MoveCount { get; internal set; }

    // Whose turn it is: P1 on even move counts, P2 on odd ones.
    public Player Current => Players[MoveCount % 2];

    public int TurnNumber => MoveCount / 2 + 1;
}
