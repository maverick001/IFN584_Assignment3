using BoardGames.Core;
using BoardGames.Tests.Support;

namespace BoardGames.Tests.Core;

public class CommandHistoryTests
{
    private readonly Player _x = new HumanPlayer(0, 'X', "P1", new Dictionary<PieceKind, int> { [PieceKind.Heavy] = 2 });
    private readonly Player _o = new HumanPlayer(1, 'O', "P2");
    private readonly GameState _state;
    private readonly CommandHistory _history = new();

    public CommandHistoryTests() => _state = new GameState(new Board(4, 4), new[] { _x, _o });

    private IMoveCommand Place(int row, int col, PieceKind kind = PieceKind.Ordinary) =>
        new PlacePieceCommand(_state.Current, kind == PieceKind.Heavy ? "H" : "O", new Position(row, col), kind);

    private void PlayMoves(int count)
    {
        // cell number n (0-based) -> row n/4+1, column n%4+1: always an empty cell
        for (int n = 0; n < count; n++) _history.Do(Place(n / 4 + 1, n % 4 + 1), _state);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void UndoTurn_is_refused_with_fewer_than_two_commands(int played)   // confirmed by the teacher: undo is refused with 'Nothing to undo'
    {
        PlayMoves(played);
        string[] before = _state.Board.ToSnapshot();

        Assert.False(_history.CanUndo);
        Assert.False(_history.UndoTurn(_state));

        Assert.Equal(before, _state.Board.ToSnapshot());
        Assert.Equal(played, _history.Count);
    }

    [Fact]
    public void A_new_move_after_undo_clears_the_redo_history()
    {
        PlayMoves(2);
        _history.UndoTurn(_state);
        Assert.True(_history.CanRedo);

        _history.Do(Place(4, 4), _state);

        Assert.False(_history.CanRedo);
        Assert.False(_history.RedoTurn(_state));
    }

    [Fact]
    public void Many_undos_then_as_many_redos_restore_the_original()
    {
        PlayMoves(10);
        string[] original = _state.Board.ToSnapshot();

        for (int i = 0; i < 5; i++) Assert.True(_history.UndoTurn(_state));
        Assert.Equal(0, _history.Count);
        Assert.Equal(0, _state.MoveCount);
        Assert.All(_state.Board.ToSnapshot(), row => Assert.Equal("....", row));

        for (int i = 0; i < 5; i++) Assert.True(_history.RedoTurn(_state));
        Assert.Equal(original, _state.Board.ToSnapshot());
        Assert.Equal(10, _history.Count);
        Assert.Equal(_history.Count, _state.MoveCount);   // the two counters stay in step
    }

    [Fact]
    public void A_pass_counts_as_one_of_the_two_commands_of_a_turn()   // confirmed by the teacher: PASS is an ordinary command
    {
        _history.Do(Place(1, 1), _state);                       // X places
        _history.Do(new PassCommand(_state.Current), _state);   // O passes

        Assert.True(_history.UndoTurn(_state));                 // both are reverted together

        Assert.Equal(0, _history.Count);
        Assert.All(_state.Board.ToSnapshot(), row => Assert.Equal("....", row));
    }

    [Fact]
    public void Commands_exactly_undo_themselves_including_special_stones()
    {
        CommandAssert.ExecuteThenUndoRestoresState(_state, Place(2, 2));
        CommandAssert.ExecuteThenUndoRestoresState(_state, Place(3, 3, PieceKind.Heavy));   // the Heavy count comes back too
        CommandAssert.ExecuteThenUndoRestoresState(_state, new PassCommand(_x));
    }
}

public class GameStateTests
{
    private readonly Player _x = new HumanPlayer(0, 'X', "P1");
    private readonly Player _o = new HumanPlayer(1, 'O', "P2");

    private GameState NewState() => new(new Board(4, 4), new[] { _x, _o });

    [Fact]
    public void Current_alternates_P1_then_P2_and_a_pass_hands_the_turn_over()
    {
        GameState state = NewState();
        var history = new CommandHistory();

        Assert.Equal(_x, state.Current);
        history.Do(new PlacePieceCommand(state.Current, "O", new Position(1, 1), PieceKind.Ordinary), state);
        Assert.Equal(_o, state.Current);
        history.Do(new PassCommand(state.Current), state);
        Assert.Equal(_x, state.Current);
    }

    [Fact]
    public void After_UndoTurn_the_requester_is_current_again()
    {
        GameState state = NewState();
        var history = new CommandHistory();
        for (int n = 0; n < 3; n++)
            history.Do(new PlacePieceCommand(state.Current, "O", new Position(1, n + 1), PieceKind.Ordinary), state);
        Assert.Equal(_o, state.Current);          // P2 is to move and asks for undo

        history.UndoTurn(state);

        Assert.Equal(_o, state.Current);          // the turn returns to P2 (spec 4.2)
    }
}
