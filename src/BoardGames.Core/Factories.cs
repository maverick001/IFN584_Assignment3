namespace BoardGames.Core;

// Names a variant. The two keys are lower-case, unique and are what the CLI and the save file use:
// --game reversi --variant corner  =  ("reversi", "corner").
public sealed record GameDescriptor(string FamilyKey, string FamilyName, string VariantKey, string VariantName);

// The parts a factory builds for one game: the board, the two players and the variant's win rule, view and help.
// GameFactoryBase.CreateGame builds it and hands it to Assemble.
public sealed record GameParts(Board Board, IReadOnlyList<Player> Players, IWinStrategy Win,
                               IBoardView View, IHelpProvider Help, GameDescriptor Descriptor);

// ABSTRACT FACTORY: one factory = one variant = one consistent family of parts
// (board, win rule, AI, view, help), so a variant can never get another variant's parts.
public interface IGameFactory
{
    GameDescriptor Descriptor { get; }
    Board CreateBoard();
    IWinStrategy CreateWinStrategy();
    IComputerStrategy CreateComputerStrategy(AiLevel level);
    IBoardView CreateView();
    IHelpProvider CreateHelp();
    Game CreateGame(GameSetup setup);
}

// Does the boring part once: CreateGame builds the players and the parts in a fixed order (a small Template Method),
// then asks the subclass to Assemble the right Game class (a FACTORY METHOD).
// A concrete factory only has to say what is different about its variant.
public abstract class GameFactoryBase : IGameFactory
{
    public abstract GameDescriptor Descriptor { get; }
    public abstract Board CreateBoard();
    public abstract IWinStrategy CreateWinStrategy();
    public abstract IHelpProvider CreateHelp();

    // FACTORY METHOD: return the variant's Game, e.g. new ReversiGame(parts, setup).
    protected abstract Game Assemble(GameParts parts, GameSetup setup);

    // FACTORY METHOD with a default: the special stones each player starts with. GomokuPlus overrides it (Heavy 2, Eraser 2).
    protected virtual Dictionary<PieceKind, int> CreateInventory(int playerIndex) => new();

    // The computer's brain. The default is a placeholder; Dumb and Smarter AI replace it.
    public virtual IComputerStrategy CreateComputerStrategy(AiLevel level) => new FirstLegalMoveStrategy();

    // The default is the plain console board. GomokuFog overrides it with the fog view.
    public virtual IBoardView CreateView() => new ConsoleBoardView();

    public Game CreateGame(GameSetup setup)
    {
        // P1 is the human and always X. P2 is the second human or the computer.
        var players = new List<Player>
        {
            new HumanPlayer(0, 'X', "Player 1", CreateInventory(0)),
            setup.Mode == GameMode.HvC
                ? new ComputerPlayer(1, 'O', "Computer", CreateComputerStrategy(setup.Level), CreateInventory(1))
                : new HumanPlayer(1, 'O', "Player 2", CreateInventory(1)),
        };

        var parts = new GameParts(CreateBoard(), players, CreateWinStrategy(), CreateView(), CreateHelp(), Descriptor);
        return Assemble(parts, setup);
    }
}

// COMPOSITE: the catalog is a tree of nodes. A family (Gomoku, Reversi) holds variants; a variant wraps one factory.
// Every node answers the same three questions, so the menu and the CLI never ask "is this a family or a variant?":
// Choose  - let the player pick a playable variant (used by the main menu)
// Find    - look a path like reversi/corner up (used by the CLI and load)
// Paths   - list every playable path (used in error messages)
public interface IGameNode
{
    string Key { get; }                                   // lower-case lookup key
    string Name { get; }                                  // menu text
    IReadOnlyList<IGameNode> Children { get; }            // empty for a variant

    // Family: list the children, read a choice and recurse. Variant: return its factory. Null = the player went back.
    IGameFactory? Choose(IGameIO io);

    // Family: match path[0] to a child and recurse. Variant: path empty = its factory, otherwise null.
    IGameFactory? Find(ReadOnlySpan<string> path);

    // Every playable path below this node. prefix is the path to this node ("" for the root).
    IEnumerable<string> Paths(string prefix);
}

// Composite: a node with children.
public sealed class GameFamilyNode : IGameNode
{
    private readonly List<IGameNode> _children = new();

    public GameFamilyNode(string key, string name)
    {
        Key = key;
        Name = name;
    }

    public string Key { get; }
    public string Name { get; }
    public IReadOnlyList<IGameNode> Children => _children;

    // Adds a child. Two children with the same key are not allowed.
    public void Add(IGameNode child)
    {
        if (_children.Any(c => SameKey(c.Key, child.Key)))
            throw new ArgumentException($"'{Name}' already has a game called '{child.Key}'.");
        _children.Add(child);
    }

    public IGameFactory? Choose(IGameIO io)
    {
        while (true)
        {
            io.WriteLine($"{Name}:");
            for (int i = 0; i < _children.Count; i++)
                io.WriteLine($"  {i + 1}) {_children[i].Name}");
            io.WriteLine("  0) Back");

            string? line = io.ReadLine("> ")?.Trim();
            if (line == null || line == "0") return null;

            if (int.TryParse(line, out int number) && number >= 1 && number <= _children.Count)
            {
                IGameFactory? chosen = _children[number - 1].Choose(io);
                if (chosen != null) return chosen;
                continue;   // the player went back from the next level: show this level again
            }
            io.WriteLine("Please enter one of the numbers shown.");
        }
    }

    public IGameFactory? Find(ReadOnlySpan<string> path)
    {
        if (path.IsEmpty) return null;
        foreach (IGameNode child in _children)
            if (SameKey(child.Key, path[0]))
                return child.Find(path.Slice(1));
        return null;
    }

    public IEnumerable<string> Paths(string prefix) =>
        _children.SelectMany(c => c.Paths(prefix.Length == 0 ? c.Key : $"{prefix}/{c.Key}"));

    internal static bool SameKey(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

// Leaf: a playable variant. It just wraps its factory.
public sealed class GameVariantNode : IGameNode
{
    private readonly IGameFactory _factory;

    public GameVariantNode(IGameFactory factory) => _factory = factory;

    public string Key => _factory.Descriptor.VariantKey;
    public string Name => _factory.Descriptor.VariantName;
    public IReadOnlyList<IGameNode> Children => Array.Empty<IGameNode>();

    public IGameFactory? Choose(IGameIO io) => _factory;
    public IGameFactory? Find(ReadOnlySpan<string> path) => path.IsEmpty ? _factory : null;
    public IEnumerable<string> Paths(string prefix) => new[] { prefix };
}

// The registry of every game. It owns the root of the Composite tree (it is not a node itself).
// To add a game or variant: Register its factory, nothing else changes.
public sealed class GameCatalog
{
    public GameFamilyNode Root { get; } = new("games", "Games");

    // Adds the variant under its family, creating the family node the first time. A duplicate variant throws.
    public void Register(IGameFactory factory)
    {
        GameDescriptor d = factory.Descriptor;
        GameFamilyNode? family = Root.Children.OfType<GameFamilyNode>()
            .FirstOrDefault(f => GameFamilyNode.SameKey(f.Key, d.FamilyKey));

        if (family == null)
        {
            family = new GameFamilyNode(d.FamilyKey, d.FamilyName);
            Root.Add(family);
        }
        family.Add(new GameVariantNode(factory));
    }

    // Finds a factory by path, for example Resolve("reversi", "corner"). Throws and lists the valid paths if there is none.
    public IGameFactory Resolve(params string[] path) =>
        Root.Find(path) ?? throw new ArgumentException(
            $"Unknown game '{string.Join("/", path)}'. Valid choices: {string.Join(", ", Root.Paths(""))}.");
}
