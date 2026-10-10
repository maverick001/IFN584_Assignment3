using BoardGames.Core;

namespace BoardGames.Gomoku;

// PLACEHOLDER for Task 2 (C): what the three Gomoku variants share (10x10 board, GomokuGame).
// Replace CreateWinStrategy with FiveInARowWin and CreateHelp with real rules text.
public abstract class GomokuFactoryBase : GameFactoryBase
{
    // Task 3 integration only; Task 2 owns game rules, inventory and views.
    // Task 4 replaces the inherited Dumb placeholder with its random strategy.
    public override IComputerStrategy CreateComputerStrategy(AiLevel level) =>
        level == AiLevel.Smarter ? new SmarterGomokuStrategy() : base.CreateComputerStrategy(level);
    public override Board CreateBoard() => new(10, 10);
    public override IWinStrategy CreateWinStrategy() => new DrawOnTerminalStrategy();   // TODO C: FiveInARowWin
    public override IHelpProvider CreateHelp() => new BasicHelpProvider("PLACEHOLDER: place stones; the real five-in-a-row rules come with Task 2.");
    protected override Game Assemble(GameParts parts, GameSetup setup) => new GomokuGame(parts, setup);
}

public sealed class StandardGomokuFactory : GomokuFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("gomoku", "Gomoku", "standard", "Standard Gomoku");
}

public sealed class GomokuPlusFactory : GomokuFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("gomoku", "Gomoku", "plus", "GomokuPlus");

    // Confirmed by the teacher: each player starts with 2 Heavy and 2 Eraser stones.
    protected override Dictionary<PieceKind, int> CreateInventory(int playerIndex) =>
        new() { [PieceKind.Heavy] = 2, [PieceKind.Eraser] = 2 };
}

public sealed class GomokuFogFactory : GomokuFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("gomoku", "Gomoku", "fog", "GomokuFog");

    // TODO C: override CreateView() to return the FogView (an IBoardView that hides cells).
}
