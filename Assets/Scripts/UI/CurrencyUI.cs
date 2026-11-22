using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ancient Dirham readout for the HUD: the current balance plus a short pulse on
/// the coin mark when the player gains dirhams. Spending updates the number
/// quietly, so the feedback still means "you picked something up".
///
/// Wiring (configured by Tools > Soledar > Apply HUD Design):
///   - Lives on the "CurrencyRow" object under the gameplay Canvas.
///   - countText:   "CurrencyRow/DirhamCount".
///   - pulseTarget: "CurrencyRow/DirhamMark".
///
/// Subscribes to SaveManager.DirhamsChanged, so every source (pickups, shops,
/// save reload) stays in sync without the HUD polling.
/// </summary>
[DisallowMultipleComponent]
public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private Text countText;
    [SerializeField] private RectTransform pulseTarget;

    [Header("Gain Pulse")]
    [SerializeField] private float pulseDuration = 0.22f;
    [SerializeField] private float pulseScale = 1.35f;

    private int shown;
    private Coroutine pulseRoutine;
    private Vector3 baseScale = Vector3.one;

    void Awake()
    {
        // Authored scale, captured once: a scene can unload mid-pulse.
        if (pulseTarget != null) baseScale = pulseTarget.localScale;
    }

    void OnEnable()
    {
        if (pulseTarget != null) pulseTarget.localScale = baseScale;

        SaveManager.DirhamsChanged += HandleDirhamsChanged;
        Refresh(SaveManager.EnsureInstance().AncientDirhams, false);
    }

    void OnDisable()
    {
        SaveManager.DirhamsChanged -= HandleDirhamsChanged;
    }

    private void HandleDirhamsChanged(int total)
    {
        bool gained = total > shown;
        Refresh(total, gained);
    }

    private void Refresh(int total, bool pulse)
    {
        shown = total;

        if (countText != null)
            countText.text = total.ToString();

        if (pulse) PlayPulse();
    }

    private void PlayPulse()
    {
        if (pulseTarget == null) return;

        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    /// <summary>Unscaled time: the HUD can update while the game is paused.</summary>
    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(elapsed / pulseDuration) * Mathf.PI); // 0 → 1 → 0
            pulseTarget.localScale = baseScale * (1f + (pulseScale - 1f) * k);
            yield return null;
        }

        pulseTarget.localScale = baseScale;
        pulseRoutine = null;
    }
}
