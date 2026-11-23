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
        // Văng lệch ra 2 bên (trái hoặc phải) để không đè lên thân nhân vật hay quái vật
        float side = Random.value < 0.5f ? -1f : 1f;
        go.transform.position = position + new Vector2(side * 0.75f, 0.9f);
        var popup = go.AddComponent<DamagePopup>();
        popup.Setup(damage, color, isCritical, side);
    }

    public static void SpawnText(Vector2 position, string text, Color color, float fontSize = 6.5f)
    {
        var go = new GameObject("TextPopup");
        go.transform.position = position + Vector2.up * 1.5f; // Hiện cao phía trên đỉnh đầu
        var popup = go.AddComponent<DamagePopup>();
        popup.SetupText(text, color, fontSize);
    }

    private void Setup(float damage, Color color, bool isCritical, float side)
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        int dmgInt = Mathf.CeilToInt(damage);
        tmp.text = isCritical ? $"{dmgInt}!" : dmgInt.ToString();
        tmp.fontSize = isCritical ? 9.5f : 6.8f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = isCritical ? new Color(1f, 0.85f, 0.1f) : color;
        CombatVFX.ApplyVfxLayer(tmp.renderer, 100);

        // Văng mạnh ra 2 bên theo đường cong parabol mượt mà
        float vx = side * Random.Range(2.4f, 3.8f);
        float vy = isCritical ? 4.8f : 3.8f;
        velocity = new Vector3(vx, vy, 0f);
    }

    private void SetupText(string text, Color color, float fontSize)
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        CombatVFX.ApplyVfxLayer(tmp.renderer, 115);

        velocity = new Vector3(Random.Range(-0.4f, 0.4f), 3.6f, 0f);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        timer += dt;

        velocity.y -= 7.5f * dt; // trọng lực tạo đường cong văng ra 2 bên đẹp mắt
        transform.position += velocity * dt;

        float t = timer / Lifetime;
        transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.8f, t);

        var c = tmp.color;
        c.a = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
        tmp.color = c;

        if (timer >= Lifetime) Destroy(gameObject);
    }
}
