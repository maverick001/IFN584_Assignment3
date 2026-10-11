using BoardGames.Core;

namespace BoardGames.Gomoku;

public sealed class GomokuGame : Game
{
    public GomokuGame(GameParts parts, GameSetup setup) : base(parts, setup) { }

    public override ValidationResult Validate(MoveInput move)
    {
        if (move is not PlaceInput place || place.Code != "O")
            return ValidationResult.Fail("Only O<row>:<col> is valid for Gomoku.");

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

public sealed class GomokuFog : Game
{
    public GomokuFog(GameParts parts, GameSetup setup) : base(parts, setup){}

    public override ValidationResult Validate(MoveInput move)
    {
        if (move is not PlaceInput place || place.Code != "O")
            return ValidationResult.Fail("Only O<row>:<col> is valid for Gomoku Fog");

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

public sealed class GomokuPlus : Game
{
    public GomokuPlus(GameParts parts, GameSetup setup) : base(parts, setup){}

    public override ValidationResult Validate(MoveInput move)
    {
        if(move is not PlaceInput place || place.Code != "O" && place.Code != "H" && place.Code != "E")
        {
            return ValidationResult.Fail("Only O/H/E<row>:<col> is valid for GomokuPlus");
        }

        Board board = State.Board;
        if(!board.InBounds(place.At))
        {
            return ValidationResult.Fail($"{place.At} is off the board: rows and colums go from 1 to {board.Rows}.");
        }
        
        if(place.Code == "E")
        {
            if(State.Current.Left(PieceKind.Eraser) <= 0)
            {
                return ValidationResult.Fail("You have no more erasers left");
            }
            else if(board[place.At] == null)
            {
                return ValidationResult.Fail($"{place.At} is empty and cannot be erased.");
            }
            else if(board[place.At].Kind == PieceKind.Heavy)
            {
                return ValidationResult.Fail($"{place.At} contains a heavy stone and cannot erased.");
            }
            else if(board[place.At].Owner.Index == State.Current.Index)
            {
                return ValidationResult.Fail($"{place.At} Is your own stone and cannot be erased.");
            }
        }
        else if(place.Code == "H" && State.Current.Left(PieceKind.Heavy) <= 0)
        {
            return ValidationResult.Fail("You have no more heavy stones left.");
        }
        else if(board[place.At] != null)
        {
            return ValidationResult.Fail($"Cell {place.At} is already taken.");
        }

        return ValidationResult.Ok;
    }

    protected override IMoveCommand CreateCommand(PlaceInput move)
    {
        if(move.Code == "E")
        {
            return new EraserCommand(State.Current, move.At);
        }
        else if(move.Code == "H")
        {
            return new PlacePieceCommand(State.Current, move.Code, move.At, PieceKind.Heavy);
        }
        else
        {
            return new PlacePieceCommand(State.Current, move.Code, move.At, PieceKind.Ordinary);
        }
    }

    public override IReadOnlyList<PlaceInput> GetLegalMoves(Player player)
    {
        Board board = State.Board;
        List<PlaceInput> moves = new List<PlaceInput>();

        foreach (Position position in board.AllPositions())
        {
            Piece? piece = board[position];

            if (piece == null)
            {
                moves.Add(new PlaceInput("O", position));

                if(player.Left(PieceKind.Heavy) > 0)
                {
                    moves.Add(new PlaceInput("H", position));
                }
            }
            else if (player.Left(PieceKind.Eraser) > 0 && piece.Kind != PieceKind.Heavy && piece.Owner.Index != player.Index)
            {
                moves.Add(new PlaceInput("E", position));
            }
        }

        return moves;
    }
}