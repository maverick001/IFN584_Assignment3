using BoardGames.Core;

namespace BoardGames.Gomoku;

// Dumb AI: plays a legal move chosen completely at random from the supplied list.
// The list holds Ordinary, Heavy and Eraser placements, so any of them can be picked.
public sealed class DumbGomokuStrategy : IComputerStrategy
{
    public PlaceInput Choose(GameState state, Player me, IReadOnlyList<PlaceInput> legal)
    {
        if (legal.Count == 0) throw new ArgumentException("A strategy needs legal placements.", nameof(legal));
        return legal[Random.Shared.Next(legal.Count)];
    }
}
