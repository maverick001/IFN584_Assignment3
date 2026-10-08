using BoardGames.Core;

namespace BoardGames.Gomoku;

// PLACEHOLDER for Task 2 (C). It plays, so the menu works, but it has no win rule, no Heavy or Eraser stones
// and no fog. Replace the bodies; keep the shape: Validate, CreateCommand, GetLegalMoves (and IsTerminal if needed).
// Look at PlacePieceCommand for the simplest command, and write EraserCommand the same way.
public sealed class GomokuGame : Game
{
    public GomokuGame(GameParts parts, GameSetup setup) : base(parts, setup) { }

    public override ValidationResult Validate(MoveInput move)
    {
        // Gomoku-family codes are O, H and E. PASS and P belong to Reversi.
        if (move is not PlaceInput place || place.Code != "O")
            return ValidationResult.Fail("Only O<row>:<col> is available yet (placeholder). PASS and P are Reversi moves.");

        Board board = State.Board;
        if (!board.InBounds(place.At))
            return ValidationResult.Fail($"{place.At} is off the board: rows and columns go from 1 to {board.Rows}.");
        if (board[place.At] != null)
            return ValidationResult.Fail($"Cell {place.At} is already taken.");
        return ValidationResult.Ok;
    }

    protected override IMoveCommand CreateCommand(PlaceInput move) =>
        new PlacePieceCommand(State.Current, move.Code, move.At, PieceKind.Ordinary);

    public override IReadOnlyList<PlaceInput> GetLegalMoves(Player player) =>
        State.Board.AllPositions()
            .Where(pos => State.Board[pos] == null)
            .Select(pos => new PlaceInput("O", pos))
            .ToList();
}
