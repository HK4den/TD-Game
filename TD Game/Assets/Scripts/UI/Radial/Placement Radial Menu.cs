using System.Collections.Generic;
using UnityEngine;

public class PlacementRadialMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerPlacementController placement;
    [SerializeField] private RectTransform optionParent;
    [SerializeField] private PlacementRadialOptionUI optionPrefab;
    [SerializeField] private EconomyManager economy;

    [Header("Layout")]
    [SerializeField] private float radius = 150f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSpeed = 12f;
    [SerializeField] private float shownAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0f;

    private bool visible;

    private List<PlacementRadialOptionUI> options = new List<PlacementRadialOptionUI>();
    private int highlightedIndex = 0;

    private void Awake()
    {
        if (placement == null)
            placement = FindFirstObjectByType<TowerPlacementController>();

        if (economy == null)
            economy = FindFirstObjectByType<EconomyManager>();
    }

    private void Update()
    {
        if (canvasGroup == null)
            return;

        float target = visible ? shownAlpha : hiddenAlpha;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, target, Time.unscaledDeltaTime * fadeSpeed);
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    public void Show()
    {
        visible = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        visible = false;
    }
    public void BuildRadial()
    {
        Clear();

        var towers = placement.GetPlaceableTowers();

        int count = towers.Count + 1; // +1 for Back

        for (int i = 0; i < count; i++)
        {
            var opt = Instantiate(optionPrefab, optionParent);
            options.Add(opt);

            if (i == 0)
            {
                opt.SetupBack();
            }
            else
            {
                var tower = towers[i - 1];
                bool affordable = economy == null || economy.Money >= tower.Cost;

                opt.SetupTower(
                    tower.Icon,
                    tower.DisplayName,
                    tower.Cost,
                    affordable
                );
            }
        }

        LayoutOptions();
        SetHighlighted(0);
    }

    private void OnEnable()
    {
        if (economy != null)
            economy.OnMoneyChanged += HandleMoneyChanged;
    }

    private void OnDisable()
    {
        if (economy != null)
            economy.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(int _)
    {
        if (gameObject.activeInHierarchy)
            BuildRadial();
    }
    void LayoutOptions()
    {
        int count = options.Count;

        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = 90f - step * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 pos = new Vector2(
                Mathf.Cos(rad),
                Mathf.Sin(rad)
            ) * radius;

            RectTransform rt = options[i].GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
        }
    }

    void Clear()
    {
        foreach (var o in options)
            Destroy(o.gameObject);

        options.Clear();
    }

    public void UpdateHighlight(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            SetHighlighted(0);
            return;
        }

        direction.Normalize();

        float bestDot = -999f;
        int best = 0;

        for (int i = 0; i < options.Count; i++)
        {
            Vector2 optionDir = ((RectTransform)options[i].transform).anchoredPosition.normalized;

            float dot = Vector2.Dot(direction, optionDir);

            if (dot > bestDot)
            {
                bestDot = dot;
                best = i;
            }
        }

        SetHighlighted(best);
    }

    void SetHighlighted(int index)
    {
        highlightedIndex = index;

        for (int i = 0; i < options.Count; i++)
            options[i].SetSelected(i == index);
    }

    public int GetHighlightedIndex()
    {
        return highlightedIndex;
    }
}