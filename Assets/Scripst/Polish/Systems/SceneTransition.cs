using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Chuyển scene có hiệu ứng mờ dần. Không cần đặt gì vào scene:
///   SceneTransition.Load("map 2");
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;
    private CanvasGroup group;
    private bool busy;
    private const float FadeTime = 0.35f;

    public static void Load(string sceneName)
    {
        Ensure();
        if (!instance.busy) instance.StartCoroutine(instance.Routine(sceneName));
    }

    private static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneTransition>();
        instance.Build();
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var imgGO = new GameObject("Fade");
        imgGO.transform.SetParent(transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = Color.black;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        group = imgGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    private IEnumerator Routine(string sceneName)
    {
        busy = true;
        group.blocksRaycasts = true;
        yield return Fade(0f, 1f);

        Time.timeScale = 1f;
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        yield return Fade(1f, 0f);
        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < FadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / FadeTime);
            yield return null;
        }
        group.alpha = to;
    }
}
