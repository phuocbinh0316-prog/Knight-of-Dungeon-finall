using TMPro;
using UnityEngine;

/// <summary>Số sát thương bay lên. Tự tạo bằng code, không cần prefab.</summary>
public class DamagePopup : MonoBehaviour
{
    private TextMeshPro tmp;
    private Vector3 velocity;
    private float timer;
    private const float Lifetime = 0.7f;

    public static void Spawn(Vector2 position, float damage, Color color, bool isCritical = false)
    {
        var go = new GameObject("DamagePopup");
        go.transform.position = position + Vector2.up * 0.6f;
        var popup = go.AddComponent<DamagePopup>();
        popup.Setup(damage, color, isCritical);
    }

    private void Setup(float damage, Color color, bool isCritical)
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        int dmgInt = Mathf.CeilToInt(damage);
        tmp.text = isCritical ? $"{dmgInt}!" : dmgInt.ToString();
        tmp.fontSize = isCritical ? 9f : 6f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = isCritical ? new Color(1f, 0.85f, 0.1f) : color;
        tmp.sortingOrder = 100;

        float vx = Random.Range(-0.8f, 0.8f);
        float vy = isCritical ? 4.5f : 3f;
        velocity = new Vector3(vx, vy, 0f);
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
