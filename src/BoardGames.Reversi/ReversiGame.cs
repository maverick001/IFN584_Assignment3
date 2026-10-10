using BoardGames.Core;

namespace BoardGames.Reversi;

// All three variants use the same placement, flanking and PASS rules.
// The factory supplies the victory objective and the computer strategy.
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
        return ReversiRules.GetFlips(board, State.Current, place.At).Count > 0
            ? ValidationResult.Ok
            : ValidationResult.Fail("A Reversi placement must flank at least one opponent disk.");
    }

    protected override IMoveCommand CreateCommand(PlaceInput move) =>
        new ReversiPlaceCommand(State.Current, move.At);

    public override IReadOnlyList<PlaceInput> GetLegalMoves(Player player) =>
        ReversiRules.GetLegalMoves(State.Board, player);
}
