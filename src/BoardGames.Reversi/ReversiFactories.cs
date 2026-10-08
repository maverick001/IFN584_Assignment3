using BoardGames.Core;

namespace BoardGames.Reversi;

// PLACEHOLDER for Task 3 (B): what the three Reversi variants share (8x8 board, ReversiGame).
// Each real factory returns its own win rule (MajorityWin, MisereWin, CornerDominanceWin),
// its help text and the Smarter AI for CreateComputerStrategy.
public abstract class ReversiFactoryBase : GameFactoryBase
{
    public override Board CreateBoard() => new(8, 8);
    public override IWinStrategy CreateWinStrategy() => new DrawOnTerminalStrategy();   // TODO B: the variant's win rule
    public override IHelpProvider CreateHelp() => new BasicHelpProvider("PLACEHOLDER: place disks; the real flanking rules come with Task 3.");
    protected override Game Assemble(GameParts parts, GameSetup setup) => new ReversiGame(parts, setup);
}

public sealed class StandardReversiFactory : ReversiFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("reversi", "Reversi", "standard", "Standard Reversi");
}

public sealed class AntiReversiFactory : ReversiFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("reversi", "Reversi", "anti", "Anti-Reversi (misère)");
}

public sealed class CornerReversiFactory : ReversiFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("reversi", "Reversi", "corner", "Corner Reversi");
}
