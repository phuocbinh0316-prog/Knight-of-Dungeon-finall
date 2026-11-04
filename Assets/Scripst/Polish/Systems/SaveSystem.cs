using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int unlockedLevel = 1;
    public int highScore = 0;
    public int totalCoins = 0;
    public float masterVolume = 1f;

    // --- Roguelite (gói 2) ---
    public int cells = 0;           // tiền vĩnh viễn
    public int upgradeHealth = 0;   // cấp nâng cấp máu
    public int upgradeDamage = 0;   // cấp nâng cấp sát thương
    public int bestDepth = 1;       // độ sâu cao nhất đạt được
}

/// <summary>Lưu/đọc tiến trình bằng JSON trong PlayerPrefs. (Bản này GHI ĐÈ SaveSystem.cs của gói 1.)</summary>
public static class SaveSystem
{
    private const string Key = "KnightOfDungeon_Save_v1";
    private static SaveData data;

    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    public static void Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
        if (data == null) data = new SaveData();
    }

    public static void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }

    public static void UnlockLevel(int level)
    {
        if (level > Data.unlockedLevel)
        {
            Data.unlockedLevel = level;
            Save();
        }
    }

    public static void SubmitScore(int score)
    {
        if (score > Data.highScore) Data.highScore = score;
        Data.totalCoins += score;
        Save();
    }

    public static void SubmitDepth(int depth)
    {
        if (depth > Data.bestDepth)
        {
            Data.bestDepth = depth;
            Save();
        }
    }

    public static void ResetAll()
    {
        data = new SaveData();
        Save();
    }
}
