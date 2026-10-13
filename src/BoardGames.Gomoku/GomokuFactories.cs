using BoardGames.Core;

namespace BoardGames.Gomoku;

public abstract class GomokuFactoryBase : GameFactoryBase
{
    // Task 3 integration only; Task 2 owns game rules, inventory and views.
    public override IComputerStrategy CreateComputerStrategy(AiLevel level) =>
        level == AiLevel.Smarter ? new SmarterGomokuStrategy() : new DumbGomokuStrategy();
    public override Board CreateBoard() => new(10, 10);
    public override IWinStrategy CreateWinStrategy() => new FiveInARowWin();
    public override IHelpProvider CreateHelp() => new BasicHelpProvider("PLACEHOLDER: place stones; the real five-in-a-row rules come with Task 2.");
    protected override Game Assemble(GameParts parts, GameSetup setup) => new GomokuGame(parts, setup);
}

public sealed class StandardGomokuFactory : GomokuFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("gomoku", "Gomoku", "standard", "Standard Gomoku");

    public override IHelpProvider CreateHelp()
    {
        return new BasicHelpProvider (
            "Players take turns placing Ordinary stones. " +
            "Each player has an unlimited amount of stones. " +
            "To win a player must line up 5 of their own stones vertically, horizontally or diagonally. "
        );
    }
}

public sealed class GomokuPlusFactory : GomokuFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("gomoku", "Gomoku", "plus", "GomokuPlus");

      protected override Dictionary<PieceKind, int> CreateInventory(int playerIndex) =>
        new() { [PieceKind.Heavy] = 2, [PieceKind.Eraser] = 2 };

    protected override Game Assemble(GameParts parts, GameSetup setup)
    {
       return new GomokuPlus(parts, setup);
    }

    public override IHelpProvider CreateHelp()
    {
        return new BasicHelpProvider(
            "Players take turns placing Ordinary or Heavy stones on Empty cells, " +
            "or erasing opponets stone. " +
            "Heavy stones cannot be erased. " + 
            "Each player starts with 2 Heavy stones and 2 Eraser stones. " +
            "Ordinary stones are unlimited. " +
            "To win a player must line up 5 of their own stones vertically, horizontally or diagonally. "
        );
    }


}

public sealed class GomokuFogFactory : GomokuFactoryBase
{
    public override GameDescriptor Descriptor { get; } = new("gomoku", "Gomoku", "fog", "GomokuFog");

    public override IBoardView CreateView() => new FogView();

    protected override Game Assemble(GameParts parts, GameSetup setup)
    {
       return new GomokuFog(parts, setup);
    }
    public override IHelpProvider CreateHelp()
    {
        return new BasicHelpProvider (
            "Players take turns placing Ordinary stones. " +
            "Players can only see board locations that contain their own stones, " +
            "or are adjacent to their stones. " +
            "Each player has an unlimited amount of stones. " +
            "To win a player must line up 5 of their own stones vertically, horizontally or diagonally. "
        );
    }

}
