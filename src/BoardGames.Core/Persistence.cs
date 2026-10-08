namespace BoardGames.Core;

// What a save file holds: the setup plus the list of moves. Loading creates a fresh game from the setup
// and replays the moves (Game.Replay), so the board, inventories, active player and undo history are rebuilt
// by the normal game code. BoardSnapshot and Inventories are only there to double-check the replay.
public sealed record GameSaveData(
    int Version,
    string FamilyKey, string VariantKey,           // the two keys GameCatalog.Resolve needs
    GameMode Mode, AiLevel Level,
    string[] BoardSnapshot,                        // Board.ToSnapshot(), used only to verify the replay
    Dictionary<int, Dictionary<PieceKind, int>> Inventories,
    List<MoveRecord> Moves);

// Writes and reads save files (JSON). Core has no JSON code on purpose: B implements this in Task 3.
// Save: write game.ToSaveData(). Load: catalog.Resolve(data.FamilyKey, data.VariantKey).CreateGame(new GameSetup(data.Mode, data.Level)),
// then game.Replay(data.Moves). Redo history is not saved (confirmed by the teacher): it is empty after a load.
public interface IGameRepository
{
    void Save(Game game, string path);
    Game Load(string path, GameCatalog catalog);
}
