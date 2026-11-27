using System.Collections;
using UnityEngine;

/// <summary>Gắn vào Enemy/Player: nháy màu khi bị đánh.</summary>
public class HitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float duration = 0.1f;

    private Color[] originalColors;
    private Coroutine routine;

    private void Awake()
    {
        RefreshRenderers();
    }

    public void RefreshRenderers()
    {
        var all = GetComponentsInChildren<SpriteRenderer>();
        var list = new System.Collections.Generic.List<SpriteRenderer>(all.Length);
        foreach (var sr in all)
        {
            if (sr == null) continue;
            // Không nháy màu lên thanh HP bar, khung Prompt hay dấu cảnh báo
            bool isUiOrBar = false;
            Transform cur = sr.transform;
            while (cur != null && cur != transform)
            {
                string n = cur.name;
                if (n.Contains("HpBar") || n.Contains("Prompt") || n.Contains("Telegraph") || n.Contains("Badge"))
                {
                    isUiOrBar = true;
                    break;
                }
                cur = cur.parent;
            }
            if (!isUiOrBar) list.Add(sr);
        }

        renderers = list.ToArray();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    public void Flash()
    {
        if (!isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = flashColor;

        yield return new WaitForSecondsRealtime(duration);

        Restore();
        routine = null;
    }

    private void Restore()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = originalColors[i];
    }

    private void OnDisable()
    {
        if (originalColors != null) Restore();
    }
}
