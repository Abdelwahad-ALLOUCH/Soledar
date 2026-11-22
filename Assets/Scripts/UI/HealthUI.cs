using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mask-style health display. Builds one heart icon per max-health point and
/// toggles them as health changes. Subscribes to PlayerHealth.OnHealthChanged,
/// fixing the original prototype's bug where hearts never refreshed after damage.
///
/// Wiring (configured by Tools > Soledar > Setup Phase 1 Scene):
///   - Lives on the scene Canvas object ("UI") in TestScene.
///   - playerHealth: the Player's PlayerHealth component.
///   - heartPrefab: Assets/Prefabs/Heart.prefab.
///   - heartsParent: the "HeartsContainer" Transform under this Canvas.
/// </summary>
public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Transform heartsParent;

    [Header("Mask Appearance")]
    [SerializeField] private Color filledColor = new Color(0.937f, 0.894f, 0.808f, 1f); // ivory
    [SerializeField] private Color emptyColor = new Color(0.431f, 0.388f, 0.325f, 0.65f); // dimmed slot

    private readonly List<Image> hearts = new List<Image>();
    private bool subscribed;

    void OnEnable()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
            if (playerHealth == null)
            {
                Debug.LogError("[HealthUI] No PlayerHealth found in the scene. Assign one in the Inspector.", this);
                return;
            }
        }

        playerHealth.OnHealthChanged += HandleHealthChanged;
        subscribed = true;
        RebuildHearts();
    }

    void OnDisable()
    {
        if (subscribed && playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        subscribed = false;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (max != hearts.Count)
            RebuildHearts();
        else
            UpdateHearts(current);
    }

    private void RebuildHearts()
    {
        if (heartPrefab == null || heartsParent == null)
        {
            Debug.LogError(
                "[HealthUI] heartPrefab or heartsParent is not assigned. " +
                "Run 'Tools > Soledar > Setup Phase 1 Scene' with the TestScene open.",
                this);
            return;
        }

        foreach (Transform child in heartsParent)
            Destroy(child.gameObject);

        hearts.Clear();

        for (int i = 0; i < playerHealth.MaxHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsParent);
            Image image = heart.GetComponent<Image>();
            if (image == null)
                image = heart.AddComponent<Image>();
            hearts.Add(image);
        }

        UpdateHearts(playerHealth.CurrentHealth);
    }

    /// <summary>
    /// Tints filled masks ivory and spent masks dim (the slot stays visible, so
    /// the player can read maximum health at a glance). Public for external
    /// refresh requests.
    /// </summary>
    public void UpdateHearts(int currentHealth)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null) continue;
            hearts[i].color = i < currentHealth ? filledColor : emptyColor;
        }
    }
}
