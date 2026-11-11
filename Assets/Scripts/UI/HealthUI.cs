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

    /// <summary>Enables hearts 0..currentHealth-1, disables the rest. Public for external refresh requests.</summary>
    public void UpdateHearts(int currentHealth)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] != null)
                hearts[i].enabled = i < currentHealth;
        }
    }
}
