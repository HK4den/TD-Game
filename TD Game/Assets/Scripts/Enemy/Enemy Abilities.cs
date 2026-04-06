using UnityEngine;

public class EnemyAbilities : MonoBehaviour
{
    [Header("Slow Resistance")]
    [SerializeField] private bool slowImmune = false;

    [Tooltip("0 = no resistance, 0.5 = reduce slows by 50%, 1 = fully negate slows.")]
    [Range(0f, 1f)]
    [SerializeField] private float slowResistancePercent = 0f;

    [Header("Extra Damage Taken Resistance")]
    [SerializeField] private bool extraDamageTakenImmune = false;

    [Tooltip("0 = no resistance, 0.5 = reduce extra damage taken effects by 50%, 1 = fully negate them.")]
    [Range(0f, 1f)]
    [SerializeField] private float extraDamageTakenResistancePercent = 0f;

    public bool SlowImmune => slowImmune;
    public float SlowResistancePercent => Mathf.Clamp01(slowResistancePercent);

    public bool ExtraDamageTakenImmune => extraDamageTakenImmune;
    public float ExtraDamageTakenResistancePercent => Mathf.Clamp01(extraDamageTakenResistancePercent);

    public float AdjustSlowPercent(float incomingSlowPercent)
    {
        float clamped = Mathf.Clamp01(incomingSlowPercent);

        if (slowImmune)
            return 0f;

        return clamped * (1f - SlowResistancePercent);
    }

    public float AdjustExtraDamageTakenPercent(float incomingPercent)
    {
        // Positive values mean "take more damage" and are resisted.
        // Negative values mean "take less damage" and are allowed through unchanged.
        if (incomingPercent < 0f)
            return incomingPercent;

        float clamped = Mathf.Max(0f, incomingPercent);

        if (extraDamageTakenImmune)
            return 0f;

        return clamped * (1f - ExtraDamageTakenResistancePercent);
    }
}