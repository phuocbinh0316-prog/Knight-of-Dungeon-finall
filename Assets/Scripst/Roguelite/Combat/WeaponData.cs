using UnityEngine;

public enum WeaponRarity { Common, Rare, Epic }

/// <summary>Dữ liệu một vũ khí. Tạo bằng: chuột phải trong Project > Create > Knight > Weapon.</summary>
[CreateAssetMenu(menuName = "Knight/Weapon", fileName = "NewWeapon")]
public class WeaponData : ScriptableObject
{
    public string displayName = "Sword";
    [TextArea] public string description;
    public Sprite icon;
    public WeaponRarity rarity = WeaponRarity.Common;

    [Header("Chỉ số")]
    public float damage = 1f;
    public float cooldown = 0.5f;
    public float range = 1f;
}
