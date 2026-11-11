using System;
using UnityEngine;

/// <summary>
/// Data asset describing Soledar's 3-hit combo with the Ashen Khoumya:
///   1 — quick horizontal slash
///   2 — diagonal slash
///   3 — powerful finishing attack
/// Each step carries its own damage, range, timing, knockback, hit-stop and
/// camera shake so balancing is pure data work.
/// Create via: Assets > Create > Soledar > Combat > Attack Combo
/// </summary>
[CreateAssetMenu(fileName = "NewAttackCombo", menuName = "Soledar/Combat/Attack Combo")]
public class AttackComboData : ScriptableObject
{
    [Serializable]
    public class AttackStep
    {
        [Tooltip("Damage dealt by this step.")]
        public int damage = 1;

        [Tooltip("Radius of the hitbox circle.")]
        public float range = 0.5f;

        [Tooltip("Minimum delay before the next attack can start (recovery).")]
        public float recoveryTime = 0.25f;

        [Tooltip("Time after recovery during which pressing again continues the combo.")]
        public float comboWindow = 0.3f;

        [Tooltip("Scales the enemy knockback impulse.")]
        public float knockbackMultiplier = 1f;

        [Tooltip("Micro freeze-frame when this step connects (0 = none).")]
        public float hitStopDuration = 0.04f;

        [Tooltip("Cinemachine impulse magnitude when this step connects.")]
        public float shakeForce = 0.2f;
    }

    [Tooltip("Combo steps, executed in order. Loop back to step 0 when the combo expires.")]
    public AttackStep[] steps = Array.Empty<AttackStep>();
}
