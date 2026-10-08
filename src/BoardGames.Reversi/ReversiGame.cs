using BoardGames.Core;

namespace BoardGames.Reversi;

// PLACEHOLDER for Task 3 (B). It plays, so the menu works, but any empty cell is "legal" and nothing is flanked or flipped.
// Replace Validate and GetLegalMoves with the real flanking rules, and write ReversiPlaceCommand
// (it must remember the flipped disks so Undo can flip them back, see PlacePieceCommand for the shape).
// You do NOT write pass code: the loop builds the PassCommand. You only decide in Validate when a PASS is allowed.
public sealed class ReversiGame : Game
{
    public ReversiGame(GameParts parts, GameSetup setup) : base(parts, setup)
    {
        // The four centre disks: (4,4)=O, (4,5)=X, (5,4)=X, (5,5)=O. X is P1, O is P2.
        Player x = State.Players[0];
        Player o = State.Players[1];
        State.Board.Apply(new[]
        {
            new CellChange(new Position(4, 4), null, new Piece(o, PieceKind.Disk)),
            new CellChange(new Position(4, 5), null, new Piece(x, PieceKind.Disk)),
            new CellChange(new Position(5, 4), null, new Piece(x, PieceKind.Disk)),
            new CellChange(new Position(5, 5), null, new Piece(o, PieceKind.Disk)),
        });
    }

    public override ValidationResult Validate(MoveInput move)
    {
        if (move is PassInput)
            return GetLegalMoves(State.Current).Count == 0
                ? ValidationResult.Ok
                : ValidationResult.Fail("You can only pass when you have no legal move.");

        // Reversi-family code is P. O, H and E belong to Gomoku.
        if (move is not PlaceInput place || place.Code != "P")
            return ValidationResult.Fail("Reversi moves look like P<row>:<col>, or PASS when you cannot move.");

        Board board = State.Board;
        if (!board.InBounds(place.At))
            return ValidationResult.Fail($"{place.At} is off the board: rows and columns go from 1 to {board.Rows}.");
        if (board[place.At] != null)
            return ValidationResult.Fail($"Cell {place.At} is already taken.");
        return ValidationResult.Ok;   // TODO B: it must also flank at least one opponent disk
    }

    protected override IMoveCommand CreateCommand(PlaceInput move) =>
        new PlacePieceCommand(State.Current, move.Code, move.At, PieceKind.Disk);   // TODO B: ReversiPlaceCommand that also flips

    public override IReadOnlyList<PlaceInput> GetLegalMoves(Player player) =>
        State.Board.AllPositions()
            .Where(pos => State.Board[pos] == null)
            .Select(pos => new PlaceInput("P", pos))
            .ToList();
}
