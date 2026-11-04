using System;
using UnityEngine;

/// <summary>Gắn vào Player. Giữ vũ khí hiện tại và cung cấp chỉ số cho Player.Attack().</summary>
public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private WeaponData startingWeapon;
    [SerializeField] private float fallbackDamage = 1f;
    [SerializeField] private float fallbackCooldown = 0.5f;
    [SerializeField] private float fallbackRange = 1f;

    public WeaponData Current { get; private set; }
    public event Action<WeaponData> onWeaponChanged;

    public float Damage => Current != null ? Current.damage : fallbackDamage;
    public float Cooldown => Current != null ? Current.cooldown : fallbackCooldown;
    public float Range => Current != null ? Current.range : fallbackRange;

    private void Awake()
    {
        Current = startingWeapon;
    }

    public void Equip(WeaponData weapon)
    {
        Current = weapon;
        onWeaponChanged?.Invoke(weapon);
    }
}
