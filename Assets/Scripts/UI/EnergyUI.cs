using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Echo Energy meter for the HUD: a thin gold bar under the masks.
///
/// The bar dims while there is not enough energy for a Lantern heal, so the
/// player can read "I can heal now" at a glance without a separate icon.
/// Subscribes to PlayerEnergy.OnEnergyChanged; no polling.
///
/// Wiring (configured by Tools > Soledar > Apply HUD Design):
///   - Lives on the "EnergyBar" object under the gameplay Canvas.
///   - fill: "EnergyBar/EnergyFill" (Image, type Filled, horizontal).
///   - PlayerEnergy is found in the scene automatically.
/// </summary>
[DisallowMultipleComponent]
public class EnergyUI : MonoBehaviour
{
    [SerializeField] private Image fill;

    [Header("Colours")]
    [Tooltip("Full enough to pay for one Lantern heal.")]
    [SerializeField] private Color readyColor = new Color(0.894f, 0.761f, 0.478f, 1f); // gold
    [Tooltip("Below one heal's cost: the bar is present but visibly dim.")]
    [SerializeField] private Color notReadyColor = new Color(0.545f, 0.478f, 0.365f, 0.85f);

    private PlayerEnergy playerEnergy;
    private PlayerHealing healing;

    void OnEnable()
    {
        playerEnergy = FindFirstObjectByType<PlayerEnergy>(FindObjectsInactive.Include);
        healing = FindFirstObjectByType<PlayerHealing>(FindObjectsInactive.Include);

        if (playerEnergy == null)
        {
            Debug.LogWarning("[EnergyUI] No PlayerEnergy found in the scene — the meter will stay empty.", this);
            SetFill(0f, 0);
            return;
        }

        playerEnergy.OnEnergyChanged += HandleEnergyChanged;
        HandleEnergyChanged(playerEnergy.Current, playerEnergy.Max);
    }

    void OnDisable()
    {
        if (playerEnergy != null)
            playerEnergy.OnEnergyChanged -= HandleEnergyChanged;
    }

    private void HandleEnergyChanged(int current, int max)
    {
        SetFill(max > 0 ? (float)current / max : 0f, current);
    }

    private void SetFill(float amount01, int current)
    {
        if (fill == null) return;

        fill.fillAmount = Mathf.Clamp01(amount01);

        int cost = healing != null ? healing.EnergyCost : 0;
        bool ready = cost > 0 && current >= cost;
        fill.color = ready ? readyColor : notReadyColor;
    }
}
