using UnityEngine;

/// <summary>
/// One-item merchant. Purchase and currency state persist through SaveManager.
///
/// Phase 6 "merchant chain": <see cref="requiresAbility"/> gates the stock on an
/// earlier ability — the Silent Palace's Lantern Keeper only sells Shadow
/// Passage to a wanderer who already owns the Wind of the Atlas.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MerchantInteractable : MonoBehaviour, IInteractable
{
    [Header("Stock")]
    [SerializeField] private string itemId = "merchant_wind_charm";
    [SerializeField] private string itemName = "Wind of the Atlas";
    [SerializeField] private int price = 25;
    [SerializeField] private string grantedAbility = "windOfAtlas";

    [Header("Merchant Chain")]
    [Tooltip("Optional: the merchant refuses to trade until this ability is owned.")]
    [SerializeField] private string requiresAbility = "";
    [Tooltip("Prompt shown while the chain requirement is missing.")]
    [SerializeField] private string lockedPrompt = "The keeper has nothing for you yet";

    public bool CanInteract { get { return true; } }

    public string GetPrompt()
    {
        SaveManager save = SaveManager.EnsureInstance();

        if (IsLockedOut(save)) return lockedPrompt;
        return save.IsItemCollected(itemId) ? $"Thank {MerchantName()}" : $"Buy {itemName} ({price} dirhams)";
    }

    public void Interact(GameObject interactor)
    {
        SaveManager save = SaveManager.EnsureInstance();

        if (IsLockedOut(save))
        {
            Debug.Log($"[Merchant] Refused: '{requiresAbility}' has not been unlocked yet.", this);
            return;
        }

        if (save.IsItemCollected(itemId)) return;

        if (!save.TrySpendDirhams(price, itemName))
        {
            Debug.Log($"[Merchant] Need {price} Ancient Dirhams for {itemName}.", this);
            return;
        }

        save.MarkItemCollected(itemId);
        save.MarkAbilityUnlocked(grantedAbility);
        Debug.Log($"[Merchant] Purchased {itemName} — ability '{grantedAbility}' unlocked.", this);
    }

    /// <summary>True while the merchant-chain requirement is still missing.</summary>
    private bool IsLockedOut(SaveManager save)
    {
        return !string.IsNullOrEmpty(requiresAbility) && !save.HasAbility(requiresAbility);
    }

    private string MerchantName()
    {
        return name.Contains("Lantern") ? "the Lantern Keeper" : "the Silent Merchant";
    }

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
}
