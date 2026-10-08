namespace BoardGames.Core;

// The shared game loop. Play() is a TEMPLATE METHOD: the order of the steps is fixed here and cannot be
// overridden. A game variant only fills in the steps below that differ (the "hooks"):
// Validate, CreateCommand, GetLegalMoves  (required)   and   IsTerminal, OnMoveApplied  (optional).
// To add a game: derive from Game, override those, write one IMoveCommand per kind of move, and a factory.
public abstract class Game
{
    private GameResult _result = GameResult.InProgress;

    protected Game(GameParts parts, GameSetup setup)
    {
        State = new GameState(parts.Board, parts.Players);
        History = new CommandHistory();
        View = parts.View;
        Win = parts.Win;
        Help = parts.Help;
        Descriptor = parts.Descriptor;
        Setup = setup;
        View.Attach(this);
    }

    public GameState State { get; }
    public CommandHistory History { get; }
    public IBoardView View { get; }
    public IWinStrategy Win { get; }
    public IHelpProvider Help { get; }
    public GameDescriptor Descriptor { get; }
    public GameSetup Setup { get; }

    // How typed text becomes input. Replace it to support another syntax.
    public IInputParser Parser { get; set; } = new StandardInputParser();

    // Does the saving and loading (B writes it). Null = the save command says it is unavailable.
    public IGameRepository? Repository { get; set; }

    // The latest result: InProgress until somebody wins or the game is drawn.
    public GameResult Result => _result;

    // Raised once after every move, undo, redo and replay. Views redraw on it (Observer pattern).
    public event EventHandler? TurnChanged;

    // ------------------------------------------------------------------ the HOOKS a variant fills in

    // Is this move allowed for State.Current? Also reject the other family's codes (Gomoku rejects P and PASS). Never change the state here.
    public abstract ValidationResult Validate(MoveInput move);

    // Builds the command for a placement that Validate accepted. PASS never gets here: the loop builds a PassCommand itself.
    protected abstract IMoveCommand CreateCommand(PlaceInput move);

    // Every placement the player may make now (never PASS). An empty list means "must pass".
    public abstract IReadOnlyList<PlaceInput> GetLegalMoves(Player player);

    // True when the game cannot continue. Default: neither player has a legal move (Gomoku board full, Reversi nobody can move).
    protected virtual bool IsTerminal() =>
        GetLegalMoves(State.Players[0]).Count == 0 && GetLegalMoves(State.Players[1]).Count == 0;

    // Called after each command is applied, before the win check. Most games need nothing here.
    protected virtual void OnMoveApplied(IMoveCommand command) { }

    // ------------------------------------------------------------------ the TEMPLATE METHOD

    // Plays until the game ends, the player quits or loads, or a script ends.
    // Steps: draw the board, then loop { computer or human takes a turn -> ApplyMove -> win check -> TurnChanged }.
    public GameOutcome Play(IGameIO io)
    {
        if (View.AutoRedraw) View.Render();
        while (true)
        {
            GameOutcome? stop = State.Current is ComputerPlayer computer
                ? TakeComputerTurn(io, computer)
                : TakeHumanTurn(io, State.Current);
            if (stop != null) return stop;
        }
    }

    // The one shared step that makes a move: Validate, build the command, run it in History, call OnMoveApplied.
    // Play and Replay both use it, so loading a game runs exactly the same code as playing it.
    // Nothing changes if the move is invalid.
    private ValidationResult ApplyMove(MoveInput move)
    {
        ValidationResult check = Validate(move);
        if (!check.IsValid) return check;

        IMoveCommand command = move is PlaceInput place
            ? CreateCommand(place)
            : new PassCommand(State.Current);
        History.Do(command, State);
        OnMoveApplied(command);
        return ValidationResult.Ok;
    }

    private GameOutcome? TakeComputerTurn(IGameIO io, ComputerPlayer computer)
    {
        IReadOnlyList<PlaceInput> legal = GetLegalMoves(computer);
        MoveInput move = legal.Count == 0 ? new PassInput() : computer.Strategy.Choose(State, computer, legal);

        ValidationResult check = ApplyMove(move);
        if (!check.IsValid)
            throw new InvalidOperationException($"The strategy of {computer.Name} chose an invalid move ({Describe(move)}): {check.Message}");

        io.WriteLine($"{computer.Name} plays {Describe(move)}.");
        return AfterStateChange(io);
    }

    private GameOutcome? TakeHumanTurn(IGameIO io, Player player)
    {
        // A human is never passed automatically: they have to type PASS (confirmed by the teacher).
        if (GetLegalMoves(player).Count == 0)
            io.WriteLine($"{player.Name} has no legal move. Type PASS to pass your turn.");

        string? line = io.ReadLine($"{player.Name} ({player.Symbol}) > ");
        if (line == null)
            return new GameOutcome(io.IsScripted ? ExitReason.ScriptEnded : ExitReason.Quit, _result, null);

        ParsedInput? input = Parser.Parse(line, out string error);
        if (input == null) return Reject(io, error);

        if (input is ControlInput control) return HandleControl(io, control);

        ValidationResult check = ApplyMove((MoveInput)input);
        if (!check.IsValid) return Reject(io, check.Message);
        return AfterStateChange(io);
    }

    // Shows why the input was refused. A player is asked again (the state is unchanged); a script stops at the first invalid command.
    private GameOutcome? Reject(IGameIO io, string reason)
    {
        io.WriteLine(reason);
        return io.IsScripted ? new GameOutcome(ExitReason.InvalidScriptCommand, _result, null) : null;
    }

    private GameOutcome? HandleControl(IGameIO io, ControlInput control)
    {
        // Scripts hold moves and PASS only (confirmed by the teacher).
        if (io.IsScripted)
            return Reject(io, $"'{control.Kind.ToString().ToLowerInvariant()}' is not allowed in a script: scripts hold moves and PASS only.");

        switch (control.Kind)
        {
            case ControlKind.Undo:
                if (!History.UndoTurn(State)) { io.WriteLine("Nothing to undo."); return null; }
                return AfterStateChange(io);

            case ControlKind.Redo:
                if (!History.RedoTurn(State)) { io.WriteLine("Nothing to redo."); return null; }
                return AfterStateChange(io);

            case ControlKind.Help:
                io.WriteLine(Help.GetHelp(this));
                return null;

            case ControlKind.Save:
                if (Repository == null) { io.WriteLine("Saving is not available yet."); return null; }
                try
                {
                    Repository.Save(this, control.Arg!);
                    io.WriteLine($"Game saved to {control.Arg}.");
                }
                catch (Exception ex) { io.WriteLine($"Could not save: {ex.Message}"); }
                return null;

            case ControlKind.Load:
                return new GameOutcome(ExitReason.LoadRequested, _result, control.Arg);   // the menu does the loading

            default: // Quit
                return new GameOutcome(ExitReason.Quit, _result, null);
        }
    }

    // After a move, undo or redo: check for a winner, tell the views, and report the result if the game is over.
    private GameOutcome? AfterStateChange(IGameIO io)
    {
        _result = Win.Evaluate(State, IsTerminal());
        TurnChanged?.Invoke(this, EventArgs.Empty);

        if (!_result.IsOver) return null;
        io.WriteLine(Describe(_result));
        return new GameOutcome(ExitReason.Finished, _result, null);
    }

    // ------------------------------------------------------------------ save and load support

    // Rebuilds a game from saved moves (load). Throws InvalidDataException if a record does not fit the game.
    public void Replay(IEnumerable<MoveRecord> records)
    {
        int number = 0;
        foreach (MoveRecord record in records)
        {
            number++;
            if (record.PlayerIndex != State.Current.Index)
                throw new InvalidDataException($"Saved move {number}: it is player {State.Current.Index}'s turn, but the record is for player {record.PlayerIndex}.");

            ValidationResult check = ApplyMove(record.ToInput());
            if (!check.IsValid)
                throw new InvalidDataException($"Saved move {number} ({record.Code}) is not valid: {check.Message}");
        }

        _result = Win.Evaluate(State, IsTerminal());
        TurnChanged?.Invoke(this, EventArgs.Empty);
    }

    // Everything a save file needs (see GameSaveData). B's repository writes it as JSON.
    public GameSaveData ToSaveData() => new(
        Version: 1,
        FamilyKey: Descriptor.FamilyKey,
        VariantKey: Descriptor.VariantKey,
        Mode: Setup.Mode,
        Level: Setup.Level,
        BoardSnapshot: State.Board.ToSnapshot(),
        Inventories: State.Players.ToDictionary(p => p.Index, p => new Dictionary<PieceKind, int>(p.Inventory)),
        Moves: History.Records().ToList());

    // ------------------------------------------------------------------ small helpers

    private static string Describe(MoveInput move) => move switch
    {
        PlaceInput p => $"{p.Code}{p.At.Row}:{p.At.Col}",
        _ => "PASS",
    };

    private static string Describe(GameResult result) => result.Kind == GameResultKind.Win
        ? $"{result.Winner!.Name} ({result.Winner.Symbol}) wins: {result.Reason}."
        : $"The game is a draw: {result.Reason}.";
}

public enum GameMode { HvH, HvC }          // Human vs Human, Human vs Computer (the human is P1 = X and moves first)
public enum AiLevel { Dumb, Smarter }

// What the player chose in the menu.
public sealed record GameSetup(GameMode Mode, AiLevel Level);

// Why Game.Play returned.
public enum ExitReason
{
    Finished,              // a win or a draw
    Quit,                  // the player typed quit
    LoadRequested,         // the player typed load <file>; LoadPath says which
    ScriptEnded,           // a script ran out of commands
    InvalidScriptCommand,  // a script command was invalid, so the run stopped at the first invalid command
}

public sealed record GameOutcome(ExitReason Reason, GameResult Result, string? LoadPath);

// The answer of Game.Validate: either fine, or a message to show the player.
public readonly record struct ValidationResult(bool IsValid, string Message)
{
    public static ValidationResult Ok => new(true, "");
    public static ValidationResult Fail(string message) => new(false, message);
}
