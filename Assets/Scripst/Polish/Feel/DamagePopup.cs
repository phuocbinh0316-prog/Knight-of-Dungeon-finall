using TMPro;
using UnityEngine;

/// <summary>Số sát thương bay lên. Tự tạo bằng code, không cần prefab.</summary>
public class DamagePopup : MonoBehaviour
{
    private TextMeshPro tmp;
    private Vector3 velocity;
    private float timer;
    private const float Lifetime = 0.7f;

    public static void Spawn(Vector2 position, float damage, Color color)
    {
        var go = new GameObject("DamagePopup");
        go.transform.position = position + Vector2.up * 0.6f;
        var popup = go.AddComponent<DamagePopup>();
        popup.Setup(damage, color);
    }

    private void Setup(float damage, Color color)
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = Mathf.CeilToInt(damage).ToString();
        tmp.fontSize = 6f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 100;

        velocity = new Vector3(Random.Range(-0.8f, 0.8f), 3f, 0f);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        timer += dt;

        velocity.y -= 6f * dt; // trọng lực nhẹ
        transform.position += velocity * dt;

        float t = timer / Lifetime;
        transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.8f, t);

        var c = tmp.color;
        c.a = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
        tmp.color = c;

        if (timer >= Lifetime) Destroy(gameObject);
    }
}
