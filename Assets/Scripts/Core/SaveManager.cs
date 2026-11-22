using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SOLEDAR's persistence layer (Phase 4).
///
/// A single hidden GameObject holds all saved state for the session and
/// mirrors it to a JSON file under Application.persistentDataPath.
///
/// Save points: Forgotten Bench rest (full heal + respawn + everything below).
/// Persisted: player health, respawn position, collected keys, opened doors,
/// unlocked abilities (Phase 5), current scene + spawn point (Phase 6).
///
/// Other systems never touch the file directly:
///   - Door.Awake / CollectibleKey.Awake query IsDoorOpen / IsKeyCollected
///     so saved state survives leaving Play mode and restarting the game.
///   - MarkKeyCollected / MarkDoorOpen / MarkAbilityUnlocked update state AND
///     save immediately.
///   - SaveGame() snapshots the player's live state (called on bench rest).
///   - SaveApplier (on the Player) applies the saved state on scene start.
/// </summary>
[DisallowMultipleComponent]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    /// <summary>Raised when an ability is unlocked (ability gates refresh themselves).</summary>
    public static event Action<string> AbilityUnlocked;

    /// <summary>Raised with the new total whenever the dirham balance changes (HUD).</summary>
    public static event Action<int> DirhamsChanged;

    private static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "soledar_save.json"); }
    }

    [Serializable]
    private class SaveData
    {
        public int version = 3;
        public float playerX;
        public float playerY;
        public int health = 5;
        public int maxHealth = 5;
        public string[] collectedKeys = new string[0];
        public string[] openedDoors = new string[0];
        public string[] unlockedAbilities = new string[0];
        public string[] defeatedBosses = new string[0];
        public string[] discoveredRooms = new string[0];
        public string[] collectedItems = new string[0];
        public int ancientDirhams;
        public string currentScene = "";
        public string spawnPointId = "";
    }

    private SaveData data = new SaveData();

    /// <summary>True when a save file exists (fresh session starts from it).</summary>
    public bool HasSave { get; private set; }

    /// <summary>Creates the manager on first use and loads the save file.</summary>
    public static SaveManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("[SaveManager]");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<SaveManager>();
        Instance.LoadFromFile();
        return Instance;
    }

    /* ---------------- queries ---------------- */

    public bool IsKeyCollected(string keyName)
    {
        return Array.IndexOf(data.collectedKeys, keyName) >= 0;
    }

    public bool IsDoorOpen(string doorName)
    {
        return Array.IndexOf(data.openedDoors, doorName) >= 0;
    }

    /// <summary>True when the named ability has been unlocked (Phase 5).</summary>
    public bool HasAbility(string abilityName)
    {
        return Array.IndexOf(data.unlockedAbilities, abilityName) >= 0;
    }

    public bool IsBossDefeated(string bossId) { return Array.IndexOf(data.defeatedBosses, bossId) >= 0; }
    public bool IsRoomDiscovered(string roomId) { return Array.IndexOf(data.discoveredRooms, roomId) >= 0; }
    public bool IsItemCollected(string itemId) { return Array.IndexOf(data.collectedItems, itemId) >= 0; }
    public int AncientDirhams { get { return data.ancientDirhams; } }

    /// <summary>Health recorded in the save (read by SceneFlow on arrival).</summary>
    public int SavedHealth { get { return data.health; } }

    /// <summary>Scene the save belongs to (Phase 6; empty for pre-Phase-6 saves).</summary>
    public string CurrentScene { get { return data.currentScene; } }

    /// <summary>SceneSpawnPoint id the player was last placed at (Phase 6).</summary>
    public string SpawnPointId { get { return data.spawnPointId; } }

    /* ---------------- state changes (auto-save) ---------------- */

    public void MarkKeyCollected(string keyName)
    {
        if (string.IsNullOrEmpty(keyName) || IsKeyCollected(keyName)) return;

        data.collectedKeys = Append(data.collectedKeys, keyName);
        SaveGame($"key '{keyName}'");
    }

    public void MarkDoorOpen(string doorName)
    {
        if (string.IsNullOrEmpty(doorName) || IsDoorOpen(doorName)) return;

        data.openedDoors = Append(data.openedDoors, doorName);
        SaveGame($"door '{doorName}'");
    }

    /// <summary>Records an unlocked ability and auto-saves (Phase 5).</summary>
    public void MarkAbilityUnlocked(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName) || HasAbility(abilityName)) return;

        data.unlockedAbilities = Append(data.unlockedAbilities, abilityName);
        SaveGame($"ability '{abilityName}'");

        if (AbilityUnlocked != null) AbilityUnlocked(abilityName);
    }

    public void MarkBossDefeated(string bossId)
    {
        if (string.IsNullOrEmpty(bossId) || IsBossDefeated(bossId)) return;
        data.defeatedBosses = Append(data.defeatedBosses, bossId);
        SaveGame($"boss '{bossId}' defeated");
    }

    public void DiscoverRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId) || IsRoomDiscovered(roomId)) return;
        data.discoveredRooms = Append(data.discoveredRooms, roomId);
        SaveGame($"room '{roomId}' discovered");
    }

    public void MarkItemCollected(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || IsItemCollected(itemId)) return;
        data.collectedItems = Append(data.collectedItems, itemId);
        SaveGame($"item '{itemId}' collected");
    }

    public void AddDirhams(int amount, string reason = "pickup")
    {
        if (amount <= 0) return;
        data.ancientDirhams += amount;
        SaveGame($"{amount} dirhams: {reason}");
        RaiseDirhamsChanged();
    }

    public bool TrySpendDirhams(int amount, string reason = "purchase")
    {
        if (amount <= 0 || data.ancientDirhams < amount) return false;
        data.ancientDirhams -= amount;
        SaveGame($"{amount} dirhams: {reason}");
        RaiseDirhamsChanged();
        return true;
    }

    /* ---------------- save / load ---------------- */

    /// <summary>
    /// Records the destination scene + spawn point of a transition and saves
    /// (Phase 6). The destination scene's SaveApplier reads them back on start.
    /// </summary>
    public void SetSceneCheckpoint(string sceneName, string spawnPointId)
    {
        // Snapshot the departing scene first, then point the save at the
        // destination (SaveGame tags the active scene and clears spawnPointId).
        SaveGame($"leaving for '{sceneName}'");

        data.currentScene = string.IsNullOrEmpty(sceneName) ? "" : sceneName;
        data.spawnPointId = string.IsNullOrEmpty(spawnPointId) ? "" : spawnPointId;
        WriteFile();
        Debug.Log($"[SaveManager] Scene checkpoint → '{data.currentScene}' at '{data.spawnPointId}'.", this);
    }

    /// <summary>Snapshots the player's live state to disk (bench rest, milestones).</summary>
    public void SaveGame(string reason = "bench rest")
    {
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        PlayerRespawn respawn = FindFirstObjectByType<PlayerRespawn>(FindObjectsInactive.Include);

        if (player != null)
        {
            data.health = player.CurrentHealth;
            data.maxHealth = player.MaxHealth;
        }

        if (respawn != null)
        {
            Vector3 pos = respawn.CurrentRespawnPosition;
            data.playerX = pos.x;
            data.playerY = pos.y;
        }

        // Phase 6: every save is tagged with the scene it was written in, so a
        // save can never restore coordinates into a different region. A normal
        // save (bench / auto-save) is position-authoritative, so any spawn id
        // left over from a transition is cleared.
        data.currentScene = SceneManager.GetActiveScene().name;
        data.spawnPointId = "";

        WriteFile();
        Debug.Log($"[SaveManager] Progress saved ({reason}) → {SavePath}", this);
    }

    /// <summary>Applies the loaded save to the scene's player. Called by SaveApplier.</summary>
    public void ApplyToScene(PlayerHealth player, PlayerRespawn respawn)
    {
        if (!HasSave || player == null || respawn == null) return;

        player.SetHealth(data.health);

        // Phase 6: a save belongs to one scene. Loading a different scene must
        // not snap the player to coordinates from the other region. Pre-Phase-6
        // saves have no scene recorded and keep the original behaviour.
        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(data.currentScene) && data.currentScene != activeScene)
        {
            Debug.Log($"[SaveManager] Save belongs to scene '{data.currentScene}' (active '{activeScene}') — health restored, position left at the scene spawn.", this);
            return;
        }

        SceneSpawnPoint spawn = SceneSpawnPoint.Find(data.spawnPointId);
        if (spawn != null)
        {
            respawn.TeleportTo(spawn.transform.position);
            respawn.SetRespawnPoint(spawn.transform);
            Debug.Log($"[SaveManager] Applied save: health {data.health}/{data.maxHealth}, spawn point '{data.spawnPointId}'.", this);
            return;
        }

        // No named spawn point: continue at the last checkpoint (bench / scene
        // spawn) recorded in the save. TeleportTo moves the physics body too.
        Vector3 savedPosition = new Vector3(data.playerX, data.playerY, 0f);
        respawn.TeleportTo(savedPosition);
        respawn.SetRespawnPosition(savedPosition);
        Debug.Log($"[SaveManager] Applied save: health {data.health}/{data.maxHealth}, respawn at ({data.playerX}, {data.playerY}).", this);
    }

    /// <summary>Deletes the save file and clears in-memory state (debug/testing).</summary>
    public void DeleteSave()
    {
        data = new SaveData();
        HasSave = false;

        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveManager] Save file deleted.", this);
        }

        RaiseDirhamsChanged();
    }

    /* ---------------- internals ---------------- */

    private void LoadFromFile()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save file found — starting fresh.", this);
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded != null)
            {
                loaded.collectedKeys ??= new string[0];
                loaded.openedDoors ??= new string[0];
                loaded.unlockedAbilities ??= new string[0];
                loaded.defeatedBosses ??= new string[0];
                loaded.discoveredRooms ??= new string[0];
                loaded.currentScene ??= "";
                loaded.spawnPointId ??= "";
                loaded.health = Mathf.Max(1, loaded.health);
                loaded.maxHealth = Mathf.Max(1, loaded.maxHealth);
                data = loaded;
                HasSave = true;
                Debug.Log($"[SaveManager] Save loaded: health {data.health}/{data.maxHealth}, " +
                          $"{data.collectedKeys.Length} key(s), {data.openedDoors.Length} door(s), " +
                          $"{data.unlockedAbilities.Length} ability/ies, scene '{data.currentScene}'.", this);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Corrupt save ignored: {e.Message}", this);
            data = new SaveData();
            HasSave = false;
        }

        RaiseDirhamsChanged();
    }

    private void RaiseDirhamsChanged()
    {
        if (DirhamsChanged != null) DirhamsChanged(data.ancientDirhams);
    }

    private void WriteFile()
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            HasSave = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to write save file: {e.Message}", this);
        }
    }

    private static string[] Append(string[] array, string value)
    {
        string[] result = new string[array.Length + 1];
        Array.Copy(array, result, array.Length);
        result[array.Length] = value;
        return result;
    }
}
