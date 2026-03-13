using UnityEngine;
using UnityEngine.UI;

public class PlacementRadialOptionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image towerIcon;
    [SerializeField] private GameObject priceGroup;
    [SerializeField] private Image priceBackground;
    [SerializeField] private Text priceText;

    [SerializeField] private GameObject nameGroup;
    [SerializeField] private Image nameBackground;
    [SerializeField] private Text nameText;

    [Header("Visuals")]
    [SerializeField] private Sprite normalBackground;
    [SerializeField] private Sprite selectedBackground;

    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float selectedScale = 1.4f;
    [SerializeField] private float scaleLerpSpeed = 12f;

    [SerializeField] private Color affordablePriceColor = Color.white;
    [SerializeField] private Color unaffordablePriceColor = Color.red;

    private bool isSelected;
    private float targetScale = 1f;

    private void Update()
    {
        float current = transform.localScale.x;
        float next = Mathf.Lerp(current, targetScale, Time.unscaledDeltaTime * scaleLerpSpeed);
        transform.localScale = new Vector3(next, next, next);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        targetScale = selected ? selectedScale : normalScale;

        if (backgroundImage != null)
            backgroundImage.sprite = selected ? selectedBackground : normalBackground;

        if (selected)
            transform.SetAsLastSibling();
    }

    public void SetupTower(Sprite icon, string name, int price, bool affordable)
    {
        if (towerIcon != null)
        {
            towerIcon.enabled = true;
            towerIcon.sprite = icon;
        }

        if (nameGroup != null)
            nameGroup.SetActive(true);

        if (nameText != null)
            nameText.text = name;

        if (priceGroup != null)
            priceGroup.SetActive(true);

        if (priceText != null)
        {
            priceText.text = price.ToString();
            priceText.color = affordable ? affordablePriceColor : unaffordablePriceColor;
        }
    }

    public void SetupBack(string label = "Back")
    {
        if (towerIcon != null)
            towerIcon.enabled = false;

        if (priceGroup != null)
            priceGroup.SetActive(false);

        if (nameGroup != null)
            nameGroup.SetActive(true);

        if (nameText != null)
            nameText.text = label;
    }
}