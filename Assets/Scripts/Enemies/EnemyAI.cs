using UnityEngine;

/// <summary>
/// Generic enemy AI state machine (Phase 2).
///
///   Idle ──► Patrol ──► (player detected via range + line of sight) ──► Chase
///                          ▲                                              │
///                          └── lost player ───────────────────────────────┘
///   Chase ──► (within attack range) ──► Attack (EnemyAttack lunge)
///
/// - Patrols between ledges/walls using probe raycasts (never walks off edges)
/// - Movement intent is applied in FixedUpdate; decisions in Update
/// - Reads EnemyData for every tunable; pauses while knocked back (stun)
///   or dead.
///
/// Lives on every enemy GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }

    [Header("Data")]
    [SerializeField] private EnemyData data;

    [Header("Behaviour Tuning (fallback)")]
    [SerializeField] private float idleTimeMin = 0.4f;
    [SerializeField] private float idleTimeMax = 1.2f;

    public State CurrentState { get { return state; } }

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private EnemyHealth health;
    private EnemyAttack attack;
    private Transform player;

    private State state = State.Idle;
    private int moveDir = 1;                 // 1 = right, -1 = left
    private float stateTimer;
    private bool hasPatrolAnchor;
    private Vector2 patrolAnchor;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
        attack = GetComponent<EnemyAttack>();

        if (data == null)
        {
            Debug.LogError($"[EnemyAI] No EnemyData assigned on '{name}'. Run 'Tools > Soledar > Setup Phase 2 Combat & NPCs' or assign one in the Inspector.", this);
            enabled = false;
            return;
        }

        PlayerMovement found = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (found != null) player = found.transform;

        hasPatrolAnchor = false;
    }

    void Update()
    {
        if (health == null || health.IsDead)
            return;

        if (health.IsStunned)
            return; // knocked back — no decisions during the reaction

        float distToPlayer = player != null
            ? Vector2.Distance(rb.position, player.position)
            : float.MaxValue;

        bool playerVisible = player != null &&
                             distToPlayer <= data.detectionRange &&
                             HasLineOfSight();

        /* ---------------- Transitions ---------------- */
        if (state == State.Attack)
        {
            // EnemyAttack owns this phase; wait for it to finish.
            if (attack == null || !attack.IsBusy)
                state = State.Chase;
            return;
        }

        if (playerVisible)
        {
            if (distToPlayer <= data.attackRange && attack != null &&
                attack.TryStartAttack(GetDirectionTowards(player.position)))
            {
                state = State.Attack;
                return;
            }

            state = State.Chase;
        }
        else if (state == State.Chase)
        {
            // Lost the player: short pause, then resume patrol.
            state = State.Idle;
            stateTimer = Random.Range(idleTimeMin, idleTimeMax);
        }

        if (state == State.Idle && stateTimer <= 0f)
        {
            state = State.Patrol;
            if (!hasPatrolAnchor)
            {
                patrolAnchor = rb.position;
                hasPatrolAnchor = true;
            }
        }

        // Turn around on obstacles while patrolling.
        if (state == State.Patrol && (EdgeAhead(moveDir) || WallAhead(moveDir)))
            moveDir = -moveDir;
    }

    void FixedUpdate()
    {
        if (health == null || health.IsDead || health.IsStunned)
            return;

        switch (state)
        {
            case State.Idle:
                SetHorizontalVelocity(0f);
                break;

            case State.Patrol:
                SetHorizontalVelocity(moveDir * data.patrolSpeed);
                break;

            case State.Chase:
                if (player != null)
                {
                    moveDir = GetDirectionTowards(player.position);
                    // Don't chase off a ledge.
                    if (EdgeAhead(moveDir))
                        SetHorizontalVelocity(0f);
                    else
                        SetHorizontalVelocity(moveDir * data.chaseSpeed);
                }
                else
                {
                    SetHorizontalVelocity(0f);
                }
                break;

            case State.Attack:
                // EnemyAttack drives velocity during windup/lunge/recovery.
                break;
        }

        if (sprite != null && moveDir != 0)
            sprite.flipX = moveDir < 0;
    }

    /* ===================== helpers ===================== */

    private void SetHorizontalVelocity(float x)
    {
        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
    }

    private int GetDirectionTowards(Vector3 target)
    {
        return target.x >= rb.position.x ? 1 : -1;
    }

    private bool HasLineOfSight()
    {
        // Blocked if the ground layer sits between the enemy and the player.
        RaycastHit2D hit = Physics2D.Linecast(rb.position, player.position, data.groundMask);
        return hit.collider == null;
    }

    private bool EdgeAhead(int direction)
    {
        Vector2 origin = rb.position + Vector2.right * direction * data.edgeProbeDistance;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, data.edgeProbeDepth, data.groundMask);
        return hit.collider == null; // no ground ahead = edge
    }

    private bool WallAhead(int direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(rb.position, Vector2.right * direction, data.edgeProbeDistance, data.groundMask);
        return hit.collider != null;
    }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);

        Gizmos.color = Color.cyan;
        Vector2 origin = (Vector2)transform.position + Vector2.right * moveDir * data.edgeProbeDistance;
        Gizmos.DrawLine(origin, origin + Vector2.down * data.edgeProbeDepth);
    }
}
