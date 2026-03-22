using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerCombatStats : MonoBehaviour
{
    [Serializable]
    public class FloatStat
    {
        [SerializeField] private float baseValue = 1f;

        private readonly List<float> flatModifiers = new List<float>();
        private readonly List<float> multiplierModifiers = new List<float>();

        public float BaseValue
        {
            get => baseValue;
            set => baseValue = value;
        }

        public float GetValue(float minValue = 0f)
        {
            float flatTotal = 0f;
            for (int i = 0; i < flatModifiers.Count; i++)
                flatTotal += flatModifiers[i];

            float multiplierTotal = 1f;
            for (int i = 0; i < multiplierModifiers.Count; i++)
                multiplierTotal *= multiplierModifiers[i];

            float result = (baseValue + flatTotal) * multiplierTotal;
            return Mathf.Max(minValue, result);
        }

        public void AddFlat(float amount) => flatModifiers.Add(amount);
        public void AddMultiplier(float amount) => multiplierModifiers.Add(amount);

        public void RemoveFlat(float amount) => flatModifiers.Remove(amount);
        public void RemoveMultiplier(float amount) => multiplierModifiers.Remove(amount);

        public void ClearModifiers()
        {
            flatModifiers.Clear();
            multiplierModifiers.Clear();
        }
    }

    [Header("Base Combat Stats")]
    [SerializeField] private float basePower = 1f;
    [SerializeField] private float baseSecondsBetweenShots = 1f;
    [SerializeField] private float basePierce = 1f;
    [SerializeField] private float baseRange = 4f;

    private readonly FloatStat powerStat = new FloatStat();
    private readonly FloatStat shootIntervalStat = new FloatStat();
    private readonly FloatStat pierceStat = new FloatStat();
    private readonly FloatStat rangeStat = new FloatStat();

    public event Action OnStatsChanged;

    public float BasePower
    {
        get => powerStat.BaseValue;
        set
        {
            powerStat.BaseValue = Mathf.Max(0.001f, value);
            RaiseStatsChanged();
        }
    }

    public float BaseSecondsBetweenShots
    {
        get => shootIntervalStat.BaseValue;
        set
        {
            shootIntervalStat.BaseValue = Mathf.Max(0.001f, value);
            RaiseStatsChanged();
        }
    }

    public int BasePierce
    {
        get => Mathf.Max(1, Mathf.RoundToInt(pierceStat.BaseValue));
        set
        {
            pierceStat.BaseValue = Mathf.Max(1f, value);
            RaiseStatsChanged();
        }
    }

    public float BaseRange
    {
        get => rangeStat.BaseValue;
        set
        {
            rangeStat.BaseValue = Mathf.Max(0.01f, value);
            RaiseStatsChanged();
        }
    }

    public float Power => powerStat.GetValue(0.001f);

    // User-facing name is Shoot Speed, but code logic stores seconds between shots.
    public float SecondsBetweenShots => shootIntervalStat.GetValue(0.001f);

    public int Pierce => Mathf.Max(1, Mathf.RoundToInt(pierceStat.GetValue(1f)));

    public float Range => rangeStat.GetValue(0.01f);

    private void Awake()
    {
        powerStat.BaseValue = Mathf.Max(0.001f, basePower);
        shootIntervalStat.BaseValue = Mathf.Max(0.001f, baseSecondsBetweenShots);
        pierceStat.BaseValue = Mathf.Max(1f, basePierce);
        rangeStat.BaseValue = Mathf.Max(0.01f, baseRange);
    }

    public void AddPowerFlat(float amount)
    {
        powerStat.AddFlat(amount);
        RaiseStatsChanged();
    }

    public void AddPowerMultiplier(float amount)
    {
        powerStat.AddMultiplier(amount);
        RaiseStatsChanged();
    }

    public void RemovePowerFlat(float amount)
    {
        powerStat.RemoveFlat(amount);
        RaiseStatsChanged();
    }

    public void RemovePowerMultiplier(float amount)
    {
        powerStat.RemoveMultiplier(amount);
        RaiseStatsChanged();
    }

    public void AddShootSpeedFlat(float amount)
    {
        shootIntervalStat.AddFlat(amount);
        RaiseStatsChanged();
    }

    public void AddShootSpeedMultiplier(float amount)
    {
        shootIntervalStat.AddMultiplier(amount);
        RaiseStatsChanged();
    }

    public void RemoveShootSpeedFlat(float amount)
    {
        shootIntervalStat.RemoveFlat(amount);
        RaiseStatsChanged();
    }

    public void RemoveShootSpeedMultiplier(float amount)
    {
        shootIntervalStat.RemoveMultiplier(amount);
        RaiseStatsChanged();
    }

    public void AddPierceFlat(float amount)
    {
        pierceStat.AddFlat(amount);
        RaiseStatsChanged();
    }

    public void AddPierceMultiplier(float amount)
    {
        pierceStat.AddMultiplier(amount);
        RaiseStatsChanged();
    }

    public void RemovePierceFlat(float amount)
    {
        pierceStat.RemoveFlat(amount);
        RaiseStatsChanged();
    }

    public void RemovePierceMultiplier(float amount)
    {
        pierceStat.RemoveMultiplier(amount);
        RaiseStatsChanged();
    }

    public void AddRangeFlat(float amount)
    {
        rangeStat.AddFlat(amount);
        RaiseStatsChanged();
    }

    public void AddRangeMultiplier(float amount)
    {
        rangeStat.AddMultiplier(amount);
        RaiseStatsChanged();
    }

    public void RemoveRangeFlat(float amount)
    {
        rangeStat.RemoveFlat(amount);
        RaiseStatsChanged();
    }

    public void RemoveRangeMultiplier(float amount)
    {
        rangeStat.RemoveMultiplier(amount);
        RaiseStatsChanged();
    }

    public void ClearAllModifiers()
    {
        powerStat.ClearModifiers();
        shootIntervalStat.ClearModifiers();
        pierceStat.ClearModifiers();
        rangeStat.ClearModifiers();
        RaiseStatsChanged();
    }

    private void RaiseStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
}