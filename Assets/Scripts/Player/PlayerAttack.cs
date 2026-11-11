using UnityEngine;

/// <summary>
/// Light attack with the Ashen Khoumya (Phase 1: single horizontal slash).
/// Split out of PlayerMovement so Phase 2 combos, directional attacks and
/// hit-stop extend this component instead of growing the movement controller.
///
/// Behaviour preserved from the original prototype:
///   - OverlapCircle on the "Enemy" layer at attackPoint
///   - 1 damage + horizontal knockback in the facing direction
///   - attackCooldown gate
///
/// Wiring (configured by Tools > Soledar > Setup Phase 1 Scene):
///   - Lives on the Player GameObject.
///   - attackPoint: child Transform "AttackPoint" (flips with facing direction).
///   - enemyLayer: the "Enemy" layer.
///   - Requires PlayerMovement (facing) + PlayerInputReader (input) on the same object.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayer;
    public float attackCooldown = 0.3f;
    public int damage = 1;

    private PlayerMovement movement;
    private PlayerInputReader input;
    private PlayerHealth health;
    private float cooldownTimer;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputReader>();
        health = GetComponent<PlayerHealth>();

        if (movement == null)
            Debug.LogError("[PlayerAttack] PlayerMovement not found on this GameObject — facing direction cannot be resolved.", this);

        if (input == null)
            Debug.LogError("[PlayerAttack] PlayerInputReader not found on this GameObject. Run 'Tools > Soledar > Setup Phase 1 Scene'.", this);

        if (attackPoint == null)
            Debug.LogError(
                "[PlayerAttack] attackPoint is not assigned. Run 'Tools > Soledar > Setup Phase 1 Scene' " +
                "with the TestScene open, or assign the child 'AttackPoint' Transform manually.",
                this);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Keep the slash in front of the player (behaviour preserved from prototype).
        if (attackPoint != null && movement != null)
        {
            Vector3 pos = attackPoint.localPosition;
            pos.x = Mathf.Abs(pos.x) * movement.FacingDirection;
            attackPoint.localPosition = pos;
        }

        if (movement == null || input == null || attackPoint == null) return;
        if (health != null && (health.IsDead || health.IsStunned))
            return; // no attacking while dying or knocked back

        if (input.AttackWasPressed && cooldownTimer <= 0f)
        {
            DoAttack();
            cooldownTimer = attackCooldown;
        }
    }

    private void DoAttack()
    {
        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (Collider2D enemy in enemiesHit)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                Vector2 knockbackDir = Vector2.right * movement.FacingDirection;
                enemyHealth.TakeDamage(damage, knockbackDir);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
