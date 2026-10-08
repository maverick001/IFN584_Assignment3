using BoardGames.Core;

namespace BoardGames.App;

// New game / Load game / Quit. It never mentions "family" or "variant": the catalog tree does the choosing
// (catalog.Root.Choose), so new games appear in the menu by themselves.
public sealed class MainMenu
{
    private readonly GameCatalog _catalog;
    private readonly IGameIO _io;
    private readonly IGameRepository? _repository;

    // repository: Saves and loads files (B writes it). Null = save and load are unavailable.
    public MainMenu(GameCatalog catalog, IGameIO io, IGameRepository? repository = null)
    {
        _catalog = catalog;
        _io = io;
        _repository = repository;
    }

    public void Run()
    {
        while (true)
        {
            _io.WriteLine("");
            _io.WriteLine("=== Board Game Framework ===");
            _io.WriteLine("  1) New game");
            _io.WriteLine("  2) Load game");
            _io.WriteLine("  3) Quit");

            switch (_io.ReadLine("> ")?.Trim())
            {
                case "1": NewGame(); break;
                case "2": LoadGame(); break;
                case "3":
                case null: return;
                default: _io.WriteLine("Please enter 1, 2 or 3."); break;
            }
        }
    }

    private void NewGame()
    {
        IGameFactory? factory = _catalog.Root.Choose(_io);
        if (factory == null) return;

        GameSetup? setup = ChooseSetup();
        if (setup == null) return;

        PlayLoop(factory.CreateGame(setup));
    }

    private GameSetup? ChooseSetup()
    {
        _io.WriteLine("Mode:");
        _io.WriteLine("  1) Human vs Human");
        _io.WriteLine("  2) Human vs Computer");
        _io.WriteLine("  0) Back");
        switch (_io.ReadLine("> ")?.Trim())
        {
            case "1": return new GameSetup(GameMode.HvH, AiLevel.Dumb);
            case "2": break;
            default: return null;
        }

        _io.WriteLine("Computer level:");
        _io.WriteLine("  1) Dumb");
        _io.WriteLine("  2) Smarter");
        _io.WriteLine("  0) Back");
        return _io.ReadLine("> ")?.Trim() switch
        {
            "1" => new GameSetup(GameMode.HvC, AiLevel.Dumb),
            "2" => new GameSetup(GameMode.HvC, AiLevel.Smarter),
            _ => null,
        };
    }

    private void LoadGame()
    {
        string? path = _io.ReadLine("File to load: ")?.Trim();
        if (string.IsNullOrEmpty(path)) return;

        Game? game = TryLoad(path);
        if (game != null) PlayLoop(game);
    }

    // Plays the game. If the player types load <file>, switches to that game. A failed load keeps the current game.
    private void PlayLoop(Game game)
    {
        game.Repository = _repository;
        while (true)
        {
            GameOutcome outcome = game.Play(_io);
            if (outcome.Reason != ExitReason.LoadRequested) return;

            Game? loaded = TryLoad(outcome.LoadPath!);
            if (loaded != null)
            {
                loaded.Repository = _repository;
                game = loaded;
            }
            else
            {
                _io.WriteLine("Continuing the current game.");
            }
        }
    }

    private Game? TryLoad(string path)
    {
        if (_repository == null)
        {
            _io.WriteLine("Loading is not available yet.");
            return null;
        }

        try { return _repository.Load(path, _catalog); }
        catch (Exception ex)
        {
            _io.WriteLine($"Could not load '{path}': {ex.Message}");
            return null;
        }
    }
}
