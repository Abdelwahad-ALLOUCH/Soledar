using UnityEngine;

/// <summary>
/// Applies the loaded save to the player at scene start (Phase 4).
/// Lives on the Player GameObject; added by the Phase 4 setup tool.
/// With no save file present this component does nothing.
/// </summary>
[DisallowMultipleComponent]
public class SaveApplier : MonoBehaviour
{
    void Start()
    {
        SaveManager manager = SaveManager.EnsureInstance();
        if (!manager.HasSave) return;

        manager.ApplyToScene(GetComponent<PlayerHealth>(), GetComponent<PlayerRespawn>());
    }
}
