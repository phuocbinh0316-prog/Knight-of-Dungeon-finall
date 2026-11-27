using System.Collections;
using UnityEngine;

/// <summary>
/// Quái Phù Thủy Hắc Ám (Dark Arcane Witch) thay thế Yurei ở Map 2:
/// - Ngoại hình Hi-bit pixel-art chế tác qua agent-sprite-forge (Resources/Map2Witch/).
/// - 3 chiêu thức phép thuật đặc trưng:
///   1. ARCANE BOLT (Đạn Phép Hắc Ám): Bắn cầu ma thuật tím truy đuổi nhẹ từ gậy phép (2 damage).
///   2. HEX CURSE CIRCLE (Vòng Tròn Nguyền Rủa): Triệu hồi vòng phép tím dưới chân người chơi, cảnh báo 0.85s rồi bộc phát cột ma lực (2 damage).
///   3. SHADOW BLINK (Tốc Biến Né Đòn): Khi người chơi áp sát (< 2.4m), hóa khói tím lùi ra sau trên cùng nền đất để giữ khoảng cách!
/// </summary>
[DisallowMultipleComponent]
public class DarkWitchEnemy : MonoBehaviour
{
    private const float BoltCooldown = 3.2f;
    private const float HexCooldown = 6.2f;
    private const float BlinkCooldown = 5.8f;
    private const float SpellDamage = 2f;

    private static Sprite sIdle, sWalk, sAttack, sHurt, sOrb;
    private static bool spritesLoaded;

    private PatrolEnemy1 host;
    private EnemyTelegraph telegraph;
    private Animator hostAnimator;
    private Transform player;

    private GameObject visualObj;
    private SpriteRenderer visualSr;

    private Vector3 lastPosition;
    private float walkTimer;
    private float nextBoltTime;
    private float nextHexTime;
    private float nextBlinkTime;
    private bool isCasting;
    private float castAnimUntil;

    private static void LoadSprites()
    {
        if (spritesLoaded) return;
        spritesLoaded = true;
        sIdle = Resources.Load<Sprite>("Map2Witch/witch_idle");
        sWalk = Resources.Load<Sprite>("Map2Witch/witch_walk");
        sAttack = Resources.Load<Sprite>("Map2Witch/witch_attack");
        sHurt = Resources.Load<Sprite>("Map2Witch/witch_hurt");
        sOrb = Resources.Load<Sprite>("Map2Witch/witch_orb");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        spritesLoaded = false;
        sIdle = sWalk = sAttack = sHurt = sOrb = null;
    }

    private void Start()
    {
        host = GetComponent<PatrolEnemy1>();
        hostAnimator = GetComponent<Animator>();
        telegraph = GetComponent<EnemyTelegraph>() ?? gameObject.AddComponent<EnemyTelegraph>();

        var p = FindAnyObjectByType<Player>();
        if (p != null) player = p.transform;

        lastPosition = transform.position;
        nextBoltTime = Time.time + UnityEngine.Random.Range(1.2f, 2.4f);
        nextHexTime = Time.time + UnityEngine.Random.Range(3.5f, 5.0f);
        nextBlinkTime = Time.time + 2.0f;

        LoadSprites();
        if (sIdle != null) InstallWitchVisual();

        var hf = GetComponent<HitFlash>();
        if (hf != null) hf.RefreshRenderers();
    }

    private void InstallWitchVisual()
    {
        int enemyLayerId = SortingLayer.NameToID("Enemy");
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr == null) continue;
            if (sr.gameObject == gameObject || (sr.transform.parent == transform && sr.gameObject.name != "DarkWitchVisual" && !sr.gameObject.name.Contains("HpBar")))
            {
                if (sr.sortingLayerID != 0) enemyLayerId = sr.sortingLayerID;
                sr.enabled = false;
            }
        }

        var existing = transform.Find("DarkWitchVisual");
        if (existing != null)
        {
            visualObj = existing.gameObject;
            visualSr = visualObj.GetComponent<SpriteRenderer>();
        }
        else
        {
            visualObj = new GameObject("DarkWitchVisual");
            visualObj.transform.SetParent(transform, false);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localRotation = Quaternion.identity;
            visualObj.transform.localScale = Vector3.one * 1.05f;
            visualSr = visualObj.AddComponent<SpriteRenderer>();
        }

        visualSr.sprite = sIdle;
        visualSr.color = Color.white;
        visualSr.enabled = true;
        if (enemyLayerId != 0) visualSr.sortingLayerID = enemyLayerId;
        visualSr.sortingOrder = 5;
    }

    private void Update()
    {
        if (host != null && host.IsDead)
        {
            StopAllCoroutines();
            if (host != null) host.ExternalControl = false;
            return;
        }

        // Hào quang ma thuật tím lơ lửng quanh Phù Thủy
        if (UnityEngine.Random.value < 0.08f)
        {
            Vector3 off = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(0.3f, 2.0f), 0f);
            CombatVFX.SpawnBurst(transform.position + off, new Color(0.75f, 0.28f, 1f, 0.75f), 1, 0.3f, 1.1f, 0.14f, 0.32f, -1.2f);
        }

        UpdateWitchAnimation();

        if (player == null || isCasting || (telegraph != null && telegraph.IsStunned)) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float dy = Mathf.Abs(transform.position.y - player.position.y);
        if (dist > 12.5f) return;

        // Chiêu 3: Shadow Blink khi người chơi áp sát quá gần (< 2.4m)
        if (dist < 2.4f && dy <= 2.0f && Time.time >= nextBlinkTime)
        {
            nextBlinkTime = Time.time + BlinkCooldown;
            StartCoroutine(ShadowBlinkRoutine());
            return;
        }

        // Chiêu 2: Hex Curse Circle (Vòng tròn nguyền rủa dưới chân người chơi)
        if (dist <= 11.5f && Time.time >= nextHexTime)
        {
            nextHexTime = Time.time + HexCooldown;
            StartCoroutine(CastHexCircleRoutine());
            return;
        }

        // Chiêu 1: Arcane Bolt (Bắn cầu ma thuật từ gậy phép)
        if (dist >= 3.2f && dist <= 12.0f && Time.time >= nextBoltTime)
        {
            nextBoltTime = Time.time + BoltCooldown;
            StartCoroutine(CastArcaneBoltRoutine());
        }
    }

    private void UpdateWitchAnimation()
    {
        if (visualSr == null || visualObj == null) return;

        bool stunned = telegraph != null && telegraph.IsStunned;
        bool attacking = Time.time < castAnimUntil || (hostAnimator != null && hostAnimator.GetBool("Attack1"));
        float speedX = Mathf.Abs(transform.position.x - lastPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        float hoverY = Mathf.Sin(Time.time * 3.8f) * 0.09f;

        if (stunned)
        {
            visualSr.sprite = sHurt ?? sIdle;
            visualObj.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 36f) * 0.04f, hoverY, 0f);
            return;
        }

        if (attacking)
        {
            visualSr.sprite = sAttack ?? sIdle;
            visualObj.transform.localPosition = new Vector3(0.08f, hoverY, 0f);
            return;
        }

        if (speedX > 0.25f)
        {
            walkTimer += Time.deltaTime * 6f;
            visualSr.sprite = (Mathf.FloorToInt(walkTimer) % 2 == 1 && sWalk != null) ? sWalk : sIdle;
            visualObj.transform.localPosition = new Vector3(0f, hoverY + 0.06f, 0f);
        }
        else
        {
            visualSr.sprite = sIdle;
            visualObj.transform.localPosition = new Vector3(0f, hoverY, 0f);
        }
    }

    // ==================== CHIÊU 1: ARCANE BOLT ====================
    private IEnumerator CastArcaneBoltRoutine()
    {
        isCasting = true;
        if (host != null)
        {
            host.ExternalControl = true;
            if (player != null) host.FaceDirection(player.position.x - transform.position.x);
        }

        if (telegraph != null) telegraph.ShowWarning(0.42f, null);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 1.3f, new Color(0.85f, 0.35f, 1f, 0.9f), 6, 0.8f, 2.5f, 0.14f, 0.28f, 0f);
        yield return new WaitForSeconds(0.42f);

        if (host != null && host.IsDead || (telegraph != null && telegraph.IsStunned))
        {
            EndCast();
            yield break;
        }

        castAnimUntil = Time.time + 0.38f;
        if (player != null)
        {
            float faceDir = (host == null || host.IsFacingRight) ? 1f : -1f;
            Vector3 spawnPos = transform.position + new Vector3(faceDir * 0.9f, 1.25f, 0f);
            WitchArcaneBolt.Spawn(spawnPos, player, sOrb, SpellDamage, gameObject);
            CombatVFX.SpawnBurst(spawnPos, new Color(0.92f, 0.4f, 1f, 1f), 8, 1.5f, 4.5f, 0.14f, 0.25f, 0f);
        }

        yield return new WaitForSeconds(0.28f);
        EndCast();
    }

    // ==================== CHIÊU 2: HEX CURSE CIRCLE ====================
    private IEnumerator CastHexCircleRoutine()
    {
        isCasting = true;
        if (host != null)
        {
            host.ExternalControl = true;
            if (player != null) host.FaceDirection(player.position.x - transform.position.x);
        }

        castAnimUntil = Time.time + 0.55f;
        DamagePopup.SpawnText(transform.position + Vector3.up * 2.4f, "HEX CURSE!", new Color(0.82f, 0.35f, 1f), 5.4f);
        yield return new WaitForSeconds(0.25f);

        if ((host == null || !host.IsDead) && player != null && (telegraph == null || !telegraph.IsStunned))
        {
            WitchHexCircle.Spawn(player.position, 1.55f, SpellDamage, 0.85f, gameObject);
        }

        yield return new WaitForSeconds(0.35f);
        EndCast();
    }

    // ==================== CHIÊU 3: SHADOW BLINK ====================
    private IEnumerator ShadowBlinkRoutine()
    {
        isCasting = true;
        if (host != null) host.ExternalControl = true;

        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 1.0f, new Color(0.75f, 0.25f, 1f, 0.9f), 0.25f, 2.0f, 0.22f);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 1.0f, new Color(0.65f, 0.2f, 0.95f, 0.95f), 12, 2f, 5.5f, 0.16f, 0.3f, 0f);
        yield return new WaitForSeconds(0.18f);

        if ((host == null || !host.IsDead) && player != null)
        {
            float awayDir = transform.position.x >= player.position.x ? 1f : -1f;
            LayerMask gMask = host != null ? host.GroundMask : (LayerMask)128;
            Vector3 candidate = transform.position + new Vector3(awayDir * 3.6f, 0.15f, 0f);
            if (Physics2D.Raycast(candidate + Vector3.up * 0.5f, Vector2.down, 2.2f, gMask))
            {
                transform.position = candidate;
                CombatVFX.SpawnShockwave(candidate + Vector3.up * 1.0f, new Color(0.85f, 0.35f, 1f, 0.95f), 0.25f, 2.2f, 0.24f);
                if (host != null) host.FaceDirection(player.position.x - transform.position.x);
            }
        }

        EndCast();
    }

    private void EndCast()
    {
        isCasting = false;
        if (host != null) host.ExternalControl = false;
    }
}

/// <summary>Đạn Cầu Ma Thuật Hắc Ám bay từ gậy phép của Phù Thủy.</summary>
public class WitchArcaneBolt : MonoBehaviour
{
    private Transform target;
    private Vector2 dir;
    private float damage;
    private GameObject owner;
    private float life = 2.6f;

    public static void Spawn(Vector3 pos, Transform targetPlayer, Sprite orbSprite, float dmg, GameObject caster)
    {
        var go = new GameObject("WitchArcaneBolt");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = orbSprite ?? CombatVFX.GetCircleSprite();
        sr.color = Color.white;
        CombatVFX.ApplyVfxLayer(sr, 70);
        go.transform.localScale = Vector3.one * 0.9f;

        var b = go.AddComponent<WitchArcaneBolt>();
        b.target = targetPlayer;
        b.damage = dmg;
        b.owner = caster;
        b.dir = targetPlayer != null
            ? ((Vector2)(targetPlayer.position + Vector3.up * 0.7f - pos)).normalized
            : Vector2.right;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        if (target != null && life > 1.8f)
        {
            Vector2 desired = ((Vector2)(target.position + Vector3.up * 0.7f - transform.position)).normalized;
            dir = Vector2.Lerp(dir, desired, Time.deltaTime * 2.2f).normalized;
        }

        transform.position += (Vector3)(dir * 8.8f * Time.deltaTime);
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);

        if (UnityEngine.Random.value < 0.35f)
            CombatVFX.SpawnBurst(transform.position, new Color(0.82f, 0.3f, 1f, 0.8f), 1, 0.4f, 1.2f, 0.1f, 0.18f, 0f);

        if (target != null && Vector2.Distance(transform.position, target.position + Vector3.up * 0.7f) <= 0.85f)
        {
            var pc = target.GetComponent<Player>();
            if (pc != null) pc.TakeDamageP(damage, transform.position, owner != null ? owner : gameObject);
            CombatVFX.SpawnShockwave(transform.position, new Color(0.85f, 0.3f, 1f, 0.95f), 0.2f, 1.6f, 0.2f);
            CombatVFX.SpawnBurst(transform.position, new Color(0.9f, 0.4f, 1f, 1f), 10, 2f, 5f, 0.14f, 0.28f, 2f);
            Destroy(gameObject);
        }
    }
}

/// <summary>Vòng Tròn Nguyền Rủa cảnh báo 0.85s dưới chân người chơi rồi bộc phát ma lực tím.</summary>
public class WitchHexCircle : MonoBehaviour
{
    private float radius, damage, delay, timer;
    private GameObject owner;
    private SpriteRenderer ring;

    public static void Spawn(Vector3 pos, float radius, float dmg, float delay, GameObject caster)
    {
        var go = new GameObject("WitchHexCircle");
        go.transform.position = pos + Vector3.up * 0.15f;
        var hex = go.AddComponent<WitchHexCircle>();
        hex.radius = radius;
        hex.damage = dmg;
        hex.delay = delay;
        hex.owner = caster;
    }

    private void Start()
    {
        ring = gameObject.AddComponent<SpriteRenderer>();
        ring.sprite = CombatVFX.GetRingSprite();
        ring.color = new Color(0.8f, 0.25f, 1f, 0.72f);
        CombatVFX.ApplyVfxLayer(ring, 45);
        transform.localScale = new Vector3(radius * 2f, radius * 0.65f, 1f);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float k = Mathf.Clamp01(timer / delay);
        if (ring != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(timer * (14f + 26f * k));
            ring.color = new Color(0.85f, 0.2f + 0.25f * pulse, 1f, 0.4f + 0.55f * k);
        }
        if (UnityEngine.Random.value < 0.35f)
            CombatVFX.SpawnBurst(transform.position + new Vector3(UnityEngine.Random.Range(-radius * 0.6f, radius * 0.6f), 0f, 0f),
                new Color(0.85f, 0.3f, 1f, 0.85f), 1, 0.5f, 2.2f, 0.14f, 0.28f, -2.5f);

        if (timer < delay) return;

        // Bộc phát cột ma lực tím
        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 0.4f, new Color(0.88f, 0.32f, 1f, 0.98f), 0.3f, radius * 2.2f, 0.26f);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 0.4f, new Color(0.92f, 0.38f, 1f, 1f), 16, 2.5f, 7.5f, 0.2f, 0.42f, -3.5f);

        var p = FindAnyObjectByType<Player>();
        if (p != null && Vector2.Distance(p.transform.position, transform.position) <= radius + 0.25f)
        {
            p.TakeDamageP(damage, transform.position, owner != null ? owner : gameObject);
        }
        Destroy(gameObject);
    }
}
