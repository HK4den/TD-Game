using UnityEngine;
using UnityEngine.UI;

public class PlacementRadialOptionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image towerIcon;
    [SerializeField] private Text priceText;
    [SerializeField] private Text nameText;

    [Header("Visuals")]
    [SerializeField] private Sprite normalBackground;
    [SerializeField] private Sprite selectedBackground;

    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float selectedScale = 1.4f;
    [SerializeField] private float scaleLerpSpeed = 12f;

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
    }

    public void SetupTower(Sprite icon, string name, int price, bool affordable)
    {
        if (towerIcon != null)
        {
            towerIcon.enabled = true;
            towerIcon.sprite = icon;
        }

        if (nameText != null)
            nameText.text = name;

        if (priceText != null)
        {
            priceText.gameObject.SetActive(true);
            priceText.text = price.ToString();
            priceText.color = affordable ? Color.white : Color.red;
        }
    }

    public void SetupBack(string label = "Back")
    {
        if (towerIcon != null)
            towerIcon.enabled = false;

        if (priceText != null)
            priceText.gameObject.SetActive(false);

        if (nameText != null)
            nameText.text = label;
    }
}