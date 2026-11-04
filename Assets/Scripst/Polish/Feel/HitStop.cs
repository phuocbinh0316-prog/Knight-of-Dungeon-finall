using System.Collections;
using UnityEngine;

/// <summary>Đóng băng game một khoảnh khắc khi đánh trúng để đòn đánh "nặng" hơn.</summary>
public class HitStop : MonoBehaviour
{
    private const float SlowScale = 0.05f;
    private static HitStop instance;
    private static bool running;

    public static void Do(float duration)
    {
        if (running) return;
        if (Time.timeScale <= 0.01f) return; // đang pause / game over thì bỏ qua

        if (instance == null)
        {
            var go = new GameObject("HitStop");
            instance = go.AddComponent<HitStop>();
        }
        instance.StartCoroutine(instance.Routine(duration));
    }

    private IEnumerator Routine(float duration)
    {
        running = true;
        float previous = Time.timeScale;
        Time.timeScale = SlowScale;

        yield return new WaitForSecondsRealtime(duration);

        // Chỉ khôi phục nếu không ai khác (Pause, GameOver) đã đổi timeScale
        if (Mathf.Approximately(Time.timeScale, SlowScale))
            Time.timeScale = previous;

        running = false;
    }

    private void OnDestroy()
    {
        running = false;
        instance = null;
    }
}
