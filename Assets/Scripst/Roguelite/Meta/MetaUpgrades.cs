using UnityEngine;

public enum UpgradeType { Health, Damage }

/// <summary>Nâng cấp vĩnh viễn mua bằng Cells (giữ lại sau khi chết) – cốt lõi của roguelite.</summary>
public static class MetaUpgrades
{
    public const int MaxLevel = 5;

    public static float BonusHealth => SaveSystem.Data.upgradeHealth * 1f;               // +1 máu / cấp
    public static float DamageMultiplier => 1f + 0.15f * SaveSystem.Data.upgradeDamage;  // +15% / cấp

    public static int GetLevel(UpgradeType t) =>
        t == UpgradeType.Health ? SaveSystem.Data.upgradeHealth : SaveSystem.Data.upgradeDamage;

    public static bool IsMaxed(UpgradeType t) => GetLevel(t) >= MaxLevel;

    public static int GetCost(UpgradeType t) => 15 * (GetLevel(t) + 1);

    public static bool TryBuy(UpgradeType t)
    {
        if (IsMaxed(t)) return false;
        int cost = GetCost(t);
        if (SaveSystem.Data.cells < cost) return false;

        SaveSystem.Data.cells -= cost;
        if (t == UpgradeType.Health) SaveSystem.Data.upgradeHealth++;
        else SaveSystem.Data.upgradeDamage++;
        SaveSystem.Save();
        return true;
    }
}
