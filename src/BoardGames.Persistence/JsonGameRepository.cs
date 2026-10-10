using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoardGames.Core;

namespace BoardGames.Persistence;

// Persistence depends on Core contracts only, so it can load any registered
// family without knowing about Reversi, special stones or Fog rendering.
public sealed class JsonGameRepository : IGameRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    public void Save(Game game, string path)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string destination = Path.GetFullPath(path);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(game.ToSaveData(), Options), new UTF8Encoding(false));
            // Write completely before replacing a previous save in the same directory.
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public Game Load(string path, GameCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        GameSaveData data;
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            RequireFields(document.RootElement);
            if (document.RootElement.GetProperty("Moves").ValueKind == JsonValueKind.Array)
                foreach (JsonElement move in document.RootElement.GetProperty("Moves").EnumerateArray())
                    RequireProperties(move, ["PlayerIndex", "Code", "Row", "Col"]);
            data = document.RootElement.Deserialize<GameSaveData>(Options)
                ?? throw new InvalidDataException("The save file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The save file contains invalid JSON or field values.", ex);
        }

        ValidateData(data);
        Game game;
        try
        {
            game = catalog.Resolve(data.FamilyKey, data.VariantKey).CreateGame(new GameSetup(data.Mode, data.Level));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException("The save file names an unregistered game variant.", ex);
        }

        bool redraw = game.View.AutoRedraw;
        game.View.AutoRedraw = false;
        try
        {
            // Replay one record at a time to detect moves AFTER a terminal result.
            // Core's Replay builds the real commands and therefore the undo stack.
            foreach (MoveRecord record in data.Moves)
            {
                if (game.Result.IsOver)
                    throw new InvalidDataException("The save file contains a move after the game ended.");
                game.Replay(new[] { record });
            }
            if (data.Moves.Count == 0) game.Replay(Array.Empty<MoveRecord>());

            if (!game.State.Board.ToSnapshot().SequenceEqual(data.BoardSnapshot))
                throw new InvalidDataException("The saved board does not match the replayed history.");
            if (data.Inventories.Count != game.State.Players.Count
                || game.State.Players.Any(p => !data.Inventories.TryGetValue(p.Index, out var inventory)
                    || p.Inventory.Count != inventory.Count
                    || p.Inventory.Any(kv => !inventory.TryGetValue(kv.Key, out int count) || kv.Value != count)))
                throw new InvalidDataException("The saved inventories do not match the replayed history.");
        }
        finally { game.View.AutoRedraw = redraw; }

        game.Repository = this;
        return game;
    }

    private static void RequireFields(JsonElement root)
        => RequireProperties(root, ["Version", "FamilyKey", "VariantKey", "Mode", "Level", "BoardSnapshot", "Inventories", "Moves"]);

    private static void RequireProperties(JsonElement root, string[] required)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("The save file must be a JSON object.");
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in root.EnumerateObject())
            if (!fields.Add(property.Name))
                throw new InvalidDataException($"Duplicate save field '{property.Name}'.");
        foreach (string name in required)
            if (!fields.Contains(name)) throw new InvalidDataException($"Missing save field '{name}'.");
    }

    private static void ValidateData(GameSaveData data)
    {
        if (data.Version != 1) throw new InvalidDataException($"Unsupported save version {data.Version}.");
        if (string.IsNullOrWhiteSpace(data.FamilyKey) || string.IsNullOrWhiteSpace(data.VariantKey)
            || !Enum.IsDefined(data.Mode) || !Enum.IsDefined(data.Level)
            || data.BoardSnapshot == null || data.BoardSnapshot.Any(row => row == null)
            || data.Inventories == null || data.Moves == null)
            throw new InvalidDataException("The save file has missing or invalid game data.");
        if (data.Inventories.Any(kv => kv.Value == null
            || kv.Value.Any(item => !Enum.IsDefined(item.Key) || item.Value < 0)))
            throw new InvalidDataException("The save file has an invalid inventory.");
        foreach (MoveRecord record in data.Moves)
        {
            if (record == null || record.PlayerIndex is < 0 or > 1
                || record.Code is not ("P" or "O" or "H" or "E" or "PASS")
                || (record.Code == "PASS" ? record.Row != null || record.Col != null
                    : record.Row == null || record.Col == null))
                throw new InvalidDataException("The save file has a malformed move record.");
        }
    }
}
