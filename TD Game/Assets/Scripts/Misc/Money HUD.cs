using UnityEngine;
using UnityEngine.UI;

public class MoneyHUD : MonoBehaviour
{
    [SerializeField] private Text moneyText;
    [SerializeField] private EconomyManager economy;

    private void Awake()
    {
        if (moneyText == null)
            moneyText = GetComponent<Text>();

        if (economy == null)
            economy = FindFirstObjectByType<EconomyManager>();
    }

    private void OnEnable()
    {
        if (economy != null)
            economy.OnMoneyChanged += HandleMoneyChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (economy != null)
            economy.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(int newMoney)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (moneyText == null) return;

        if (economy == null)
        {
            moneyText.text = "Money: (no EconomyManager found)";
            return;
        }

        moneyText.text = "Money: " + economy.Money;
    }
}
