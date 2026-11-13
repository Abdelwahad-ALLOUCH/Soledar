using UnityEngine;

/// <summary>One-item merchant. Purchase and currency state persist through SaveManager.</summary>
[RequireComponent(typeof(Collider2D))]
public class MerchantInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemId = "merchant_wind_charm";
    [SerializeField] private string itemName = "Wind of the Atlas";
    [SerializeField] private int price = 25;
    [SerializeField] private string grantedAbility = "windOfAtlas";
    public bool CanInteract { get { return true; } }
    public string GetPrompt()
    {
        SaveManager save = SaveManager.EnsureInstance();
        return save.IsItemCollected(itemId) ? "Thank the Silent Merchant" : $"Buy {itemName} ({price} dirhams)";
    }
    public void Interact(GameObject interactor)
    {
        SaveManager save = SaveManager.EnsureInstance();
        if (save.IsItemCollected(itemId)) return;
        if (!save.TrySpendDirhams(price, itemName)) { Debug.Log($"[Merchant] Need {price} Ancient Dirhams for {itemName}.", this); return; }
        save.MarkItemCollected(itemId);
        save.MarkAbilityUnlocked(grantedAbility);
        Debug.Log($"[Merchant] Purchased {itemName}.", this);
    }
    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
}
