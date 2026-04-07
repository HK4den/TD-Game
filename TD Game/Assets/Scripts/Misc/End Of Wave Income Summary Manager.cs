using System.Collections;
using System.Collections.Generic;
using System.Text;
using DamageNumbersPro;
using TMPro;
using UnityEngine;

public class EndOfWaveIncomeSummaryManager : MonoBehaviour
{
    private class GroupSummary
    {
        public string familyKey;
        public int totalAmount;
        public int towerCount;
    }

    [Header("Refs")]
    [SerializeField] private WaveSpawner waveSpawner;
    [SerializeField] private EconomyManager economy;

    [Header("DNP Total Popup (GUI)")]
    [SerializeField] private DamageNumber positiveTotalPopupPrefab;
    [SerializeField] private DamageNumber negativeTotalPopupPrefab;
    [SerializeField] private DamageNumber zeroTotalPopupPrefab;
    [SerializeField] private RectTransform totalPopupAnchor;

    [Header("Per-Tower World Popup")]
    [SerializeField] private bool showPerTowerWorldPopups = true;
    [SerializeField] private DamageNumber positiveWorldPopupPrefab;
    [SerializeField] private DamageNumber negativeWorldPopupPrefab;
    [SerializeField] private DamageNumber zeroWorldPopupPrefab;
    [SerializeField] private Vector3 perTowerWorldOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Breakdown UI")]
    [SerializeField] private CanvasGroup breakdownCanvasGroup;
    [SerializeField] private TMP_Text breakdownText;

    [Header("Timing")]
    [SerializeField] private float showDelayAfterWaveComplete = 1.5f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float visibleDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Text Colors")]
    [SerializeField] private string positiveHexColor = "#6CFF7A";
    [SerializeField] private string negativeHexColor = "#FF6B6B";
    [SerializeField] private string zeroHexColor = "#CFCFCF";

    private Coroutine currentBreakdownRoutine;

    private void Awake()
    {
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<WaveSpawner>();
        if (economy == null) economy = FindFirstObjectByType<EconomyManager>();

        if (breakdownCanvasGroup != null)
            breakdownCanvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        if (waveSpawner != null)
            waveSpawner.OnWaveCompleted += HandleWaveCompleted;
    }

    private void OnDisable()
    {
        if (waveSpawner != null)
            waveSpawner.OnWaveCompleted -= HandleWaveCompleted;
    }

    private void HandleWaveCompleted(int waveNumber, int reward)
    {
        EndOfWaveMoneyTower[] towers = FindObjectsByType<EndOfWaveMoneyTower>(FindObjectsSortMode.None);

        Dictionary<string, GroupSummary> groups = new Dictionary<string, GroupSummary>();
        int towerMoneyTotal = 0;

        for (int i = 0; i < towers.Length; i++)
        {
            EndOfWaveMoneyTower tower = towers[i];
            if (tower == null || !tower.isActiveAndEnabled)
                continue;

            int rolledAmount = tower.RollMoneyChange();
            towerMoneyTotal += rolledAmount;

            TowerVisualSquash visualSquash = tower.GetComponent<TowerVisualSquash>();
            if (visualSquash == null)
                visualSquash = tower.GetComponentInChildren<TowerVisualSquash>();

            if (visualSquash != null)
                visualSquash.TriggerMoneyPulse();

            if (economy != null)
                economy.AdjustMoneySigned(rolledAmount);



            if (showPerTowerWorldPopups)
                SpawnPerTowerWorldPopup(tower.transform.position + perTowerWorldOffset, rolledAmount);

            string familyKey = tower.FamilyKey;
            if (string.IsNullOrWhiteSpace(familyKey))
                familyKey = "Unknown";

            if (!groups.TryGetValue(familyKey, out GroupSummary summary))
            {
                summary = new GroupSummary
                {
                    familyKey = familyKey,
                    totalAmount = 0,
                    towerCount = 0
                };
                groups.Add(familyKey, summary);
            }

            summary.totalAmount += rolledAmount;
            summary.towerCount++;
        }

        int totalDelta = reward + towerMoneyTotal;

        SpawnTotalPopup(totalDelta);

        if (groups.Count > 0)
        {
            string builtText = BuildBreakdownText(reward, groups);

            if (currentBreakdownRoutine != null)
                StopCoroutine(currentBreakdownRoutine);

            currentBreakdownRoutine = StartCoroutine(ShowBreakdownRoutine(builtText));
        }
        else
        {
            if (currentBreakdownRoutine != null)
            {
                StopCoroutine(currentBreakdownRoutine);
                currentBreakdownRoutine = null;
            }

            if (breakdownCanvasGroup != null)
                breakdownCanvasGroup.alpha = 0f;

            if (breakdownText != null)
                breakdownText.text = string.Empty;
        }
    }

    private void SpawnTotalPopup(int totalDelta)
    {
        if (totalPopupAnchor == null)
            return;

        DamageNumber prefabToUse = null;

        if (totalDelta > 0)
            prefabToUse = positiveTotalPopupPrefab;
        else if (totalDelta < 0)
            prefabToUse = negativeTotalPopupPrefab;
        else
            prefabToUse = zeroTotalPopupPrefab;

        if (prefabToUse == null)
            return;

        DamageNumber dn = prefabToUse.Spawn(Vector3.zero);
        dn.leftText = FormatMoneySigned(totalDelta);
        dn.SetAnchoredPosition(totalPopupAnchor, Vector2.zero);
    }

    private void SpawnPerTowerWorldPopup(Vector3 worldPosition, int amount)
    {
        DamageNumber prefabToUse = null;

        if (amount > 0)
            prefabToUse = positiveWorldPopupPrefab;
        else if (amount < 0)
            prefabToUse = negativeWorldPopupPrefab;
        else
            prefabToUse = zeroWorldPopupPrefab;

        if (prefabToUse == null)
            return;

        DamageNumber dn = prefabToUse.Spawn(worldPosition);
        dn.leftText = FormatMoneySigned(amount);
    }

    private string BuildBreakdownText(int reward, Dictionary<string, GroupSummary> groups)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(BuildColoredLine("Round Finished", reward));

        foreach (KeyValuePair<string, GroupSummary> pair in groups)
        {
            GroupSummary group = pair.Value;
            sb.AppendLine();

            string label = group.familyKey;
            if (group.towerCount > 1)
                label += "s";

            sb.Append(BuildColoredLine(label, group.totalAmount));
        }

        return sb.ToString();
    }

    private string BuildColoredLine(string label, int amount)
    {
        string color = GetColorForAmount(amount);
        return $"<color={color}>{label}: {FormatMoneySigned(amount)}</color>";
    }

    private string GetColorForAmount(int amount)
    {
        if (amount > 0)
            return positiveHexColor;

        if (amount < 0)
            return negativeHexColor;

        return zeroHexColor;
    }

    private string FormatMoneySigned(int amount)
    {
        if (amount > 0)
            return $"+${amount}";

        if (amount < 0)
            return $"-${Mathf.Abs(amount)}";

        return "+$0";
    }

    private IEnumerator ShowBreakdownRoutine(string textToShow)
    {
        if (breakdownCanvasGroup == null || breakdownText == null)
            yield break;

        breakdownCanvasGroup.alpha = 0f;
        breakdownText.text = string.Empty;

        yield return WaitForSecondsGameplay(showDelayAfterWaveComplete);

        breakdownText.text = textToShow;

        yield return FadeCanvasGroup(breakdownCanvasGroup, 0f, 1f, fadeInDuration);
        yield return WaitForSecondsGameplay(visibleDuration);
        yield return FadeCanvasGroup(breakdownCanvasGroup, 1f, 0f, fadeOutDuration);

        breakdownText.text = string.Empty;
        currentBreakdownRoutine = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float timer = 0f;
        group.alpha = from;

        while (timer < duration)
        {
            if (!PauseState.IsPaused)
                timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator WaitForSecondsGameplay(float seconds)
    {
        if (seconds <= 0f)
            yield break;

        float timer = 0f;
        while (timer < seconds)
        {
            if (!PauseState.IsPaused)
                timer += Time.deltaTime;

            yield return null;
        }
    }
}