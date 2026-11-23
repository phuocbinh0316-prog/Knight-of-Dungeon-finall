using System.Collections;
using UnityEngine;

/// <summary>
/// Quái Mini Địa Ngục — HELLSPAWN CULTIST (cùng concept với Infernal Abyssal Knight).
/// Gắn tự động lên quái thường (PatrolEnemy1 không phải Elite) ở Map 1.
///
/// Ngoại hình:
/// - Nếu có sprite trong Resources/HellMini/ (mini_idle, mini_walk, mini_attack, mini_hurt) -> thay toàn bộ ngoại hình.
/// - Nếu chưa có -> giữ sprite gốc nhưng nhuộm tông đen-đỏ địa ngục + tàn lửa bay quanh người.
///
/// Cơ chế mới:
/// 1. HELL LUNGE: người chơi ở tầm trung (3.2 - 6.5) -> dấu "!" cảnh báo 0.45s rồi lao giáo tới (2 damage). Hồi chiêu 4.5s.
/// 2. BLOOD FRENZY: dưới 35% máu -> nổi điên, tốc độ x1.45, thân đỏ rực.
/// 3. EMBER DEATH: khi chết để lại vòng cảnh báo rồi nổ lửa sau 0.6s (1 damage, bán kính 1.6) — đừng đứng sát xác!
/// </summary>
[DisallowMultipleComponent]
public class MiniHellspawn : MonoBehaviour
{
    private const float VisualScale = 1.0f;
    private const float LungeMinDist = 3.2f;
    private const float LungeMaxDist = 6.5f;
    private const float LungeCooldown = 4.5f;
    private const float LungeDamage = 2f;
    private const float FrenzyThreshold = 0.35f;
    private const float FrenzySpeed = 1.45f;
    private const float DeathBlastRadius = 1.6f;
    private const float DeathBlastDamage = 1f;

    private static Sprite sIdle, sWalk, sAttack, sHurt;
    private static bool spritesLoaded;

    private PatrolEnemy1 host;
    private EnemyTelegraph telegraph;
    private Animator hostAnimator;
    private Rigidbody2D rb;
    private Transform player;

    private GameObject visualObj;
    private SpriteRenderer visualSr;
    private SpriteRenderer[] tintedRenderers;
    private Color[] originalColors;
    private bool usingCustomSprites;

    private Vector3 lastPosition;
    private float walkTimer;
    private float nextLungeTime;
    private bool lunging;
    private bool frenzied;
    private bool deathHandled;

    private static void LoadSprites()
    {
        if (spritesLoaded) return;
        spritesLoaded = true;
        sIdle = Resources.Load<Sprite>("HellMini/mini_idle");
        sWalk = Resources.Load<Sprite>("HellMini/mini_walk");
        sAttack = Resources.Load<Sprite>("HellMini/mini_attack");
        sHurt = Resources.Load<Sprite>("HellMini/mini_hurt");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        spritesLoaded = false;
        sIdle = sWalk = sAttack = sHurt = null;
    }

    private void Start()
    {
        host = GetComponent<PatrolEnemy1>();
        if (host == null || GetComponent<EliteEnemy>() != null)
        {
            Destroy(this);
            return;
        }

        telegraph = GetComponent<EnemyTelegraph>() ?? gameObject.AddComponent<EnemyTelegraph>();
        hostAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        var p = FindAnyObjectByType<Player>();
        if (p != null) player = p.transform;

        lastPosition = transform.position;
        nextLungeTime = Time.time + UnityEngine.Random.Range(1.5f, 3.5f);

        LoadSprites();
        if (sIdle != null) InstallCustomVisual();
        else ApplyHellTint(new Color(0.78f, 0.42f, 0.42f));

        var hf = GetComponent<HitFlash>();
        if (hf != null) hf.RefreshRenderers();
    }

    // ───────────────────────── Ngoại hình ─────────────────────────

    private void InstallCustomVisual()
    {
        usingCustomSprites = true;
        int layerId = SortingLayer.NameToID("Enemy");
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr == null) continue;
            if (sr.gameObject == gameObject || (sr.transform.parent == transform && sr.gameObject.name != "HellMiniVisual"))
            {
                if (sr.sortingLayerID != 0) layerId = sr.sortingLayerID;
                sr.enabled = false;
            }
        }

        var existingChild = transform.Find("HellMiniVisual");
        if (existingChild != null)
        {
            visualObj = existingChild.gameObject;
            visualSr = visualObj.GetComponent<SpriteRenderer>();
            if (visualSr != null)
            {
                visualSr.enabled = true;
                if (sIdle != null && visualSr.sprite == null) visualSr.sprite = sIdle;
            }
            tintedRenderers = new[] { visualSr };
            originalColors = new[] { Color.white };
            return;
        }

        visualObj = new GameObject("HellMiniVisual");
        visualObj.transform.SetParent(transform, false);
        visualObj.transform.localScale = Vector3.one * VisualScale;
        visualSr = visualObj.AddComponent<SpriteRenderer>();
        visualSr.sprite = sIdle;
        if (layerId != 0) visualSr.sortingLayerID = layerId;
        visualSr.sortingOrder = 5;
        tintedRenderers = new[] { visualSr };
        originalColors = new[] { Color.white };
    }

    private void ApplyHellTint(Color tint)
    {
        if (tintedRenderers == null)
        {
            var list = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr == null) continue;
                if (sr.gameObject == gameObject || sr.transform.parent == transform) list.Add(sr);
            }
            tintedRenderers = list.ToArray();
            originalColors = new Color[tintedRenderers.Length];
            for (int i = 0; i < tintedRenderers.Length; i++) originalColors[i] = tintedRenderers[i].color;
        }
        for (int i = 0; i < tintedRenderers.Length; i++)
        {
            if (tintedRenderers[i] == null) continue;
            Color c = originalColors[i] * tint;
            c.a = originalColors[i].a;
            tintedRenderers[i].color = c;
        }
    }

    // ───────────────────────── Vòng lặp ─────────────────────────

    private void Update()
    {
        if (host == null) return;

        if (host.IsDead)
        {
            if (!deathHandled) OnDeath();
            return;
        }

        // Tàn lửa nhỏ bay quanh thân (dày hơn khi nổi điên)
        if (UnityEngine.Random.value < (frenzied ? 0.14f : 0.05f))
        {
            Color ember = UnityEngine.Random.value < 0.5f ? new Color(1f, 0.42f, 0.1f, 0.8f) : new Color(0.9f, 0.15f, 0.15f, 0.75f);
            Vector3 off = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(0.2f, 1.6f), 0f);
            CombatVFX.SpawnBurst(transform.position + off, ember, 1, 0.4f, 1.4f, 0.11f, 0.32f, -1.8f);
        }

        UpdateFrenzy();
        UpdateAnimation();
        TryStartLunge();
    }

    private void UpdateFrenzy()
    {
        if (frenzied || host.HpRatio > FrenzyThreshold || host.HpRatio <= 0f) return;
        frenzied = true;
        host.SpeedBonus = FrenzySpeed;
        DamagePopup.SpawnText(transform.position + Vector3.up * 1.6f, "FRENZY!", new Color(1f, 0.2f, 0.2f), 5.2f);
        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 0.9f, new Color(1f, 0.2f, 0.15f, 0.85f), 0.2f, 1.8f, 0.25f);
        ApplyHellTint(usingCustomSprites ? new Color(1f, 0.6f, 0.55f) : new Color(1f, 0.32f, 0.3f));
    }

    private void UpdateAnimation()
    {
        if (!usingCustomSprites || visualSr == null) return;

        float speedX = Mathf.Abs(transform.position.x - lastPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        if (telegraph != null && telegraph.IsStunned)
        {
            visualSr.sprite = sHurt ?? sIdle;
            visualObj.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 40f) * 0.04f, 0f, 0f);
            return;
        }

        if (lunging || (hostAnimator != null && hostAnimator.GetBool("Attack1")))
        {
            visualSr.sprite = sAttack ?? sIdle;
            visualObj.transform.localPosition = new Vector3(0.08f, 0f, 0f);
            return;
        }

        if (speedX > 0.25f)
        {
            walkTimer += Time.deltaTime * (frenzied ? 9f : 6.5f);
            bool step = Mathf.FloorToInt(walkTimer) % 2 == 1;
            visualSr.sprite = (step && sWalk != null) ? sWalk : sIdle;
            visualObj.transform.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(walkTimer * Mathf.PI)) * 0.06f, 0f);
            visualObj.transform.localScale = Vector3.one * VisualScale;
        }
        else
        {
            visualSr.sprite = sIdle;
            visualObj.transform.localPosition = Vector3.zero;
            float breathe = 1f + 0.025f * Mathf.Sin(Time.time * 3.8f);
            visualObj.transform.localScale = new Vector3(VisualScale, VisualScale * breathe, 1f);
        }
    }

    // ───────────────────────── Cơ chế 1: HELL LUNGE ─────────────────────────

    private void TryStartLunge()
    {
        if (lunging || player == null || Time.time < nextLungeTime) return;
        if (telegraph != null && (telegraph.IsStunned || telegraph.IsPreparingAttack)) return;

        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);
        if (dist < LungeMinDist || dist > LungeMaxDist) return;
        if (Mathf.Abs(player.position.y - transform.position.y) > 1.5f) return;

        nextLungeTime = Time.time + LungeCooldown * (frenzied ? 0.7f : 1f);
        StartCoroutine(LungeRoutine(Mathf.Sign(dx)));
    }

    private IEnumerator LungeRoutine(float dir)
    {
        lunging = true;
        host.ExternalControl = true;
        host.FaceDirection(dir);
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Cảnh báo: dấu ! + tụ lửa ở mũi giáo
        bool warned = false;
        if (telegraph != null) telegraph.ShowWarning(0.45f, () => warned = true);
        float t = 0f;
        while (t < 0.5f && !warned)
        {
            if (host.IsDead || (telegraph != null && telegraph.IsStunned)) { EndLunge(); yield break; }
            t += Time.deltaTime;
            if (UnityEngine.Random.value < 0.5f)
                CombatVFX.SpawnBurst(transform.position + new Vector3(dir * 0.9f, 0.9f, 0f), new Color(1f, 0.5f, 0.1f, 0.9f), 1, 0.5f, 1.5f, 0.12f, 0.2f, 0f);
            yield return null;
        }

        // Lao tới
        bool hit = false;
        float dashTime = 0.28f;
        float dashSpeed = frenzied ? 15f : 12f;
        float elapsed = 0f;
        while (elapsed < dashTime)
        {
            if (host.IsDead || (telegraph != null && telegraph.IsStunned)) break;

            Vector3 next = transform.position + new Vector3(dir * dashSpeed * Time.deltaTime, 0f, 0f);
            // Không lao khỏi mép vực / xuyên tường
            Vector2 ahead = (Vector2)next + new Vector2(dir * 0.4f, 0.5f);
            bool ground = Physics2D.Raycast(ahead, Vector2.down, 1.6f, host.GroundMask);
            bool wall = Physics2D.Raycast((Vector2)transform.position + Vector2.up * 0.6f, new Vector2(dir, 0f), 0.6f, host.GroundMask);
            if (!ground || wall) break;

            transform.position = next;
            if (visualSr != null && UnityEngine.Random.value < 0.5f)
                CombatVFX.SpawnAfterimage(visualSr, new Color(1f, 0.3f, 0.1f, 0.55f), 0.16f);

            if (!hit && player != null && Vector2.Distance(transform.position + new Vector3(dir * 0.7f, 0.8f, 0f), player.position + Vector3.up * 0.8f) < 1.0f)
            {
                hit = true;
                var pc = player.GetComponent<Player>();
                if (pc != null) pc.TakeDamageP(LungeDamage, transform.position, gameObject);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        CombatVFX.SpawnFootDust(transform.position, 5);
        yield return new WaitForSeconds(0.25f); // hở sơ hở sau chiêu -> cơ hội phản công
        EndLunge();
    }

    private void EndLunge()
    {
        lunging = false;
        if (host != null) host.ExternalControl = false;
    }

    // ───────────────────────── Cơ chế 3: EMBER DEATH ─────────────────────────

    private void OnDeath()
    {
        deathHandled = true;
        StopAllCoroutines();
        EndLunge();
        if (visualSr != null) visualSr.sprite = sHurt ?? sIdle;
        DeathBlast.Spawn(transform.position + Vector3.up * 0.4f, DeathBlastRadius, DeathBlastDamage, 0.6f);
        if (visualObj != null) StartCoroutine(FadeVisual());
    }

    private IEnumerator FadeVisual()
    {
        float t = 0f;
        while (t < 0.5f && visualSr != null)
        {
            t += Time.deltaTime;
            visualSr.color = new Color(1f, 0.5f, 0.4f, 1f - t / 0.5f);
            yield return null;
        }
    }
}

/// <summary>Vòng cảnh báo + vụ nổ lửa khi quái mini chết. Tồn tại độc lập vì quái gốc sẽ bị Destroy.</summary>
public class DeathBlast : MonoBehaviour
{
    private float radius, damage, delay, timer;
    private SpriteRenderer ring;

    public static void Spawn(Vector3 pos, float radius, float damage, float delay)
    {
        var go = new GameObject("HellspawnDeathBlast");
        go.transform.position = pos;
        var b = go.AddComponent<DeathBlast>();
        b.radius = radius; b.damage = damage; b.delay = delay;
    }

    private void Start()
    {
        ring = gameObject.AddComponent<SpriteRenderer>();
        ring.sprite = CombatVFX.GetRingSprite();
        ring.color = new Color(1f, 0.3f, 0.1f, 0.7f);
        CombatVFX.ApplyVfxLayer(ring, 40);
        transform.localScale = Vector3.one * radius * 2f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float k = Mathf.Clamp01(timer / delay);
        if (ring != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(timer * (12f + 30f * k));
            ring.color = new Color(1f, 0.25f + 0.2f * pulse, 0.08f, 0.35f + 0.5f * k);
        }
        if (Random.value < 0.3f)
            CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.45f, 0.1f, 0.8f), 1, 0.5f, 2f, 0.12f, 0.25f, -1f);

        if (timer < delay) return;

        CombatVFX.SpawnShockwave(transform.position, new Color(1f, 0.4f, 0.1f, 0.95f), 0.3f, radius * 2.2f, 0.25f);
        CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.5f, 0.12f, 0.95f), 14, 2.5f, 6.5f, 0.2f, 0.4f, 4f);

        var p = FindAnyObjectByType<Player>();
        if (p != null && Vector2.Distance(p.transform.position + Vector3.up * 0.6f, transform.position) <= radius)
            p.TakeDamageP(damage, transform.position, gameObject);

        Destroy(gameObject);
    }
}
