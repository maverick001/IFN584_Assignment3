namespace BoardGames.Core;

// One move as an object (Command pattern). Execute changes the state, Undo reverses it exactly,
// ToRecord is what goes in the save file. Keep whatever Undo needs (flipped cells, erased stone) in fields.
public interface IMoveCommand
{
    Player Actor { get; }
    void Execute(GameState state);
    void Undo(GameState state);
    MoveRecord ToRecord();
}

// One move as it is written in the save file. Code is O, H, E, P or PASS.
// Row and Col are null for PASS (a pass has no position).
public sealed record MoveRecord(int PlayerIndex, string Code, int? Row, int? Col)
{
    // Turns a saved record back into the input the game loop understands. Load uses only this.
    public MoveInput ToInput()
    {
        if (Code == "PASS") return new PassInput();

        if (Row is null || Col is null)
            throw new InvalidDataException($"The saved move '{Code}' has no row and column.");
        return new PlaceInput(Code, new Position(Row.Value, Col.Value));
    }
}

// The undo and redo stacks. One undo or redo moves a whole turn, which is TWO commands
// (both players in HvH, human + computer in HvC). A PASS is an ordinary command and counts as one of the two.
public sealed class CommandHistory
{
    private readonly List<IMoveCommand> _undo = new();   // last item = most recent command
    private readonly List<IMoveCommand> _redo = new();

    // Commands applied so far.
    public int Count => _undo.Count;

    public bool CanUndo => _undo.Count >= 2;
    public bool CanRedo => _redo.Count >= 2;

    // Runs a new command. Playing a new move after an undo clears all redo history.
    public void Do(IMoveCommand command, GameState state)
    {
        command.Execute(state);
        _undo.Add(command);
        _redo.Clear();
        state.MoveCount++;
    }

    // Reverts the last two commands. Returns false (and changes nothing) if fewer than two exist.
    public bool UndoTurn(GameState state)
    {
        if (!CanUndo) return false;

        for (int i = 0; i < 2; i++)
        {
            IMoveCommand command = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            command.Undo(state);
            state.MoveCount--;
            _redo.Add(command);
        }
        return true;
    }

    // Re-applies the last two undone commands, oldest first. Returns false if there is nothing to redo.
    public bool RedoTurn(GameState state)
    {
        if (!CanRedo) return false;

        for (int i = 0; i < 2; i++)
        {
            IMoveCommand command = _redo[^1];
            _redo.RemoveAt(_redo.Count - 1);
            command.Execute(state);
            _undo.Add(command);
            state.MoveCount++;
        }
        return true;
    }

    // The applied commands as save-file records, oldest first.
    public IReadOnlyList<MoveRecord> Records() => _undo.Select(c => c.ToRecord()).ToList();
}

// A pass: changes nothing on the board, but it is still a command, so it takes a turn,
// can be undone and is saved. Game.ApplyMove creates it for every PassInput, so variants never write pass code.
public sealed class PassCommand : IMoveCommand
{
    public PassCommand(Player actor) => Actor = actor;

    public Player Actor { get; }

    public void Execute(GameState state) { }
    public void Undo(GameState state) { }

    public MoveRecord ToRecord() => new(Actor.Index, "PASS", null, null);
}

// The simplest real command: put one piece on an empty cell. Use it as the pattern for your own commands
// (EraserCommand, ReversiPlaceCommand): describe the effect as CellChanges, apply them in Execute,
// apply the inverse in Undo.
// If the piece kind has an entry in the player's Inventory (Heavy, Eraser), Execute uses one up and Undo gives it back.
public sealed class PlacePieceCommand : IMoveCommand
{
    private readonly string _code;
    private readonly Position _at;
    private readonly PieceKind _kind;

    public PlacePieceCommand(Player actor, string code, Position at, PieceKind kind)
    {
        Actor = actor;
        _code = code;
        _at = at;
        _kind = kind;
    }

    public Player Actor { get; }

    public void Execute(GameState state)
    {
        state.Board.Apply(new[] { new CellChange(_at, null, new Piece(Actor, _kind)) });
        if (Actor.Inventory.ContainsKey(_kind)) Actor.Inventory[_kind]--;
    }

    public void Undo(GameState state)
    {
        state.Board.Apply(new[] { new CellChange(_at, new Piece(Actor, _kind), null) });
        if (Actor.Inventory.ContainsKey(_kind)) Actor.Inventory[_kind]++;
    }

    public MoveRecord ToRecord() => new(Actor.Index, _code, _at.Row, _at.Col);
}
