using TMPro;
using UnityEngine;

/// <summary>Panel cửa hàng nâng cấp trong MainMenu. Gán text + nối 2 nút OnClick tới BuyHealth/BuyDamage.</summary>
public class UpgradeShopUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cellsText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageText;

    private void OnEnable() => Refresh();

    public void BuyHealth() { MetaUpgrades.TryBuy(UpgradeType.Health); Refresh(); }
    public void BuyDamage() { MetaUpgrades.TryBuy(UpgradeType.Damage); Refresh(); }

    public void Refresh()
    {
        if (cellsText != null) cellsText.text = "Cells: " + SaveSystem.Data.cells;
        if (healthText != null) healthText.text = Describe("Max HP", UpgradeType.Health);
        if (damageText != null) damageText.text = Describe("Damage", UpgradeType.Damage);
    }

    private static string Describe(string label, UpgradeType t)
    {
        int lv = MetaUpgrades.GetLevel(t);
        return MetaUpgrades.IsMaxed(t)
            ? $"{label}  Lv {lv}/{MetaUpgrades.MaxLevel}  (MAX)"
            : $"{label}  Lv {lv}/{MetaUpgrades.MaxLevel}  - {MetaUpgrades.GetCost(t)} cells";
    }
}
