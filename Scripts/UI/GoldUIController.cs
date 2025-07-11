using TMPro;
using UnityEngine;

public class GoldUIController : MonoBehaviour
{
    public CurrencyManager currencyManager;
    public TextMeshProUGUI goldText;

    void Start()
    {
        currencyManager.onGoldChanged += UpdateGoldUI;
        UpdateGoldUI(currencyManager.GetGold());
    }

    void UpdateGoldUI(double gold)
    {
        goldText.text = $"{gold:N0}";
    }
}