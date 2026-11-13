using UnityEngine;

/// <summary>
/// The Gatekeeper — first boss (blueprint §6.1): a large armored guardian at
/// the end of the Medina Outskirts, standing in front of the Palace Gate.
/// Three basic attack patterns (slam, sweep, volley), a vulnerable window
/// after each one, and a faster phase below half health. Teaches dodging and
/// counterattacking.
///
/// Not a plain enemy with more health: dedicated state machine, telegraphs,
/// arena clamping, a phase transition, and a BossArena owner that seals the
/// courtyard, drives the boss health bar, and grants the reward on death.
///
/// Player damage in: PlayerAttack overlaps the Enemy layer → EnemyHealth.
/// Player damage out: shared ContactDamage component on this GameObject.
///
/// Lives on the boss GameObject (Phase 5 setup tool).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
[DisallowMultipleComponent]
public class GatekeeperBoss : MonoBehaviour
{
    private enum State { Dormant, Choosing, WindUp, Slam, Sweep, Volley, Recover, Dead }

    [Header("Identity")]
    [SerializeField] private string bossName = "THE GATEKEEPER";

    [Header("Layers")]
    [Tooltip("Layer the tilemaps are on — projectiles pop when they land on it.")]
    public LayerMask groundLayer;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float gravityScale = 3f;

    [Header("Pattern 1 — Slam")]
    [SerializeField] private float slamJumpForce = 12f;
    [SerializeField] private float slamChaseSpeed = 7f;
    [SerializeField] private float slamDamageRadius = 1.7f;

    [Header("Pattern 2 — Sweep")]
    [SerializeField] private float sweepSpeed = 8f;
    [SerializeField] private float sweepDuration = 0.55f;

    [Header("Pattern 3 — Volley")]
    [SerializeField] private BossProjectile projectilePrefab;   // optional; spawns inline when null
    [SerializeField] private Sprite projectileSprite;           // used by the inline spawn
    [SerializeField] private int volleyCount = 3;
    [SerializeField] private float volleySpeed = 7f;

    [Header("Phase 2 (<= 50% health)")]
    [SerializeField] private int phase2VolleyCount = 5;
    [SerializeField] private float phase2SweepSpeed = 10f;

    [Header("Timing")]
    [SerializeField] private float telegraphTime = 0.55f;
    [SerializeField] private float recoverTime = 1.1f;
    [SerializeField] private float chooseDelay = 0.7f;

    public string BossName { get { return bossName; } }
    public bool IsAwake { get; private set; }

    private Rigidbody2D rb;
    private EnemyHealth health;
    private SpriteRenderer sprite;
    private Color initialColor = Color.white;  // captured once in Awake
    private Color restingColor = Color.white;  // initial, or the phase-2 angry tint
    private Transform player;
    private CameraShake shake;

    private State state = State.Dormant;
    private float stateTimer;
    private int chosenAttack;
    private int sweepDirection = 1;

    private bool phase2;
    private int volleyBase;
    private float sweepBase;

    private Vector2 arenaMin;
    private Vector2 arenaMax;
    private bool boundsKnown;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            initialColor = sprite.color;
            restingColor = initialColor;
        }

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;

        volleyBase = volleyCount;
        sweepBase = sweepSpeed;
    }

    void Start()
    {
        if (health != null) health.OnHealthChanged += HandleHealthChanged;
        shake = FindFirstObjectByType<CameraShake>(FindObjectsInactive.Include);
    }

    void OnDestroy()
    {
        if (health != null) health.OnHealthChanged -= HandleHealthChanged;
    }

    /* ===================== API (called by BossArena) ===================== */

    /// <summary>Starts the fight: the boss wakes and fights inside the given bounds.</summary>
    public void Wake(Transform playerTransform, Vector2 min, Vector2 max)
    {
        if (IsAwake) return;

        player = playerTransform;
        arenaMin = min;
        arenaMax = max;
        boundsKnown = true;
        IsAwake = true;
        EnterState(State.Choosing, chooseDelay);
        Debug.Log("[GatekeeperBoss] The Gatekeeper awakens.", this);
    }

    /// <summary>Retry support (player died mid-fight): full reset, no respawn of the object.</summary>
    public void ResetBoss(Vector3 spawnPosition)
    {
        if (health != null) health.ResetHealth();

        phase2 = false;
        volleyCount = volleyBase;
        sweepSpeed = sweepBase;
        IsAwake = false;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
        transform.position = spawnPosition;
        if (rb != null)
            rb.position = spawnPosition; // Unity 6: autoSyncTransforms is off — sync the body explicitly
        Physics2D.SyncTransforms();

        if (sprite != null)
        {
            restingColor = initialColor; // phase 1 again after a retry
            sprite.color = restingColor;
        }
        state = State.Dormant;
        stateTimer = 0f;
        Debug.Log("[GatekeeperBoss] The Gatekeeper resets — the fight can begin again.", this);
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (phase2 || max <= 0 || current <= 0 || current > max / 2) return;

        phase2 = true;
        volleyCount = phase2VolleyCount;
        sweepSpeed = phase2SweepSpeed;
        if (sprite != null)
        {
            restingColor = new Color(0.85f, 0.32f, 0.22f, 1f); // angry tint becomes the resting color
            sprite.color = restingColor;
        }
        Debug.Log("[GatekeeperBoss] Phase 2 — the Gatekeeper burns with stale incense.", this);
    }

    /* ===================== state machine ===================== */

    private void EnterState(State next, float duration)
    {
        state = next;
        stateTimer = duration;
    }

    void Update()
    {
        if (!IsAwake || state == State.Dead) return;
        if (health != null && health.IsDead)
        {
            state = State.Dead;
            return;
        }

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Choosing:
                WalkTowardPlayer();
                if (stateTimer <= 0f) ChooseAttack();
                break;

            case State.WindUp:
                if (stateTimer <= 0f) ExecuteAttack();
                break;

            case State.Sweep:
                rb.linearVelocity = new Vector2(sweepDirection * sweepSpeed, 0f);
                if (stateTimer <= 0f)
                {
                    rb.linearVelocity = Vector2.zero;
                    EnterState(State.Recover, recoverTime);
                }
                break;

            case State.Slam:
            case State.Volley:
                if (stateTimer <= 0f) EnterState(State.Recover, recoverTime);
                break;

            case State.Recover:
                if (stateTimer <= 0f) EnterState(State.Choosing, chooseDelay);
                break;
        }
    }

    void FixedUpdate()
    {
        // Never let the fight leave the courtyard (walls do most of the work;
        // this is the safety net against knockback into the climb).
        if (!boundsKnown || rb == null) return;

        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, arenaMin.x, arenaMax.x);
        if (pos != rb.position) rb.position = pos;
    }

    private void WalkTowardPlayer()
    {
        if (player == null) return;

        float dx = player.position.x - transform.position.x;
        if (sprite != null) sprite.flipX = dx < 0f;

        if (Mathf.Abs(dx) > 2.5f)
            rb.linearVelocity = new Vector2(Mathf.Sign(dx) * walkSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void ChooseAttack()
    {
        chosenAttack = Random.Range(0, 3);
        EnterState(State.WindUp, telegraphTime);

        if (sprite != null)
            sprite.color = Color.Lerp(restingColor, Color.red, 0.7f); // telegraph flash
    }

    private void ExecuteAttack()
    {
        if (sprite != null) sprite.color = restingColor;

        switch (chosenAttack)
        {
            case 0: StartSlam(); break;
            case 1: StartSweep(); break;
            default: StartVolley(); break;
        }
    }

    private void StartSlam()
    {
        if (player == null) { EnterState(State.Recover, recoverTime); return; }

        float dx = player.position.x - transform.position.x;
        rb.linearVelocity = new Vector2(
            Mathf.Clamp(dx * 1.5f, -slamChaseSpeed, slamChaseSpeed),
            slamJumpForce);
        EnterState(State.Slam, 1.4f); // arc up + fall; landing resolves in OnCollisionEnter2D
    }

    private void StartSweep()
    {
        if (player != null)
            sweepDirection = player.position.x > transform.position.x ? 1 : -1;
        EnterState(State.Sweep, sweepDuration);
    }

    private void StartVolley()
    {
        for (int i = 0; i < volleyCount; i++)
        {
            float spread = volleyCount > 1
                ? Mathf.Lerp(-2.5f, 2.5f, (float)i / (volleyCount - 1))
                : 0f;
            SpawnProjectile(new Vector2(spread, volleySpeed));
        }

        EnterState(State.Volley, 0.5f);
    }

    private void SpawnProjectile(Vector2 velocity)
    {
        Vector3 spawnPos = transform.position + Vector3.up * 0.6f;
        BossProjectile shot = projectilePrefab != null
            ? Instantiate(projectilePrefab, spawnPos, Quaternion.identity)
            : BossProjectile.Create(spawnPos, projectileSprite);

        shot.Launch(velocity);
    }

    /* ===================== slam landing ===================== */

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != State.Slam) return;
        if (((1 << collision.gameObject.layer) & groundLayer) == 0) return;

        // Shockwave on landing: damages the player inside the radius.
        if (player != null && !IsPlayerDead())
        {
            float distance = Vector2.Distance(
                new Vector2(player.position.x, player.position.y),
                new Vector2(transform.position.x, transform.position.y));

            if (distance <= slamDamageRadius)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                PlayerHealth playerHealth = player.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(1, direction);
            }
        }

        if (shake != null) shake.Shake(1f);
        EnterState(State.Recover, recoverTime);
    }

    private bool IsPlayerDead()
    {
        PlayerHealth playerHealth = player != null ? player.GetComponentInParent<PlayerHealth>() : null;
        return playerHealth == null || playerHealth.IsDead;
    }
}
