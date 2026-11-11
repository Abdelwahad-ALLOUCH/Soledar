using UnityEngine;

/// <summary>
/// Data asset for an enemy type (Phase 2: Dust Crawler, Medina Guard).
/// All tunables live here so designers balance without touching code.
/// Create via: Assets > Create > Soledar > Enemies > Enemy Data
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Soledar/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Enemy";

    [Header("Health")]
    public int maxHealth = 3;
    [Range(0f, 1f)] public float knockbackResist = 0f;   // 0 = full knockback, 1 = immune

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float contactCooldown = 0.8f;   // per-enemy cooldown between contact hits

    [Header("Knockback")]
    public float knockbackForce = 6f;      // impulse applied when hit

    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3f;

    [Header("Detection")]
    public float detectionRange = 6f;      // distance at which the enemy notices the player

    [Header("Attack")]
    public float attackRange = 1.3f;       // distance at which the enemy initiates an attack
    public float attackCooldown = 1.5f;    // seconds between attacks
    public float attackWindup = 0.35f;     // telegraph time before the lunge
    public float lungeSpeed = 6f;
    public float lungeDuration = 0.25f;
    public float recoveryTime = 0.35f;     // vulnerable standstill after the lunge

    [Header("Navigation Probes")]
    [Tooltip("Layers that count as ground/walls for edge and wall detection.")]
    public LayerMask groundMask;
    public float edgeProbeDistance = 0.6f; // how far ahead the probe sits
    public float edgeProbeDepth = 0.6f;    // how far down the edge probe looks

    [Header("Placeholder Visuals")]
    [Tooltip("Temporary tint until real enemy sprites exist (Phase 6).")]
    public Color placeholderColor = new Color(0.7f, 0.3f, 0.25f, 1f);
}
