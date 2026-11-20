using System;
using UnityEngine;

/// <summary>
/// Theo dõi chuỗi Kill Streak (hạ gục liên tiếp không mất máu) kiểu Dead Cells & Thống kê tổng kết Run.
/// Đạt mốc 5, 10, 15... mạng không trúng đòn sẽ thưởng Cells và tặng ngay 1 Power Scroll chọn 1 trong 3!
/// </summary>
public class KillStreakManager : MonoBehaviour
{
    private static KillStreakManager instance;

    public int CurrentStreak { get; private set; }
    public int BestStreak { get; private set; }
    public int TotalKills { get; private set; }
    public int TotalParries { get; private set; }
    public int ScrollsCollected { get; private set; }
    public float TotalDamageDealt { get; private set; }
    public float RunStartTime { get; private set; }

    public const int ScrollMilestoneInterval = 5; // Cứ 5 mạng không mất máu thưởng 1 Power Scroll!

    public event Action OnUpdated;

    public static KillStreakManager Get()
    {
        if (instance == null)
        {
            var go = new GameObject("KillStreakManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<KillStreakManager>();
            instance.RunStartTime = Time.unscaledTime;
        }
        return instance;
    }

    public void ResetStats()
    {
        CurrentStreak = 0;
        BestStreak = 0;
        TotalKills = 0;
        TotalParries = 0;
        ScrollsCollected = 0;
        TotalDamageDealt = 0f;
        RunStartTime = Time.unscaledTime;
        ScrollUpgradeUI.ResetRunStats();
        OnUpdated?.Invoke();
    }

    public void RegisterKill(Vector3 enemyPos)
    {
        TotalKills++;
        CurrentStreak++;
        if (CurrentStreak > BestStreak) BestStreak = CurrentStreak;

        RunManager.Get().RegisterKill();

        if (CurrentStreak > 0 && CurrentStreak % ScrollMilestoneInterval == 0)
        {
            int bonusCells = 5 * (CurrentStreak / ScrollMilestoneInterval);
            RunManager.Get().AddCells(bonusCells);

            var player = FindAnyObjectByType<Player>();
            Vector3 popPos = player != null ? player.transform.position : enemyPos;
            DamagePopup.SpawnText(popPos + Vector3.up * 1.2f, $"{CurrentStreak}x UNTOUCHABLE! +{bonusCells} CELLS", new Color(1f, 0.88f, 0.2f), 7.2f);
            CombatVFX.SpawnShockwave(popPos, new Color(1f, 0.85f, 0.2f, 0.9f), 0.4f, 3.0f, 0.4f);

            // Tặng ngay 1 cuộn sức mạnh Power Scroll (Chọn 1 trong 3) như phần thưởng cửa Kill Streak của Dead Cells!
            ScrollUpgradeUI.ShowScrollChoice($"KILL STREAK {CurrentStreak}x REWARD!");
        }
        else if (CurrentStreak >= 3)
        {
            DamagePopup.SpawnText(enemyPos + Vector3.up * 1.0f, $"{CurrentStreak}x STREAK!", new Color(0.95f, 0.78f, 0.25f), 5.6f);
        }

        OnUpdated?.Invoke();
    }

    public void OnPlayerHit()
    {
        if (CurrentStreak >= 3)
        {
            var player = FindAnyObjectByType<Player>();
            if (player != null)
            {
                DamagePopup.SpawnText(player.transform.position + Vector3.up * 1.1f, "STREAK BROKEN!", new Color(1f, 0.35f, 0.35f), 5.8f);
            }
        }
        CurrentStreak = 0;
        OnUpdated?.Invoke();
    }

    public void RegisterDamageDealt(float dmg)
    {
        TotalDamageDealt += Mathf.Max(0f, dmg);
    }

    public void RegisterParry()
    {
        TotalParries++;
        OnUpdated?.Invoke();
    }

    public void RegisterScrollCollected()
    {
        ScrollsCollected++;
        OnUpdated?.Invoke();
    }

    public float GetElapsedSeconds() => Mathf.Max(0f, Time.unscaledTime - RunStartTime);
}
