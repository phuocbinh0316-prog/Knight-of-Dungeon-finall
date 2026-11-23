using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Hệ thống Lời Nguyền (Curse — x2.5 Sát thương nhận vào) từ Rương Nguyền Rủa:
/// Mở Rương Nguyền Rủa nhận ngay Scroll + Vàng + Cells, nhưng chịu x2.5 sát thương nhận vào cho đến khi hạ đủ 5 quái!
/// </summary>
public static class CurseSystem
{
    public static int CurseKillsRemaining { get; private set; }
    public static bool IsCursed => CurseKillsRemaining > 0;
    public static float DamageTakenMultiplier => IsCursed ? 2.5f : 1f;

    public static event Action OnCurseChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStaticOnPlay()
    {
        CurseKillsRemaining = 0;
        OnCurseChanged = null;
    }

    public static void ResetCurse()
    {
        CurseKillsRemaining = 0;
        OnCurseChanged?.Invoke();
    }

    public static void ApplyCurse(int killsRequired = 5)
    {
        CurseKillsRemaining += Mathf.Max(1, killsRequired);
        OnCurseChanged?.Invoke();

        var p = UnityEngine.Object.FindAnyObjectByType<Player>();
        if (p != null)
        {
            DamagePopup.SpawnText(p.transform.position + Vector3.up * 1.3f, $"CURSED! ({CurseKillsRemaining} KILLS)", new Color(0.92f, 0.15f, 0.95f), 7.4f);
            CombatVFX.SpawnShockwave(p.transform.position, new Color(0.85f, 0.1f, 0.95f, 0.95f), 0.3f, 3.2f, 0.42f);
        }
        CameraShake.Shake(0.35f);
    }

    public static void OnEnemyKilled()
    {
        if (!IsCursed) return;
        CurseKillsRemaining = Mathf.Max(0, CurseKillsRemaining - 1);
        OnCurseChanged?.Invoke();

        var p = UnityEngine.Object.FindAnyObjectByType<Player>();
        if (p != null)
        {
            if (CurseKillsRemaining == 0)
            {
                DamagePopup.SpawnText(p.transform.position + Vector3.up * 1.3f, "CURSE LIFTED!", new Color(0.35f, 1f, 0.55f), 8.0f);
                CombatVFX.SpawnShockwave(p.transform.position, new Color(0.35f, 1f, 0.6f, 0.95f), 0.35f, 3.4f, 0.4f);
                if (AudioManager.Instance != null) AudioManager.Instance.SFX_Win();
            }
            else
            {
                DamagePopup.SpawnText(p.transform.position + Vector3.up * 1.15f, $"CURSE: {CurseKillsRemaining} LEFT", new Color(0.95f, 0.35f, 1f), 6.0f);
            }
        }
    }
}
