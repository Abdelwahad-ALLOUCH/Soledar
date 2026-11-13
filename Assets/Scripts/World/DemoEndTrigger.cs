using UnityEngine;

/// <summary>Shows completion after the Gatekeeper's persistent defeat state exists.</summary>
[RequireComponent(typeof(Collider2D))]
public class DemoEndTrigger : MonoBehaviour
{
    [SerializeField] private string requiredBossId = "gatekeeper";
    [SerializeField] private GameObject completionPanel;
    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null && SaveManager.EnsureInstance().IsBossDefeated(requiredBossId) && completionPanel != null)
            completionPanel.SetActive(true);
    }
}
