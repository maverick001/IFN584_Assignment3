namespace BoardGames.Core;

public enum GameResultKind { InProgress, Win, Draw }

// The outcome after a move. Use the helpers: GameResult.InProgress, GameResult.Win(...), GameResult.Draw(...).
public sealed record GameResult(GameResultKind Kind, Player? Winner, string Reason)
{
    public static GameResult InProgress => new(GameResultKind.InProgress, null, "");
    public static GameResult Win(Player winner, string reason) => new(GameResultKind.Win, winner, reason);
    public static GameResult Draw(string reason) => new(GameResultKind.Draw, null, reason);

    public bool IsOver => Kind != GameResultKind.InProgress;
}

// Decides who has won (Strategy pattern).
public interface IWinStrategy
{
    GameResult Evaluate(GameState state, bool isTerminal);
}

// Picks the computer's move (Strategy pattern): Dumb or Smarter.
public interface IComputerStrategy
{
    PlaceInput Choose(GameState state, Player me, IReadOnlyList<PlaceInput> legal);
}

// Default AI of GameFactoryBase: always plays the first legal move. The Gomoku and Reversi factories use their own Dumb and Smarter strategies.
public sealed class FirstLegalMoveStrategy : IComputerStrategy
{
    public PlaceInput Choose(GameState state, Player me, IReadOnlyList<PlaceInput> legal) => legal[0];
}

// Default win rule: nobody wins, the game is a draw once it cannot continue. Real variants replace it.
public sealed class DrawOnTerminalStrategy : IWinStrategy
{
    public GameResult Evaluate(GameState state, bool isTerminal) =>
        isTerminal ? GameResult.Draw("the game cannot continue") : GameResult.InProgress;
}
