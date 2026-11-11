using UnityEngine;

/// <summary>
/// Melee lunge attack pattern (Phase 2).
///   Windup (telegraph tint) ──► Lunge (burst of velocity) ──► Recovery ──► cooldown
/// Damage itself comes from ContactDamage, which stays active during the lunge.
/// Windup tint + cooldown make the pattern readable instead of random.
///
/// Component order matters: EnemyAI must be ABOVE this component so the lunge
/// velocity (written here) wins over the AI's idle velocity in Attack state.
/// The setup tool adds them in that order.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class EnemyAttack : MonoBehaviour
{
    private enum Phase { None, Windup, Lunge, Recovery }

    [SerializeField] private EnemyData data;

    public bool IsBusy { get { return phase != Phase.None; } }

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private EnemyHealth health;

    private Phase phase = Phase.None;
    private float phaseTimer;
    private float cooldownTimer;
    private int lungeDir = 1;
    private Color baseColor = Color.white;
    private bool baseColorCached;
    private bool tinted;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();

        if (data == null)
            Debug.LogError($"[EnemyAttack] No EnemyData assigned on '{name}'. Run the Phase 2 setup tool or assign one in the Inspector.", this);

        if (sprite != null)
        {
            baseColor = sprite.color;
            baseColorCached = true;
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (health != null && health.IsDead)
        {
            ClearTint();
            phase = Phase.None;
            return;
        }

        switch (phase)
        {
            case Phase.Windup:
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                {
                    phase = Phase.Lunge;
                    phaseTimer = data != null ? data.lungeDuration : 0.25f;
                    ClearTint();
                }
                break;

            case Phase.Lunge:
                rb.linearVelocity = new Vector2(lungeDir * (data != null ? data.lungeSpeed : 6f), rb.linearVelocity.y);
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                {
                    phase = Phase.Recovery;
                    phaseTimer = data != null ? data.recoveryTime : 0.35f;
                }
                break;

            case Phase.Recovery:
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                {
                    phase = Phase.None;
                    cooldownTimer = data != null ? data.attackCooldown : 1.5f;
                }
                break;
        }
    }

    void FixedUpdate()
    {
        // Hold still during windup and recovery so the lunge reads clearly.
        if (phase == Phase.Windup || phase == Phase.Recovery)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>Attempts to start the attack cycle. Returns false when busy/on cooldown.</summary>
    public bool TryStartAttack(int direction)
    {
        if (phase != Phase.None) return false;
        if (health != null && (health.IsDead || health.IsStunned)) return false;
        if (cooldownTimer > 0f) return false;

        lungeDir = direction != 0 ? direction : 1;
        phase = Phase.Windup;
        phaseTimer = data != null ? data.attackWindup : 0.35f;

        // Telegraph: tint the sprite during windup.
        if (sprite != null)
        {
            sprite.color = new Color(1f, 0.4f, 0.35f, 1f);
            tinted = true;
        }

        return true;
    }

    private void ClearTint()
    {
        if (tinted && sprite != null && baseColorCached)
            sprite.color = baseColor;
        tinted = false;
    }
}
