using System;
using System.IO;
using UnityEngine;

/// <summary>
/// SOLEDAR's persistence layer (Phase 4).
///
/// A single hidden GameObject holds all saved state for the session and
/// mirrors it to a JSON file under Application.persistentDataPath.
///
/// Save points: Forgotten Bench rest (full heal + respawn + everything below).
/// Persisted: player health, respawn position, collected keys, opened doors.
///
/// Other systems never touch the file directly:
///   - Door.Awake / CollectibleKey.Awake query IsDoorOpen / IsKeyCollected
///     so saved state survives leaving Play mode and restarting the game.
///   - MarkKeyCollected / MarkDoorOpen update state AND save immediately.
///   - SaveGame() snapshots the player's live state (called on bench rest).
///   - SaveApplier (on the Player) applies the saved state on scene start.
/// </summary>
[DisallowMultipleComponent]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "soledar_save.json"); }
    }

    [Serializable]
    private class SaveData
    {
        public float playerX;
        public float playerY;
        public int health = 5;
        public int maxHealth = 5;
        public string[] collectedKeys = new string[0];
        public string[] openedDoors = new string[0];
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

    /* ---------------- save / load ---------------- */

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

        WriteFile();
        Debug.Log($"[SaveManager] Progress saved ({reason}) → {SavePath}", this);
    }

    /// <summary>Applies the loaded save to the scene's player. Called by SaveApplier.</summary>
    public void ApplyToScene(PlayerHealth player, PlayerRespawn respawn)
    {
        if (!HasSave || player == null || respawn == null) return;

        player.SetHealth(data.health);
        respawn.SetRespawnPosition(new Vector3(data.playerX, data.playerY, 0f));
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
                data = loaded;
                HasSave = true;
                Debug.Log($"[SaveManager] Save loaded: health {data.health}/{data.maxHealth}, " +
                          $"{data.collectedKeys.Length} key(s), {data.openedDoors.Length} door(s).", this);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to read save file: {e.Message}", this);
        }
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
