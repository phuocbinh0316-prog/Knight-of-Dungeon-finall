using UnityEngine;

/// <summary>
/// Thư viện hiệu ứng hình ảnh (VFX) tạo tự động bằng code (không cần kéo thả prefab):
/// Vệt chém lưỡi liềm (Slash Arc), Bom lửa, Bụi chân, Tia lửa Crit/Parry, Sóng xung kích, Tia sét, Bóng mờ Afterimage.
/// Luôn đặt đúng Sorting Layer cao hơn Player/Background để hiển thị rõ nét trên mọi Scene!
/// </summary>
public static class CombatVFX
{
    public const string VfxSortingLayer = "HpBar";     // Trên Player & Grass, dưới UI
    public const string WorldSortingLayer = "Player";  // Cùng lớp Player (cho NPC, Rương, Đạn)

    private static Sprite slashSprite;
    private static Sprite circleSprite;
    private static Sprite ringSprite;
    private static Sprite pixelSprite;
    private static Sprite bombSprite;
    private static Material cachedLineMaterial;

    public static void ApplyVfxLayer(Renderer r, int order = 50, bool useWorldLayer = false)
    {
        if (r == null) return;
        string target = useWorldLayer ? WorldSortingLayer : VfxSortingLayer;
        int id = SortingLayer.NameToID(target);
        if (id != 0) r.sortingLayerID = id;
        r.sortingOrder = order;
    }

    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int sz = 32;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        float r = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                float a = d <= 0.7f ? 1f : (d <= 1f ? 1f - (d - 0.7f) / 0.3f : 0f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 32f);
        return circleSprite;
    }

    public static Sprite GetBombSprite()
    {
        if (bombSprite != null) return bombSprite;
        const int sz = 24;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color clear = new Color(0, 0, 0, 0);
        Color outline = new Color(0.08f, 0.05f, 0.1f, 1f);
        Color ironDark = new Color(0.22f, 0.18f, 0.28f, 1f);
        Color ironHi = new Color(0.45f, 0.38f, 0.55f, 1f);
        Color coreGlow = new Color(1f, 0.42f, 0.1f, 1f);
        Color spark = new Color(1f, 0.92f, 0.35f, 1f);

        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
                tex.SetPixel(x, y, clear);

        // Thân bom tròn ở tâm (11, 10) bán kính 7
        for (int y = 2; y <= 18; y++)
        {
            for (int x = 3; x <= 19; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(11f, 10f));
                if (d <= 7.4f)
                {
                    if (d >= 6.2f) tex.SetPixel(x, y, outline);
                    else if (d <= 2.4f) tex.SetPixel(x, y, coreGlow);
                    else if (x < 10 && y > 10) tex.SetPixel(x, y, ironHi);
                    else tex.SetPixel(x, y, ironDark);
                }
            }
        }
        // Cổ ngòi + tia lửa rực sáng ở đỉnh phải
        for (int y = 17; y <= 20; y++)
            for (int x = 10; x <= 12; x++)
                tex.SetPixel(x, y, outline);
        tex.SetPixel(13, 20, coreGlow);
        tex.SetPixel(14, 21, coreGlow);
        tex.SetPixel(15, 22, spark);
        tex.SetPixel(16, 21, spark);
        tex.SetPixel(15, 20, spark);
        tex.SetPixel(14, 22, spark);

        tex.Apply();
        bombSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 20f);
        return bombSprite;
    }

    public static Sprite GetRingSprite()
    {
        if (ringSprite != null) return ringSprite;
        const int sz = 64;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        float r = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                float a = (d >= 0.72f && d <= 0.96f) ? 1f - Mathf.Abs(d - 0.84f) / 0.14f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
        }
        tex.Apply();
        ringSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 32f);
        return ringSprite;
    }

    public static Sprite GetSlashSprite()
    {
        if (slashSprite != null) return slashSprite;
        const int sz = 64;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float nx = (x - sz * 0.32f) / (sz * 0.58f);
                float ny = (y - sz * 0.5f) / (sz * 0.48f);
                float dOuter = Mathf.Sqrt(nx * nx + ny * ny);

                float ix = (x - sz * 0.16f) / (sz * 0.52f);
                float iy = (y - sz * 0.5f) / (sz * 0.42f);
                float dInner = Mathf.Sqrt(ix * ix + iy * iy);

                float a = 0f;
                if (dOuter <= 1f && dInner >= 0.76f && x > sz * 0.25f)
                {
                    float edgeFade = 1f - Mathf.Clamp01((dOuter - 0.82f) / 0.18f);
                    float tailFade = Mathf.Clamp01((x - sz * 0.25f) / (sz * 0.35f));
                    a = Mathf.Clamp01(edgeFade * tailFade * 1.4f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        slashSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 32f);
        return slashSprite;
    }

    public static Sprite GetPixelSprite()
    {
        if (pixelSprite != null) return pixelSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        pixelSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return pixelSprite;
    }

    public static void SpawnSlashArc(Vector3 position, bool facingRight, int comboStep, bool isCrit = false)
    {
        var go = new GameObject("SlashArcVFX");
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSlashSprite();
        ApplyVfxLayer(sr, 55);

        Color col;
        float scale;
        float angleZ;
        if (isCrit || comboStep == 3)
        {
            col = new Color(1f, 0.78f, 0.15f, 0.95f);
            scale = 1.45f;
            angleZ = facingRight ? -10f : 10f;
        }
        else if (comboStep == 2)
        {
            col = new Color(0.55f, 0.9f, 1f, 0.9f);
            scale = 1.15f;
            angleZ = facingRight ? 18f : -18f;
        }
        else
        {
            col = new Color(0.95f, 0.97f, 1f, 0.88f);
            scale = 1.05f;
            angleZ = facingRight ? -14f : 14f;
        }

        sr.color = col;
        sr.flipX = !facingRight;
        sr.flipY = (comboStep == 2);
        go.transform.rotation = Quaternion.Euler(0f, 0f, angleZ);
        go.transform.localScale = Vector3.one * scale;

        var anim = go.AddComponent<VfxAutoFade>();
        anim.Init(0.14f, scale, scale * 1.25f, new Vector3(facingRight ? 2.2f : -2.2f, 0f, 0f));
    }

    public static void SpawnShockwave(Vector3 position, Color color, float startScale, float endScale, float duration = 0.25f)
    {
        var go = new GameObject("ShockwaveVFX");
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        sr.color = color;
        ApplyVfxLayer(sr, 60);
        go.transform.localScale = Vector3.one * startScale;

        var anim = go.AddComponent<VfxAutoFade>();
        anim.Init(duration, startScale, endScale, Vector3.zero);
    }

    public static void SpawnBurst(Vector3 position, Color color, int count = 8, float minSpeed = 2f, float maxSpeed = 6f, float size = 0.22f, float lifetime = 0.35f, float gravity = 8f)
    {
        var sp = GetCircleSprite();
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("VfxParticle");
            go.transform.position = position + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.15f), 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = color;
            ApplyVfxLayer(sr, 52);

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(minSpeed, maxSpeed);
            Vector3 vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 1.2f, 0f);

            var p = go.AddComponent<VfxParticleBehaviour>();
            p.Init(vel, Random.Range(size * 0.7f, size * 1.3f), Random.Range(lifetime * 0.75f, lifetime * 1.2f), gravity);
        }
    }

    public static void SpawnFootDust(Vector3 feetPos, int count = 6, bool isDoubleJump = false)
    {
        Color col = isDoubleJump ? new Color(0.75f, 0.92f, 1f, 0.85f) : new Color(0.85f, 0.82f, 0.78f, 0.75f);
        if (isDoubleJump)
        {
            SpawnShockwave(feetPos, new Color(0.6f, 0.9f, 1f, 0.8f), 0.35f, 1.3f, 0.2f);
        }
        SpawnBurst(feetPos, col, count, 1.5f, 4.0f, 0.2f, 0.28f, 3f);
    }

    public static void SpawnAfterimage(SpriteRenderer source, Color tint, float lifetime = 0.2f)
    {
        if (source == null || source.sprite == null) return;
        var go = new GameObject("DashAfterimage");
        go.transform.position = source.transform.position;
        go.transform.rotation = source.transform.rotation;
        go.transform.localScale = source.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.color = tint;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder - 1;

        var anim = go.AddComponent<VfxAutoFade>();
        anim.Init(lifetime, 1f, 0.95f, Vector3.zero, keepCurrentScale: true);
    }

    public static void SpawnLightningBolt(Vector3 start, Vector3 end, Color color, float duration = 0.22f)
    {
        var go = new GameObject("LightningBoltVFX");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        ApplyVfxLayer(lr, 68);
        lr.startWidth = 0.14f;
        lr.endWidth = 0.08f;

        if (cachedLineMaterial == null)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh != null) cachedLineMaterial = new Material(sh);
        }
        if (cachedLineMaterial != null) lr.sharedMaterial = cachedLineMaterial;

        lr.startColor = color;
        lr.endColor = new Color(color.r, color.g, color.b, 0.7f);

        const int segments = 7;
        lr.positionCount = segments + 1;
        Vector3 dir = end - start;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f).normalized;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 pt = Vector3.Lerp(start, end, t);
            if (i > 0 && i < segments)
            {
                pt += perp * Random.Range(-0.42f, 0.42f);
            }
            pt.z = -0.1f;
            lr.SetPosition(i, pt);
        }

        var fade = go.AddComponent<VfxLineFade>();
        fade.Init(duration, color);
    }
}

public class VfxAutoFade : MonoBehaviour
{
    private SpriteRenderer sr;
    private float duration;
    private float elapsed;
    private float startScale;
    private float endScale;
    private Vector3 velocity;
    private Vector3 initialLocalScale;
    private Color startColor;
    private bool keepScale;

    public void Init(float dur, float s0, float s1, Vector3 vel, bool keepCurrentScale = false)
    {
        sr = GetComponent<SpriteRenderer>();
        duration = Mathf.Max(0.01f, dur);
        startScale = s0;
        endScale = s1;
        velocity = vel;
        keepScale = keepCurrentScale;
        initialLocalScale = transform.localScale;
        if (sr != null) startColor = sr.color;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        float t = Mathf.Clamp01(elapsed / duration);

        transform.position += velocity * dt;
        if (!keepScale)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
        }
        else
        {
            transform.localScale = initialLocalScale * Mathf.Lerp(startScale, endScale, t);
        }

        if (sr != null)
        {
            var c = startColor;
            c.a = startColor.a * (1f - t);
            sr.color = c;
        }

        if (elapsed >= duration) Destroy(gameObject);
    }
}

public class VfxParticleBehaviour : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector3 velocity;
    private float gravity;
    private float duration;
    private float elapsed;
    private float startSize;
    private Color startColor;

    public void Init(Vector3 vel, float size, float dur, float grav)
    {
        sr = GetComponent<SpriteRenderer>();
        velocity = vel;
        startSize = size;
        duration = Mathf.Max(0.05f, dur);
        gravity = grav;
        transform.localScale = Vector3.one * size;
        if (sr != null) startColor = sr.color;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        elapsed += dt;
        float t = Mathf.Clamp01(elapsed / duration);

        velocity.y -= gravity * dt;
        transform.position += velocity * dt;
        transform.localScale = Vector3.one * Mathf.Lerp(startSize, 0.02f, t);

        if (sr != null)
        {
            var c = startColor;
            c.a = startColor.a * (1f - t);
            sr.color = c;
        }

        if (elapsed >= duration) Destroy(gameObject);
    }
}

public class VfxLineFade : MonoBehaviour
{
    private LineRenderer lr;
    private float duration;
    private float elapsed;
    private Color baseColor;

    public void Init(float dur, Color col)
    {
        lr = GetComponent<LineRenderer>();
        duration = Mathf.Max(0.05f, dur);
        baseColor = col;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        if (lr != null)
        {
            var c = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - t));
            lr.startColor = c;
            lr.endColor = c;
            lr.startWidth = Mathf.Lerp(0.16f, 0.02f, t);
        }
        if (elapsed >= duration) Destroy(gameObject);
    }
}
