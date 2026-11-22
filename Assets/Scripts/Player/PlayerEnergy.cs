using System;
using UnityEngine;

/// <summary>
/// Echo Energy — the resource that pays for Lantern healing.
///
/// Gained by striking enemies (every hit that connects, so pressure in combat
/// is what buys survival) and spent by PlayerHealing. Not persisted: a bench
/// rest refills it, and a fresh scene starts the run at its starting value.
///
/// Subscribes to PlayerAttack.AttackConnected instead of polling hitboxes, and
/// lives on the Player GameObject.
/// </summary>
[DisallowMultipleComponent]
public class PlayerEnergy : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField] private int maxEnergy = 100;
    [SerializeField, Range(0, 100)] private int startingEnergy = 0;

    [Header("Gain")]
    [Tooltip("Energy added for each enemy an attack connects with.")]
    [SerializeField] private int energyPerHit = 9;

    /// <summary>(current, max). Raised whenever the value changes.</summary>
    public event Action<int, int> OnEnergyChanged;

    public int Current { get; private set; }
    public int Max { get { return maxEnergy; } }
    public bool IsFull { get { return Current >= maxEnergy; } }
    public bool HasEnough(int amount) { return amount > 0 && Current >= amount; }

    private PlayerAttack attack;
    private bool subscribed;

    void Awake()
    {
        Current = Mathf.Clamp(startingEnergy, 0, maxEnergy);
    }

    void OnEnable()
    {
        if (!subscribed)
        {
            attack = GetComponent<PlayerAttack>();
            if (attack != null)
            {
                attack.AttackConnected += HandleAttackConnected;
                subscribed = true;
            }
        }

        RaiseChanged();
    }

    void OnDisable()
    {
        if (subscribed && attack != null)
            attack.AttackConnected -= HandleAttackConnected;
        subscribed = false;
    }

    private void HandleAttackConnected(int enemiesHit)
    {
        if (enemiesHit <= 0 || energyPerHit <= 0) return;
        AddEnergy(energyPerHit * enemiesHit);
    }

    /// <summary>Adds energy, clamped to capacity. Silent when already full.</summary>
    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;

        int next = Mathf.Min(maxEnergy, Current + amount);
        if (next == Current) return;

        Current = next;
        RaiseChanged();
    }

    /// <summary>Spends energy only if the whole amount is available.</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || Current < amount) return false;

        Current -= amount;
        RaiseChanged();
        return true;
    }

    /// <summary>Bench rest: the benches restore health, breathing and energy alike.</summary>
    public void Refill()
    {
        if (Current == maxEnergy) return;
        Current = maxEnergy;
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        if (OnEnergyChanged != null) OnEnergyChanged(Current, maxEnergy);
    }
}
