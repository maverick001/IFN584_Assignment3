using BoardGames.Core;

namespace BoardGames.Reversi;

// A placement and ALL its flips form one atomic board change. Retaining both
// colours makes Undo an exact inverse; redo reuses the same changes.
public sealed class ReversiPlaceCommand : IMoveCommand
{
    private readonly Position _at;
    private IReadOnlyList<CellChange>? _changes;

    public ReversiPlaceCommand(Player actor, Position at)
    {
        Actor = actor;
        _at = at;
    }

    public Player Actor { get; }
    public Position At => _at;
    public IReadOnlyList<CellChange> Changes => _changes ?? Array.Empty<CellChange>();

    public void Execute(GameState state)
    {
        if (_changes == null)
        {
            IReadOnlyList<Position> flips = ReversiRules.GetFlips(state.Board, Actor, _at);
            if (flips.Count == 0)
                throw new InvalidOperationException("A Reversi placement must flank at least one opponent disk.");
            var changes = new List<CellChange>
            {
                new(_at, null, new Piece(Actor, PieceKind.Disk)),
            };
            changes.AddRange(flips.Select(p => new CellChange(p, state.Board[p], new Piece(Actor, PieceKind.Disk))));
            _changes = changes.AsReadOnly();
        }
        state.Board.Apply(_changes);
    }

    public void Undo(GameState state)
    {
        if (_changes == null) throw new InvalidOperationException("The placement has not been executed.");
        state.Board.Apply(_changes.Select(change => change.Inverse()));
    }

    public MoveRecord ToRecord() => new(Actor.Index, "P", _at.Row, _at.Col);
}
