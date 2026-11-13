using UnityEngine;

/// <summary>
/// Boss fight orchestration (Phase 5): when the player steps into the
/// courtyard it seals the entrance, wakes the boss, shows the health bar and
/// handles both outcomes:
///   - Victory: grants Descent of Ash (persisted + auto-saved) and opens the
///     Palace Gate (persisted like any door), plus a reward banner.
///   - Player death: unseals, resets the boss and hides the UI so the fight
///     can be retried from the bench inside the courtyard.
///
/// Wiring (Phase 5 setup tool):
///   - Lives on "GatekeeperArena" (trigger across the courtyard entrance).
///   - seal: child "Seal" BoxCollider2D (disabled until the fight starts).
///   - boundsCollider: child "BossBounds" BoxCollider2D covering the courtyard
///     (the boss is clamped inside it).
///   - boss / bossHealth: the Gatekeeper + its EnemyHealth.
///   - healthUI: BossHealthUI on the scene Canvas.
///   - rewardDoor: the PalaceGate Door.
///   - rewardBanner: canvas Text (hidden; shown on victory).
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class BossArena : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Collider2D seal;
    [SerializeField] private Collider2D boundsCollider;
    [SerializeField] private GatekeeperBoss boss;
    [SerializeField] private EnemyHealth bossHealth;
    [SerializeField] private BossHealthUI healthUI;
    [SerializeField] private Door rewardDoor;
    [SerializeField] private GameObject rewardBanner;

    [Header("Reward")]
    [SerializeField] private string bossId = "gatekeeper";
    [SerializeField] private string rewardAbilityName = "descentOfAsh";
    [SerializeField] private string rewardAbilityLabel = "Descent of Ash";

    [Header("Banner")]
    [SerializeField] private float bannerDuration = 6f;

    public bool FightActive { get; private set; }

    private Vector2 arenaMin;
    private Vector2 arenaMax;
    private Transform player;
    private PlayerHealth playerHealth;
    private Vector3 bossSpawn;

    void Awake()
    {
        Collider2D source = boundsCollider != null ? boundsCollider : GetComponent<Collider2D>();
        Bounds bounds = source.bounds;
        arenaMin = bounds.min;
        arenaMax = bounds.max;

        // A defeated boss stays defeated across application restarts. The
        // clone scene retains the boss object for authoring, but not at runtime.
        if (SaveManager.EnsureInstance().IsBossDefeated(bossId))
        {
            if (rewardDoor != null && !rewardDoor.IsOpen) rewardDoor.Unlock("Gatekeeper reward");
            if (boss != null) boss.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (FightActive || SaveManager.EnsureInstance().IsBossDefeated(bossId)) return;

        PlayerHealth entering = other.GetComponentInParent<PlayerHealth>();
        if (entering == null || entering.IsDead) return;

        StartFight(entering.transform.root);
    }

    void Update()
    {
        if (!FightActive && bossHealth != null && !bossHealth.IsDead &&
            playerHealth != null && !playerHealth.IsDead)
        {
            // Re-arm on respawn: the bench checkpoint sits inside the arena, so
            // the player can reappear inside the trigger without an enter event.
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger.OverlapPoint(playerHealth.transform.position))
                StartFight(playerHealth.transform.root);
        }

        if (!FightActive || bossHealth == null) return;

        if (bossHealth.IsDead)
            EndFight();
    }

    private void StartFight(Transform playerTransform)
    {
        if (boss == null || bossHealth == null)
        {
            Debug.LogError("[BossArena] boss/bossHealth are not wired — run 'Tools > Soledar > Setup Phase 5 Boss'.", this);
            return;
        }
        if (bossHealth.IsDead) return; // already beaten

        FightActive = true;
        player = playerTransform;
        playerHealth = player.GetComponentInParent<PlayerHealth>();
        bossSpawn = boss.transform.position;

        if (playerHealth != null)
            playerHealth.OnDied += HandlePlayerDeath;

        if (seal != null) seal.enabled = true;
        if (healthUI != null) healthUI.Show(bossHealth, boss.BossName);

        boss.Wake(player, arenaMin + Vector2.one * 0.5f, arenaMax - Vector2.one * 0.5f);
        Debug.Log("[BossArena] The Gatekeeper fight begins.", this);
    }

    private void HandlePlayerDeath()
    {
        // Retry loop: unseal, reset the boss, hide the UI. The player respawns
        // via PlayerRespawn (the bench inside the courtyard is the checkpoint).
        playerHealth.OnDied -= HandlePlayerDeath;
        FightActive = false;

        if (seal != null) seal.enabled = false;
        if (healthUI != null) healthUI.Hide();
        if (boss != null) boss.ResetBoss(bossSpawn);

        Debug.Log("[BossArena] Player died — the fight resets.", this);
    }

    private void EndFight()
    {
        FightActive = false;
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDeath;

        if (seal != null) seal.enabled = false;
        if (healthUI != null) healthUI.Hide();

        // Reward 1: Descent of Ash (persisted + auto-saved by SaveManager).
        SaveManager.EnsureInstance().MarkAbilityUnlocked(rewardAbilityName);
        SaveManager.EnsureInstance().MarkBossDefeated(bossId);

        // Reward 2: the Palace Gate opens for good (persisted like any door).
        if (rewardDoor != null && !rewardDoor.IsOpen)
            rewardDoor.Unlock(rewardAbilityLabel);

        if (rewardBanner != null)
        {
            rewardBanner.SetActive(true);
            CancelInvoke(nameof(HideBanner));
            Invoke(nameof(HideBanner), bannerDuration);
        }

        Debug.Log("[BossArena] The Gatekeeper falls — Descent of Ash acquired, Palace Gate opens.", this);
    }

    private void HideBanner()
    {
        if (rewardBanner != null) rewardBanner.SetActive(false);
    }
}
