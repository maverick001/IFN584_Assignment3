using BoardGames.Core;

namespace BoardGames.Reversi;

public enum ReversiObjective { Majority, Misere, Corners }

public sealed class SmarterReversiStrategy : IComputerStrategy
{
    public SmarterReversiStrategy(ReversiObjective objective) => Objective = objective;
    public ReversiObjective Objective { get; }

    public PlaceInput Choose(GameState state, Player me, IReadOnlyList<PlaceInput> legal)
    {
        if (legal.Count == 0) throw new ArgumentException("The game loop handles PASS.", nameof(legal));
        // Stable ties make tactical choices reproducible in demonstrations and tests.
        return legal.OrderByDescending(move => Score(state, me, move)).First();
    }

    private int Score(GameState state, Player me, PlaceInput move)
    {
        int flips = ReversiRules.GetFlips(state.Board, me, move.At).Count;
        bool corner = ReversiRules.IsCorner(state.Board, move.At);
        if (Objective == ReversiObjective.Majority) return flips;
        if (Objective == ReversiObjective.Misere)
            return -flips - (corner ? 10_000 : 0);

        Board after = ReversiRules.AfterMove(state.Board, me, move.At);
        if (ReversiRules.CountCorners(after, me) >= 3) return 1_000_000 + flips;

        Player opponent = state.Players.Single(p => p != me);
        var opponentCorners = ReversiRules.GetLegalMoves(after, opponent)
            .Where(m => ReversiRules.IsCorner(after, m.At)).ToList();
        bool immediateLoss = ReversiRules.CountCorners(after, opponent) >= 2 && opponentCorners.Count > 0;
        // Avoid an opponent's third corner first, then seek our own corners,
        // reduce available opponent corners, and finally maximise flips.
        return (immediateLoss ? -100_000 : 0) + (corner ? 10_000 : 0)
            - opponentCorners.Count * 1_000 + flips;
    }
}
