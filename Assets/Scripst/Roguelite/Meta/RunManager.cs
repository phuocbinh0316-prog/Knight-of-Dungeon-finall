using System;
using UnityEngine;

/// <summary>
/// Quản lý 1 "lượt chơi" (run): độ sâu, cell nhặt được, số quái giết. Tự tạo khi cần, sống qua các scene.
/// </summary>
public class RunManager : MonoBehaviour
{
    private static RunManager instance;

    public int Depth { get; private set; } = 1;
    public int CellsThisRun { get; private set; }
    public int Gold { get; private set; }
    public int Kills { get; private set; }

    public event Action onChanged;

    public static RunManager Get()
    {
        if (instance == null)
        {
            var go = new GameObject("RunManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<RunManager>();
        }
        return instance;
    }

    public void AddCells(int amount)
    {
        CellsThisRun += amount;
        onChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        onChanged?.Invoke();
    }

    public bool TrySpendGold(int cost)
    {
        if (Gold < cost) return false;
        Gold -= cost;
        onChanged?.Invoke();
        return true;
    }

    public void RegisterKill()
    {
        Kills++;
        onChanged?.Invoke();
    }

    /// <summary>Chuyển cell của lượt này vào kho vĩnh viễn (gọi khi chết hoặc thắng màn).</summary>
    public void BankCells()
    {
        if (CellsThisRun > 0)
        {
            SaveSystem.Data.cells += CellsThisRun;
            SaveSystem.Save();
        }
        SaveSystem.SubmitDepth(Depth);
        CellsThisRun = 0;
        onChanged?.Invoke();
    }

    public void NextDepth()
    {
        Depth++;
        onChanged?.Invoke();
    }

    /// <summary>Bắt đầu lượt mới (gọi khi bấm Play từ menu).</summary>
    public void ResetRun()
    {
        Depth = 1;
        CellsThisRun = 0;
        Gold = 0;
        Kills = 0;
        onChanged?.Invoke();
    }
}
