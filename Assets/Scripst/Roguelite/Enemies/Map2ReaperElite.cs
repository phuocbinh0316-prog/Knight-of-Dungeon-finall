using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Quái Tinh Anh Map 2 — SPECTRAL SOUL REAPER (Tử Thần Lưỡi Hái Linh Hồn):
/// - Tạo hình hoàn toàn riêng biệt (Hi-bit pixel-art qua agent-sprite-forge, Resources/Map2Elite/).
/// - Máu cực trâu (maxHealth x2.6 ≈ 78 HP), thanh máu đỏ viền đen nổi bật trên đầu & tụt mượt khi trúng đòn.
/// - Bộ 3 kỹ năng độc quyền khác biệt hoàn toàn Map 1:
///   1. SOUL SCYTHE BOOMERANG: Ném lưỡi hái xoay bay xa 8.5m rồi vòng ngược lại (2 damage).
///   2. SPECTRAL SHOCKWAVE SLAM: Đập lưỡi hái giải phóng 2 luồng sóng linh hồn chạy 2 hướng trái/phải (2 damage + Frost).
///   3. SOUL WRATH (< 50% HP): Hóa cuồng nộ, tăng tốc ra chiêu & phóng 2 Hồn Ma truy đuổi!
/// </summary>
[DisallowMultipleComponent]
public class Map2ReaperElite : MonoBehaviour
{
    [SerializeField] private float healthMultiplier = 2.6f;
    [SerializeField] private int goldReward = 30;
    [SerializeField] private int cellReward = 12;

    private static Sprite sIdle, sWalk, sAttack, sHurt, sScythe;
    private static bool spritesLoaded;

    private Transform player;
    private PatrolEnemy1 patrolEnemy;
    private EnemyTelegraph telegraph;
    private SpriteRenderer visualSr;

    private GameObject eliteHpBarRoot;
    private Transform eliteHpFill;
    private SpriteRenderer eliteHpBgSr;
    private SpriteRenderer eliteHpFillSr;
    private GameObject eliteBadgeObj;
    private float displayedHpRatio = 1f;

    private Vector3 lastPosition;
    private float walkTimer;
    private float nextBoomerangTime;
    private float nextSlamTime;
    private float nextWispTime;
    private float attackPoseUntil;
    private bool isCasting;
    private bool enraged;
    private bool rewarded;

    private static void LoadSprites()
    {
        if (spritesLoaded) return;
        spritesLoaded = true;
        sIdle = Resources.Load<Sprite>("Map2Elite/reaper_idle");
        sWalk = Resources.Load<Sprite>("Map2Elite/reaper_walk");
        sAttack = Resources.Load<Sprite>("Map2Elite/reaper_attack");
        sHurt = Resources.Load<Sprite>("Map2Elite/reaper_hurt");
        sScythe = Resources.Load<Sprite>("Map2Elite/reaper_scythe");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        spritesLoaded = false;
        sIdle = sWalk = sAttack = sHurt = sScythe = null;
    }

    private void Start()
    {
        patrolEnemy = GetComponent<PatrolEnemy1>();
        telegraph = GetComponent<EnemyTelegraph>() ?? gameObject.AddComponent<EnemyTelegraph>();
        visualSr = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;

        LoadSprites();
        if (visualSr != null && sIdle != null)
        {
            visualSr.enabled = true;
            visualSr.sprite = sIdle;
            visualSr.color = Color.white;
            int enemyLayer = SortingLayer.NameToID("Enemy");
            if (enemyLayer != 0) visualSr.sortingLayerID = enemyLayer;
            visualSr.sortingOrder = 6;
        }

        if (patrolEnemy != null)
        {
            patrolEnemy.ScaleMaxHealth(healthMultiplier);
            patrolEnemy.SetDamage(2f);
        }

        var p = FindAnyObjectByType<Player>();
        if (p != null) player = p.transform;

        SetupHpBarAndBadge();

        nextBoomerangTime = Time.time + 2.2f;
        nextSlamTime = Time.time + 4.8f;
        nextWispTime = Time.time + 3.0f;

        var hf = GetComponent<HitFlash>();
        if (hf != null) hf.RefreshRenderers();
    }

    private void SetupHpBarAndBadge()
    {
        // Nếu đã gắn sẵn HpBar.prefab chuẩn của game -> dùng trực tiếp, không tạo thanh phụ đè lên!
        if (GetComponentInChildren<UnityEngine.UI.Image>() != null)
        {
            eliteHpBarRoot = null;
        }
        else
        {
            var existingBar = transform.Find("EliteHpBar");
            if (existingBar != null)
            {
                eliteHpBarRoot = existingBar.gameObject;
                var bgT = existingBar.Find("Bg");
                var pivT = existingBar.Find("FillPivot");
                if (bgT != null) eliteHpBgSr = bgT.GetComponent<SpriteRenderer>();
                if (pivT != null)
                {
                    eliteHpFill = pivT;
                    var fillT = pivT.Find("Fill");
                    if (fillT != null) eliteHpFillSr = fillT.GetComponent<SpriteRenderer>();
                }
            }
        }

        displayedHpRatio = 1f;

        if (eliteBadgeObj == null)
        {
            eliteBadgeObj = new GameObject("EliteExclamationBadge");
            eliteBadgeObj.transform.SetParent(transform, false);
            var tmp = eliteBadgeObj.AddComponent<TextMeshPro>();
            tmp.text = "<color=#00F5D4><b>!</b></color>";
            tmp.fontSize = 7.2f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            CombatVFX.ApplyVfxLayer(tmp.renderer, 120);
        }
    }

    private static Sprite cachedSolidSprite;
    private static Sprite GetSolidSprite()
    {
        if (cachedSolidSprite != null) return cachedSolidSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        cachedSolidSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return cachedSolidSprite;
    }

    private void Update()
    {
        if (rewarded) return;
        if (patrolEnemy != null && patrolEnemy.IsDead)
        {
            OnReaperKilled();
            return;
        }

        // Thanh máu đỏ viền đen luôn giữ màu chuẩn và tụt mượt từ từ phía trên đầu
        float topY = visualSr != null ? (visualSr.bounds.max.y + 0.30f) : (transform.position.y + 3.25f);
        if (eliteHpBarRoot != null)
        {
            eliteHpBarRoot.transform.rotation = Quaternion.identity;
            eliteHpBarRoot.transform.position = new Vector3(transform.position.x, topY, transform.position.z);
            if (eliteHpBgSr != null) eliteHpBgSr.color = Color.black;
            if (eliteHpFillSr != null) eliteHpFillSr.color = new Color(1f, 0.06f, 0.06f, 1f);
            if (eliteHpFill != null && patrolEnemy != null)
            {
                float target = Mathf.Clamp01(patrolEnemy.HpRatio);
                displayedHpRatio = Mathf.MoveTowards(displayedHpRatio, target, Time.deltaTime * 1.15f);
                eliteHpFill.localScale = new Vector3(displayedHpRatio, 1f, 1f);
            }
        }
        if (eliteBadgeObj != null)
        {
            eliteBadgeObj.transform.rotation = Quaternion.identity;
            float badgeY = topY;
            var hpImg = GetComponentInChildren<UnityEngine.UI.Image>();
            if (hpImg != null) badgeY = hpImg.transform.position.y;
            eliteBadgeObj.transform.position = new Vector3(transform.position.x - 1.25f, badgeY, transform.position.z);
        }

        // Hào quang lửa linh hồn xanh ngọc
        if (UnityEngine.Random.value < (enraged ? 0.22f : 0.11f))
        {
            Color soulCol = UnityEngine.Random.value < 0.5f
                ? new Color(0.1f, 0.96f, 0.82f, 0.85f)
                : new Color(0.25f, 0.72f, 1f, 0.8f);
            Vector3 off = new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(0.2f, 2.3f), 0f);
            CombatVFX.SpawnBurst(transform.position + off, soulCol, 1, 0.4f, 1.6f, 0.14f, 0.36f, -1.8f);
        }

        UpdateReaperAnimation();

        // Kích hoạt Cuồng Nộ Linh Hồn dưới 50% máu
        if (!enraged && patrolEnemy != null && patrolEnemy.HpRatio <= 0.5f)
        {
            enraged = true;
            patrolEnemy.SpeedBonus = 1.25f;
            DamagePopup.SpawnText(transform.position + Vector3.up * 2.8f, "SOUL WRATH!", new Color(0.15f, 1f, 0.85f), 6.4f);
            CombatVFX.SpawnShockwave(transform.position + Vector3.up * 1.2f, new Color(0.1f, 1f, 0.85f, 0.95f), 0.35f, 3.8f, 0.36f);
            CombatVFX.SpawnBurst(transform.position + Vector3.up * 1.2f, new Color(0.2f, 1f, 0.9f, 1f), 18, 2.5f, 7.5f, 0.2f, 0.42f, 2f);
        }

        if (player == null || isCasting || (telegraph != null && telegraph.IsStunned)) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float dy = Mathf.Abs(transform.position.y - player.position.y);
        if (dist > 11.5f || dy > 3.2f) return;

        float cdMult = enraged ? 0.72f : 1f;

        // Chiêu 3 (khi < 50% HP): Triệu hồi 2 Hồn Ma truy đuổi
        if (enraged && Time.time >= nextWispTime)
        {
            nextWispTime = Time.time + 5.2f;
            SpawnSoulWisps();
        }

        // Chiêu 2: Chấn Động Linh Hồn (2 luồng sóng chạy 2 bên)
        if (dist <= 7.0f && Time.time >= nextSlamTime)
        {
            nextSlamTime = Time.time + 6.4f * cdMult;
            StartCoroutine(SpectralSlamRoutine());
            return;
        }

        // Chiêu 1: Ném Lưỡi Hái Xoay Hồi Quy
        if (dist >= 3.0f && dist <= 11.0f && Time.time >= nextBoomerangTime)
        {
            nextBoomerangTime = Time.time + 4.5f * cdMult;
            StartCoroutine(ScytheBoomerangRoutine());
        }
    }

    private void UpdateReaperAnimation()
    {
        if (visualSr == null || sIdle == null) return;

        bool stunned = telegraph != null && telegraph.IsStunned;
        bool attacking = Time.time < attackPoseUntil || (patrolEnemy != null && patrolEnemy.IsAttacking);
        float speedX = Mathf.Abs(transform.position.x - lastPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        if (stunned)
        {
            visualSr.sprite = sHurt ?? sIdle;
            return;
        }
        if (attacking)
        {
            visualSr.sprite = sAttack ?? sIdle;
            return;
        }
        if (speedX > 0.25f)
        {
            walkTimer += Time.deltaTime * 6.5f;
            visualSr.sprite = (Mathf.FloorToInt(walkTimer) % 2 == 1 && sWalk != null) ? sWalk : sIdle;
        }
        else
        {
            visualSr.sprite = sIdle;
        }
    }

    // ==================== CHIÊU 1: SOUL SCYTHE BOOMERANG ====================
    private IEnumerator ScytheBoomerangRoutine()
    {
        isCasting = true;
        if (patrolEnemy != null)
        {
            patrolEnemy.ExternalControl = true;
            if (player != null) patrolEnemy.FaceDirection(player.position.x - transform.position.x);
        }

        if (telegraph != null) telegraph.ShowWarning(0.42f, null);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 1.4f, new Color(0.15f, 1f, 0.85f, 0.95f), 8, 1.2f, 3.5f, 0.14f, 0.28f, 0f);
        yield return new WaitForSeconds(0.42f);

        if ((patrolEnemy != null && patrolEnemy.IsDead) || (telegraph != null && telegraph.IsStunned))
        {
            EndCast();
            yield break;
        }

        attackPoseUntil = Time.time + 0.45f;
        float dir = (patrolEnemy == null || patrolEnemy.IsFacingRight) ? 1f : -1f;
        ReaperScytheBoomerang.Spawn(transform, transform.position + new Vector3(dir * 0.9f, 1.2f, 0f), dir, sScythe, 2f);

        yield return new WaitForSeconds(0.35f);
        EndCast();
    }

    // ==================== CHIÊU 2: SPECTRAL SHOCKWAVE SLAM ====================
    private IEnumerator SpectralSlamRoutine()
    {
        isCasting = true;
        if (patrolEnemy != null)
        {
            patrolEnemy.ExternalControl = true;
            if (player != null) patrolEnemy.FaceDirection(player.position.x - transform.position.x);
        }

        if (telegraph != null) telegraph.ShowWarning(0.55f, null);
        DamagePopup.SpawnText(transform.position + Vector3.up * 2.5f, "SOUL SLAM!", new Color(0.15f, 0.98f, 0.88f), 5.6f);
        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 0.4f, new Color(0.1f, 0.95f, 0.85f, 0.9f), 0.3f, 2.5f, 0.35f);
        yield return new WaitForSeconds(0.55f);

        if ((patrolEnemy != null && patrolEnemy.IsDead) || (telegraph != null && telegraph.IsStunned))
        {
            EndCast();
            yield break;
        }

        attackPoseUntil = Time.time + 0.45f;
        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 0.4f, new Color(0.1f, 1f, 0.85f, 0.98f), 0.4f, 3.8f, 0.32f);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 0.4f, new Color(0.2f, 1f, 0.9f, 1f), 16, 2.5f, 7f, 0.18f, 0.4f, 3f);

        // Bắn 2 luồng sóng linh hồn chạy sang Trái và Phải
        ReaperSoulWave.Spawn(transform.position + Vector3.up * 0.5f, 1f, 2f, gameObject);
        ReaperSoulWave.Spawn(transform.position + Vector3.up * 0.5f, -1f, 2f, gameObject);

        yield return new WaitForSeconds(0.35f);
        EndCast();
    }

    // ==================== CHIÊU 3: HOMING SOUL WISPS (< 50% HP) ====================
    private void SpawnSoulWisps()
    {
        if (player == null) return;
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 spawnPos = transform.position + new Vector3(i * 1.1f, 2.2f, 0f);
            ReaperSoulWisp.Spawn(spawnPos, player, 2f, gameObject);
        }
    }

    private void EndCast()
    {
        isCasting = false;
        if (patrolEnemy != null) patrolEnemy.ExternalControl = false;
    }

    public void OnReaperKilled()
    {
        if (rewarded) return;
        rewarded = true;
        StopAllCoroutines();
        EndCast();

        if (eliteHpBarRoot != null) Destroy(eliteHpBarRoot);
        if (eliteBadgeObj != null) Destroy(eliteBadgeObj);

        RunManager.Get().AddGold(goldReward);
        RunManager.Get().AddCells(cellReward);

        DamagePopup.SpawnText(transform.position + Vector3.up * 2.2f, $"REAPER SLAIN! +{goldReward}G +{cellReward}C", new Color(0.15f, 1f, 0.85f), 7.2f);
        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 1.1f, new Color(0.15f, 1f, 0.85f, 0.95f), 0.4f, 4.4f, 0.45f);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 1.1f, new Color(0.2f, 1f, 0.9f, 1f), 26, 3.5f, 9f, 0.25f, 0.55f, 4f);

        if (!ScrollUpgradeUI.IsOpen && !CursedSkillUI.IsOpen)
        {
            MutationSystem.ShowMutationPicker("SPECTRAL REWARD — CHOOSE MUTATION!");
        }
    }
}

/// <summary>Lưỡi Hái Linh Hồn xoay bay xa rồi quay ngược về Tử Thần.</summary>
public class ReaperScytheBoomerang : MonoBehaviour
{
    private Transform owner;
    private float dir;
    private float damage;
    private float timer;
    private bool returning;
    private float nextHitTime;

    public static void Spawn(Transform reaper, Vector3 pos, float direction, Sprite sprite, float dmg)
    {
        var go = new GameObject("ReaperScytheBoomerang");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite ?? CombatVFX.GetCircleSprite();
        sr.color = Color.white;
        CombatVFX.ApplyVfxLayer(sr, 74);
        go.transform.localScale = Vector3.one * 1.15f;

        var b = go.AddComponent<ReaperScytheBoomerang>();
        b.owner = reaper;
        b.dir = direction;
        b.damage = dmg;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.Rotate(0f, 0f, -720f * dir * Time.deltaTime);

        if (!returning)
        {
            transform.position += new Vector3(dir * 11.5f * Time.deltaTime, 0f, 0f);
            if (timer >= 0.75f) returning = true;
        }
        else
        {
            if (owner == null || timer >= 2.0f) { Destroy(gameObject); return; }
            Vector3 home = owner.position + Vector3.up * 1.2f;
            transform.position = Vector3.MoveTowards(transform.position, home, 13.0f * Time.deltaTime);
            if (Vector2.Distance(transform.position, home) < 0.7f) { Destroy(gameObject); return; }
        }

        if (UnityEngine.Random.value < 0.4f)
            CombatVFX.SpawnBurst(transform.position, new Color(0.15f, 1f, 0.85f, 0.85f), 1, 0.5f, 1.8f, 0.1f, 0.2f, 0f);

        if (Time.time >= nextHitTime)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null && Vector2.Distance(transform.position, p.transform.position + Vector3.up * 0.8f) <= 1.05f)
            {
                nextHitTime = Time.time + 0.65f;
                p.TakeDamageP(damage, transform.position, owner != null ? owner.gameObject : gameObject);
                CombatVFX.SpawnBurst(p.transform.position + Vector3.up * 0.8f, new Color(0.15f, 1f, 0.85f, 1f), 10, 2f, 5.5f, 0.14f, 0.28f, 2f);
            }
        }
    }
}

/// <summary>Sóng Linh Hồn bò dọc mặt đất 2 hướng trái/phải.</summary>
public class ReaperSoulWave : MonoBehaviour
{
    private float dir, damage, life = 0.85f;
    private GameObject owner;
    private bool hit;

    public static void Spawn(Vector3 pos, float direction, float dmg, GameObject caster)
    {
        var go = new GameObject("ReaperSoulWave");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVFX.GetCircleSprite();
        sr.color = new Color(0.15f, 1f, 0.85f, 0.92f);
        CombatVFX.ApplyVfxLayer(sr, 72);
        go.transform.localScale = new Vector3(0.7f, 1.35f, 1f);

        var w = go.AddComponent<ReaperSoulWave>();
        w.dir = direction;
        w.damage = dmg;
        w.owner = caster;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        transform.position += new Vector3(dir * 9.5f * Time.deltaTime, 0f, 0f);
        if (UnityEngine.Random.value < 0.5f)
            CombatVFX.SpawnBurst(transform.position, new Color(0.15f, 0.98f, 0.88f, 0.85f), 1, 0.6f, 2.2f, 0.12f, 0.25f, -1.5f);

        if (!hit)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null && Vector2.Distance(transform.position, p.transform.position + Vector3.up * 0.6f) <= 0.95f)
            {
                hit = true;
                p.TakeDamageP(damage, transform.position, owner != null ? owner : gameObject);
                CombatVFX.SpawnShockwave(transform.position, new Color(0.15f, 1f, 0.85f, 0.95f), 0.2f, 1.8f, 0.2f);
                Destroy(gameObject);
            }
        }
    }
}

/// <summary>Hồn Ma truy đuổi nhẹ sinh ra khi Tử Thần dưới 50% máu.</summary>
public class ReaperSoulWisp : MonoBehaviour
{
    private Transform target;
    private Vector2 dir;
    private float damage, life = 3.2f;
    private GameObject owner;

    public static void Spawn(Vector3 pos, Transform targetPlayer, float dmg, GameObject caster)
    {
        var go = new GameObject("ReaperSoulWisp");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVFX.GetCircleSprite();
        sr.color = new Color(0.2f, 1f, 0.9f, 0.95f);
        CombatVFX.ApplyVfxLayer(sr, 73);
        go.transform.localScale = Vector3.one * 0.52f;

        var w = go.AddComponent<ReaperSoulWisp>();
        w.target = targetPlayer;
        w.damage = dmg;
        w.owner = caster;
        w.dir = targetPlayer != null ? ((Vector2)(targetPlayer.position + Vector3.up * 0.8f - pos)).normalized : Vector2.right;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        if (target != null)
        {
            Vector2 desired = ((Vector2)(target.position + Vector3.up * 0.8f - transform.position)).normalized;
            dir = Vector2.Lerp(dir, desired, Time.deltaTime * 3.0f).normalized;
        }
        transform.position += (Vector3)(dir * 6.5f * Time.deltaTime);

        if (UnityEngine.Random.value < 0.35f)
            CombatVFX.SpawnBurst(transform.position, new Color(0.2f, 1f, 0.9f, 0.75f), 1, 0.3f, 1f, 0.1f, 0.2f, 0f);

        if (target != null && Vector2.Distance(transform.position, target.position + Vector3.up * 0.8f) <= 0.8f)
        {
            var pc = target.GetComponent<Player>();
            if (pc != null) pc.TakeDamageP(damage, transform.position, owner != null ? owner : gameObject);
            CombatVFX.SpawnBurst(transform.position, new Color(0.2f, 1f, 0.9f, 1f), 10, 2f, 5f, 0.14f, 0.26f, 1f);
            Destroy(gameObject);
        }
    }
}
