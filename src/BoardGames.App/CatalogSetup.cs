using BoardGames.Core;
using BoardGames.Gomoku;
using BoardGames.Reversi;

namespace BoardGames.App;

// The one place that lists every game. To add a game or variant, add one Register line;
// the menu, the CLI and the loop need no change.
public static class CatalogSetup
{
    public static GameCatalog Build()
    {
        var catalog = new GameCatalog();

        // Task 2 (C): replace these placeholders with the real Gomoku factories.
        catalog.Register(new StandardGomokuFactory());
        catalog.Register(new GomokuPlusFactory());
        catalog.Register(new GomokuFogFactory());

        // Task 3 (B): replace these placeholders with the real Reversi factories.
        catalog.Register(new StandardReversiFactory());
        catalog.Register(new AntiReversiFactory());
        catalog.Register(new CornerReversiFactory());

        return catalog;
    }
}
