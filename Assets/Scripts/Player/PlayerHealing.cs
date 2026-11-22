using System;
using UnityEngine;

/// <summary>
/// Lantern focus: hold Heal (F / gamepad LB) while standing still to convert
/// Echo Energy into one mask.
///
/// Design rules:
///   - The player must stand still on the ground, so healing is a decision made
///     in a safe moment rather than something to spam mid-fight.
///   - Taking damage, dying, attacking or letting go of the button cancels the
///     focus before any energy is spent; energy is only charged on completion.
///   - Healing costs more energy than a couple of hits refill, so a fight that
///     ends badly cannot be instantly undone.
///
/// Lives on the Player GameObject. Wires itself to PlayerHealth, PlayerEnergy,
/// PlayerInputReader and PlayerMovement on the same object.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerHealing : MonoBehaviour
{
    [Header("Lantern Focus")]
    [Tooltip("Echo Energy spent per heal.")]
    [SerializeField] private int energyCost = 33;
    [Tooltip("Seconds the button must be held before the mask is restored.")]
    [SerializeField] private float focusDuration = 0.6f;
    [Tooltip("Masks restored per completed focus.")]
    [SerializeField] private int masksRestored = 1;

    /// <summary>0..1 focus progress, raised every frame while focusing.</summary>
    public event Action<float> FocusProgress;

    /// <summary>Raised with the masks actually restored when a focus completes.</summary>
    public event Action<int> Healed;

    public bool IsFocusing { get; private set; }
    public int EnergyCost { get { return energyCost; } }

    private PlayerHealth health;
    private PlayerEnergy energy;
    private PlayerInputReader input;
    private PlayerMovement movement;

    private float focusTimer;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        energy = GetComponent<PlayerEnergy>();
        input = GetComponent<PlayerInputReader>();
        movement = GetComponent<PlayerMovement>();

        if (energy == null)
            Debug.LogError(
                "[PlayerHealing] No PlayerEnergy on the Player — Lantern healing cannot be paid for. " +
                "Add PlayerEnergy to the Player GameObject.", this);

        health.OnDied += HandleDied;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnDied -= HandleDied;
    }

    void Update()
    {
        if (health == null || energy == null || input == null || movement == null)
            return;

        if (health.IsDead)
        {
            Cancel();
            return;
        }

        // Damage or an attack breaks the focus instead of silently continuing.
        if (IsFocusing && (input.AttackWasPressed || health.IsStunned))
        {
            Cancel();
            return;
        }

        bool wantsFocus = input.HealHeld && health.CanHeal && energy.HasEnough(energyCost);

        if (!wantsFocus)
        {
            Cancel();
            return;
        }

        if (!IsFocusing)
        {
            // A focus must begin rooted: no starting it out of a jump or a dash.
            if (!movement.IsGroundedNow || movement.IsDashing)
            {
                Cancel();
                return;
            }

            IsFocusing = true;
            focusTimer = 0f;
            movement.SetMovementLocked(true);
        }

        focusTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(focusTimer / Mathf.Max(0.01f, focusDuration));
        if (FocusProgress != null) FocusProgress(progress);

        if (focusTimer >= focusDuration)
            Complete();
    }

    private void Complete()
    {
        int healed = health.CanHeal ? health.Heal(masksRestored) : 0;

        if (healed <= 0)
        {
            Cancel();
            return;
        }

        energy.TrySpend(energyCost);
        Debug.Log($"[PlayerHealing] Lantern focus complete — restored {healed} mask(s) for {energyCost} Echo Energy.", this);

        if (Healed != null) Healed(healed);
        Cancel();
    }

    private void HandleDied()
    {
        Cancel();
    }

    private void Cancel()
    {
        if (focusTimer > 0f || IsFocusing)
        {
            focusTimer = 0f;
            if (FocusProgress != null) FocusProgress(0f);
        }

        if (IsFocusing)
        {
            IsFocusing = false;
            if (movement != null) movement.SetMovementLocked(false);
        }
    }
}
