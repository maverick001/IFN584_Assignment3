using BoardGames.Core;
using BoardGames.Gomoku;
using BoardGames.Reversi;

namespace BoardGames.App;

// The one place that lists every game. To add a game or variant, add one Register line.
public static class CatalogSetup
{
    public static GameCatalog Build()
    {
        var catalog = new GameCatalog();

        catalog.Register(new StandardGomokuFactory());
        catalog.Register(new GomokuPlusFactory());
        catalog.Register(new GomokuFogFactory());

        catalog.Register(new StandardReversiFactory());
        catalog.Register(new AntiReversiFactory());
        catalog.Register(new CornerReversiFactory());

        return catalog;
    }
}
