using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss health bar (Phase 5): top-center title + bar, shown only while a boss
/// fight is running. BossArena binds it to the boss's EnemyHealth on fight
/// start and hides it on victory or retry.
///
/// Wiring (Phase 5 setup tool): lives on the scene Canvas 'UI' with
/// barPanel (inactive by default), fillImage (Image.Type.Filled) and
/// titleText wired below.
/// </summary>
[DisallowMultipleComponent]
public class BossHealthUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject barPanel;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text titleText;

    private EnemyHealth boundHealth;

    void Awake()
    {
        if (barPanel != null) barPanel.SetActive(false);
    }

    public void Show(EnemyHealth health, string title)
    {
        Unbind();
        boundHealth = health;

        if (titleText != null) titleText.text = title;

        if (boundHealth != null)
        {
            boundHealth.OnHealthChanged += Refresh;
            Refresh(boundHealth.CurrentHealth, boundHealth.MaxHealth);
        }

        if (barPanel != null) barPanel.SetActive(true);
    }

    public void Hide()
    {
        Unbind();
        if (barPanel != null) barPanel.SetActive(false);
    }

    private void Unbind()
    {
        if (boundHealth != null)
            boundHealth.OnHealthChanged -= Refresh;
        boundHealth = null;
    }

    private void Refresh(int current, int max)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
    }
}
