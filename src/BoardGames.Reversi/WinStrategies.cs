using BoardGames.Core;

namespace BoardGames.Reversi;

public sealed class MajorityWin : IWinStrategy
{
    public GameResult Evaluate(GameState state, bool isTerminal) =>
        isTerminal ? DiskCountResult.Evaluate(state, fewestWins: false) : GameResult.InProgress;
}

public sealed class MisereWin : IWinStrategy
{
    public GameResult Evaluate(GameState state, bool isTerminal) =>
        isTerminal ? DiskCountResult.Evaluate(state, fewestWins: true) : GameResult.InProgress;
}

public sealed class CornerDominanceWin : IWinStrategy
{
    public GameResult Evaluate(GameState state, bool isTerminal)
    {
        foreach (Player player in state.Players)
            if (ReversiRules.CountCorners(state.Board, player) >= 3)
                return GameResult.Win(player, "three-corner dominance");
        return isTerminal ? DiskCountResult.Evaluate(state, fewestWins: false) : GameResult.InProgress;
    }
}

internal static class DiskCountResult
{
    internal static GameResult Evaluate(GameState state, bool fewestWins)
    {
        Player x = state.Players[0], o = state.Players[1];
        int nx = ReversiRules.CountDisks(state.Board, x), no = ReversiRules.CountDisks(state.Board, o);
        string counts = $"disk counts {x.Symbol}={nx}, {o.Symbol}={no}";
        if (nx == no) return GameResult.Draw(counts);
        Player winner = (fewestWins ? nx < no : nx > no) ? x : o;
        return GameResult.Win(winner, $"{(fewestWins ? "fewest" : "most")} disks; {counts}");
    }
}
