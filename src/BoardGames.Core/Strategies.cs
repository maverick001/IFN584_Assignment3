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

// Decides who has won (Strategy pattern). Called after EVERY move, and again with isTerminal = true when
// the game cannot continue (Game.IsTerminal). When isTerminal is true you must return Win or Draw.
// Examples: FiveInARowWin, MajorityWin, MisereWin, CornerDominanceWin.
public interface IWinStrategy
{
    GameResult Evaluate(GameState state, bool isTerminal);
}

// Picks the computer's move (Strategy pattern): Dumb (random) or Smarter.
// It only sees the GameState (never the whole Game) and the list of legal placements.
// legal is never empty and the answer must come from it. When the computer has no legal move,
// the game loop passes for it, so Choose is not called.
public interface IComputerStrategy
{
    PlaceInput Choose(GameState state, Player me, IReadOnlyList<PlaceInput> legal);
}

// Placeholder AI: always plays the first legal move. D replaces it with the random Dumb AI.
public sealed class FirstLegalMoveStrategy : IComputerStrategy
{
    public PlaceInput Choose(GameState state, Player me, IReadOnlyList<PlaceInput> legal) => legal[0];
}

// Placeholder win rule: nobody wins, the game is a draw once it cannot continue. Real variants replace it.
public sealed class DrawOnTerminalStrategy : IWinStrategy
{
    public GameResult Evaluate(GameState state, bool isTerminal) =>
        isTerminal ? GameResult.Draw("the game cannot continue") : GameResult.InProgress;
}
