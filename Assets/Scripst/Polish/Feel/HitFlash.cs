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
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();

        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].color;
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
