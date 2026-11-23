using System.Collections;
using UnityEngine;

/// <summary>Đóng băng game một khoảnh khắc khi đánh trúng để đòn đánh "nặng" hơn.</summary>
public class HitStop : MonoBehaviour
{
    private const float SlowScale = 0.05f;
    private static HitStop instance;
    private static bool running;

    public static bool IsRunning => running;

    public static void Do(float duration)
    {
        if (running) return;
        if (Time.timeScale <= 0.1f) return; // đang pause / game over / modal thì bỏ qua

        if (instance == null)
        {
            var go = new GameObject("HitStop");
            instance = go.AddComponent<HitStop>();
        }
        instance.StartCoroutine(instance.Routine(duration));
    }

    /// <summary>Hủy HitStop ngay lập tức khi mở bảng UI Pause / Scroll / Mutation để không bị kẹt Time.timeScale = 0.05!</summary>
    public static void Cancel()
    {
        if (instance != null)
        {
            instance.StopAllCoroutines();
        }
        running = false;
        if (Mathf.Approximately(Time.timeScale, SlowScale))
        {
            Time.timeScale = 1f;
        }
    }

    private IEnumerator Routine(float duration)
    {
        running = true;
        Time.timeScale = SlowScale;

        yield return new WaitForSecondsRealtime(duration);

        // Chỉ khôi phục về 1f nếu không có menu nào khác đang pause (timeScale == 0)
        if (Mathf.Approximately(Time.timeScale, SlowScale))
            Time.timeScale = 1f;

        running = false;
    }

    private void OnDestroy()
    {
        if (running && Mathf.Approximately(Time.timeScale, SlowScale))
            Time.timeScale = 1f;
        running = false;
        instance = null;
    }
}
