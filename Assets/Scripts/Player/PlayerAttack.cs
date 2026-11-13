using UnityEngine;

/// <summary>
/// Soledar's Ashen Khoumya combat (Phase 2).
///
/// Three-hit combo driven by AttackComboData:
///   step 0 — quick horizontal slash
///   step 1 — diagonal slash
///   step 2 — powerful finisher
/// Pressing attack again within the combo window advances to the next step.
/// Holding Up/Down at the moment of an attack aims the slash upward/downward.
///
/// Descent of Ash (Phase 5): once unlocked (SaveManager ability), attacking
/// while airborne and holding Down performs a downward strike instead of a
/// combo step. Connecting with an enemy or a PogoSurface bounces the player
/// upward (pogo) — the traversal core of the ability.
///
/// Feel: hit-stop + camera shake fire ONLY when a step connects.
/// Rules preserved from Phase 1: no attacking while stunned or dead.
///
/// Wiring (Phase 1/2 setup tool):
///   - Lives on the Player GameObject.
///   - attackPoint: child Transform "AttackPoint" (flips with facing).
///   - comboData: Assets/ScriptableObjects/Combat/KhoumyaCombo.asset.
///   - shake: the Player's CameraShake component (same object).
/// </summary>
[DisallowMultipleComponent]
public class PlayerAttack : MonoBehaviour
{
    [Header("Combo Data")]
    [Tooltip("Assets/ScriptableObjects/Combat/KhoumyaCombo.asset")]
    public AttackComboData comboData;

    [Header("Fallback (used when no comboData assigned)")]
    public int fallbackDamage = 1;
    public float fallbackRange = 0.5f;

    [Header("Wiring")]
    public Transform attackPoint;
    public LayerMask enemyLayer;

    [Header("Descent of Ash (Phase 5)")]
    [Tooltip("SaveManager ability name that unlocks the downward strike.")]
    public string descentAbilityName = "descentOfAsh";
    public float downAttackRange = 1f;
    public float pogoForce = 11f;
    public float downAttackRecovery = 0.25f;

    private PlayerMovement movement;
    private PlayerInputReader input;
    private PlayerHealth health;
    private CameraShake shake;

    private int currentStep;               // next step to execute
    private float recoveryTimer;
    private float comboWindowTimer;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputReader>();
        health = GetComponent<PlayerHealth>();
        shake = GetComponent<CameraShake>();

        if (movement == null)
            Debug.LogError("[PlayerAttack] PlayerMovement not found on this GameObject — facing direction cannot be resolved.", this);

        if (input == null)
            Debug.LogError("[PlayerAttack] PlayerInputReader not found on this GameObject. Run 'Tools > Soledar > Setup Phase 1 Scene'.", this);

        if (attackPoint == null)
            Debug.LogError(
                "[PlayerAttack] attackPoint is not assigned. Run 'Tools > Soledar > Setup Phase 1 Scene' " +
                "with the TestScene open, or assign the child 'AttackPoint' Transform manually.", this);

        if (comboData == null)
            Debug.LogWarning(
                "[PlayerAttack] No AttackComboData assigned — using fallback single attack. " +
                "Run 'Tools > Soledar > Setup Phase 2 Combat & NPCs' to create and assign the combo.", this);
    }

    void Update()
    {
        if (recoveryTimer > 0f)
            recoveryTimer -= Time.deltaTime;

        if (comboWindowTimer > 0f)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0f)
                currentStep = 0; // combo expired — reset to first slash
        }

        // Keep the slash in front of the player (behaviour preserved from Phase 1).
        if (attackPoint != null && movement != null)
        {
            Vector3 pos = attackPoint.localPosition;
            pos.x = Mathf.Abs(pos.x) * movement.FacingDirection;
            attackPoint.localPosition = pos;
        }

        if (movement == null || input == null || attackPoint == null) return;
        if (health != null && (health.IsDead || health.IsStunned))
        {
            ResetCombo();
            return;
        }

        if (input.AttackWasPressed && recoveryTimer <= 0f)
        {
            // Descent of Ash (Phase 5): airborne + holding Down + ability unlocked
            // → a dedicated downward strike instead of the next combo step.
            if (input.MoveInput.y < -0.5f && !movement.IsGroundedNow && DescentUnlocked())
            {
                PerformDownStrike(ResolveStep());
                return;
            }

            PerformNextStep();
        }
    }

    private bool DescentUnlocked()
    {
        return !string.IsNullOrEmpty(descentAbilityName) &&
               SaveManager.EnsureInstance().HasAbility(descentAbilityName);
    }

    /// <summary>
    /// Downward strike below the player: damages enemies (no knockback — they
    /// are below), bounces off PogoSurfaces and pogo-bounces the player upward
    /// on any connection. Does not advance the combo.
    /// </summary>
    private void PerformDownStrike(AttackComboData.AttackStep step)
    {
        int damage = step != null ? step.damage : fallbackDamage;
        Vector2 origin = (Vector2)transform.position + Vector2.down * (downAttackRange * 0.6f);
        bool connected = false;

        foreach (Collider2D enemy in Physics2D.OverlapCircleAll(origin, downAttackRange, enemyLayer))
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth == null) continue;

            enemyHealth.TakeDamage(damage, Vector2.down, 1f);
            connected = true;
        }

        // PogoSurfaces: bounce pads (and later breakable floors). Unmasked
        // query so trigger pads are found; only PogoSurface components count.
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, downAttackRange))
        {
            PogoSurface surface = hit.GetComponent<PogoSurface>();
            if (surface != null && surface.Struck())
                connected = true;
        }

        if (connected)
        {
            movement.PogoBounce(pogoForce);
            HitStopController.Freeze(0.05f);
            if (shake != null) shake.Shake(0.6f);
        }

        recoveryTimer = downAttackRecovery;
    }

    private void PerformNextStep()
    {
        AttackComboData.AttackStep step = ResolveStep();

        int damage = step != null ? step.damage : fallbackDamage;
        float range = step != null ? step.range : fallbackRange;

        /* -------- direction: forward / up / down -------- */
        Vector2 baseOrigin = attackPoint.position;
        if (input.MoveInput.y > 0.5f)
            baseOrigin += Vector2.up * range;          // strike above
        else if (input.MoveInput.y < -0.5f)
            baseOrigin += Vector2.down * range;        // strike below

        /* -------- hit detection (instant, like the prototype) -------- */
        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(baseOrigin, range, enemyLayer);
        int hits = 0;

        foreach (Collider2D enemy in enemiesHit)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth == null) continue;

            Vector2 knockbackDir = Vector2.right * movement.FacingDirection *
                                   (step != null ? step.knockbackMultiplier : 1f);
            enemyHealth.TakeDamage(damage, knockbackDir, 1f);
            hits++;
        }

        /* -------- feedback: only when the attack connects -------- */
        if (hits > 0 && step != null)
        {
            HitStopController.Freeze(step.hitStopDuration);
            if (shake != null) shake.Shake(step.shakeForce);
        }

        /* -------- timing -------- */
        if (step != null)
        {
            recoveryTimer = step.recoveryTime;
            comboWindowTimer = step.recoveryTime + step.comboWindow;
        }
        else
        {
            recoveryTimer = 0.3f; // legacy fallback cadence
        }

        currentStep = (currentStep + 1) % Mathf.Max(1, StepCount());
    }

    private AttackComboData.AttackStep ResolveStep()
    {
        if (comboData == null || comboData.steps == null || comboData.steps.Length == 0)
            return null;

        return comboData.steps[Mathf.Clamp(currentStep, 0, comboData.steps.Length - 1)];
    }

    private int StepCount()
    {
        return comboData != null && comboData.steps != null ? comboData.steps.Length : 1;
    }

    private void ResetCombo()
    {
        currentStep = 0;
        comboWindowTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, fallbackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(attackPoint.position + Vector3.up * fallbackRange, fallbackRange);
        Gizmos.DrawWireSphere(attackPoint.position + Vector3.down * fallbackRange, fallbackRange);

        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f); // Descent of Ash strike zone
        Gizmos.DrawWireSphere(
            transform.position + Vector3.down * (downAttackRange * 0.6f), downAttackRange);
    }
}
