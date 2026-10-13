using BoardGames.Core;

namespace BoardGames.Reversi;

// Dumb AI: plays a legal move chosen completely at random from the supplied list.
// When there is no legal move the game loop issues the PASS, so Choose is not called.
public sealed class DumbReversiStrategy : IComputerStrategy
{
    public PlaceInput Choose(GameState state, Player me, IReadOnlyList<PlaceInput> legal)
    {
        if (legal.Count == 0) throw new ArgumentException("The game loop handles PASS.", nameof(legal));
        return legal[Random.Shared.Next(legal.Count)];
    }
}
