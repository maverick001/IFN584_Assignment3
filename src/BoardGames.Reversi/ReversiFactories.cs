using BoardGames.Core;

namespace BoardGames.Reversi;

// Factories compose the shared rules with variant-specific strategies.
public abstract class ReversiFactoryBase : GameFactoryBase
{
    public override Board CreateBoard() => new(8, 8);
    protected abstract ReversiObjective Objective { get; }
    public override IComputerStrategy CreateComputerStrategy(AiLevel level) =>
        level == AiLevel.Smarter ? new SmarterReversiStrategy(Objective) : base.CreateComputerStrategy(level);
    // Task 4 can extend the text and replace the inherited Dumb placeholder.
    public override IHelpProvider CreateHelp() => new BasicHelpProvider(
        "Place a disk on an empty cell to flank one or more opposing disks between your disks " +
        "in any of eight straight directions. All flanked disks flip. PASS is allowed only if " +
        "you have no legal placement. Play ends when neither player can move or the board is full. " +
        (Objective switch
        {
            ReversiObjective.Misere => "The player with fewer disks wins; equal counts draw.",
            ReversiObjective.Corners => "Owning three corners wins immediately. Otherwise, at the end the player with more disks wins; equal counts draw.",
            _ => "The player with more disks wins; equal counts draw.",
        }));
    protected override Game Assemble(GameParts parts, GameSetup setup) => new ReversiGame(parts, setup);
}

public sealed class StandardReversiFactory : ReversiFactoryBase
{
    protected override ReversiObjective Objective => ReversiObjective.Majority;
    public override IWinStrategy CreateWinStrategy() => new MajorityWin();
    public override GameDescriptor Descriptor { get; } = new("reversi", "Reversi", "standard", "Standard Reversi");
}

public sealed class AntiReversiFactory : ReversiFactoryBase
{
    protected override ReversiObjective Objective => ReversiObjective.Misere;
    public override IWinStrategy CreateWinStrategy() => new MisereWin();
    public override GameDescriptor Descriptor { get; } = new("reversi", "Reversi", "anti", "Anti-Reversi (misère)");
}

public sealed class CornerReversiFactory : ReversiFactoryBase
{
    protected override ReversiObjective Objective => ReversiObjective.Corners;
    public override IWinStrategy CreateWinStrategy() => new CornerDominanceWin();
    public override GameDescriptor Descriptor { get; } = new("reversi", "Reversi", "corner", "Corner Reversi");
}
