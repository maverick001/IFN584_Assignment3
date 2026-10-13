namespace BoardGames.Core;

// What a save file holds: the setup plus the list of moves.
public sealed record GameSaveData(
    int Version,
    string FamilyKey, string VariantKey,           // the two keys GameCatalog.Resolve needs
    GameMode Mode, AiLevel Level,
    string[] BoardSnapshot,                        // Board.ToSnapshot(), used only to verify the replay
    Dictionary<int, Dictionary<PieceKind, int>> Inventories,
    List<MoveRecord> Moves);

// Writes and reads save files (JSON).
public interface IGameRepository
{
    void Save(Game game, string path);
    Game Load(string path, GameCatalog catalog);
}
