using BoardGames.Core;

namespace BoardGames.Gomoku;

public sealed class EraserCommand : IMoveCommand
{
    private readonly Position _at;
    private Piece? _erasedPiece;

    public Player Actor{get;}

    public EraserCommand(Player actor, Position at)
    {
        Actor = actor;
        _at = at;

    }

    public void Execute(GameState state)
    {
        _erasedPiece = state.Board[_at];

        state.Board.Apply(new[] {new CellChange(_at, _erasedPiece, null)});

        Actor.Inventory[PieceKind.Eraser]--;
    }

    public void Undo(GameState state)
    {
        state.Board.Apply(new[] {new CellChange(_at, null, _erasedPiece)});

        Actor.Inventory[PieceKind.Eraser]++;
    }

    public MoveRecord ToRecord()
    {
        return new(Actor.Index, "E", _at.Row, _at.Col);
    }
}