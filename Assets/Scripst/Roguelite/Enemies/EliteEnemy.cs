using System.Collections;
using TMPro;
using UnityEngine;

public enum EliteAffix
{
    Teleporter, // Dịch chuyển tức thời ra sau lưng người chơi có cảnh báo dung nham
    LaserOrbs   // 2 cầu lửa địa ngục xoay quanh thân gây sát thương khi lại gần
}

/// <summary>
/// Quái Tinh Anh Địa Ngục — ★ INFERNAL ABYSSAL KNIGHT ★:
/// - Tự động thay thế ngoại hình quái thường thành Ác Quỷ Địa Ngục pixel-art Hi-bit (chế tác bằng agent-sprite-forge)
///   với đầy đủ trạng thái Idle, Walk, Overhead Wind-up + Molten Cleave Attack, và Hurt!
/// - Giữ nguyên Animator gốc ẩn đi để không làm hỏng Animation Event Attack() / OnDieAnimationEnd().
/// - Máu x2, hào quang lửa địa ngục, kỹ năng đặc biệt (Infernal Blink / Hellfire Orbs), hạ gục thưởng Gold/Cells + Mutation!
/// </summary>
public class EliteEnemy : MonoBehaviour
{
    [SerializeField] private EliteAffix affix = EliteAffix.Teleporter;
    [SerializeField] private bool randomizeAffix = true;
    [SerializeField] private float healthMultiplier = 2.0f;
    [SerializeField] private int goldReward = 20;
    [SerializeField] private int cellReward = 8;

    private static Sprite cachedIdle;
    private static Sprite cachedWalk;
    private static Sprite cachedAttack;
    private static Sprite cachedHurt;
    private static bool spritesLoaded;

    private Transform player;
    private Animator hostAnimator;
    private EnemyTelegraph telegraph;
    private PatrolEnemy1 patrolEnemy;
    private GameObject labelObj;
    private GameObject eliteBadgeObj;
    private GameObject eliteHpBarRoot;
    private Transform eliteHpFill;
    private SpriteRenderer eliteHpBgSr;
    private SpriteRenderer eliteHpFillSr;
    private float displayedHpRatio = 1f;
    private GameObject visualObj;
    private SpriteRenderer visualSr;
    private readonly Transform[] orbitOrbs = new Transform[2];

    private Vector3 spawnOrigin;
    private Vector3 lastPosition;
    private float walkAnimTimer;
    private float attackAnimTimer;
    private bool wasAttacking;
    private float orbitAngle;
    private float nextTeleportTime;
    private float nextOrbHitTime;
    private bool rewarded;

    public EliteAffix Affix => affix;

    private static void LoadHellDemonSprites()
    {
        if (spritesLoaded) return;
        spritesLoaded = true;
        cachedIdle = Resources.Load<Sprite>("HellElite/hell_idle");
        cachedWalk = Resources.Load<Sprite>("HellElite/hell_walk");
        cachedAttack = Resources.Load<Sprite>("HellElite/hell_attack");
        cachedHurt = Resources.Load<Sprite>("HellElite/hell_hurt");
    }

    private void Start()
    {
        if (randomizeAffix)
        {
            affix = UnityEngine.Random.value < 0.5f ? EliteAffix.Teleporter : EliteAffix.LaserOrbs;
        }

        hostAnimator = GetComponent<Animator>();
        patrolEnemy = GetComponent<PatrolEnemy1>();
        spawnOrigin = transform.position;
        lastPosition = transform.position;

        // Nạp bộ sprite Ác Quỷ Địa Ngục Hi-bit và thay thế ngoại hình sạch sẽ
        LoadHellDemonSprites();
        if (cachedIdle != null)
        {
            InstallHellDemonVisual();
        }
        else
        {
            // Fallback an toàn nếu chưa import xong sprite
            transform.localScale *= 1.18f;
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr != null) sr.color = new Color(1f, 0.55f, 0.45f, sr.color.a);
            }
        }

        // Tăng gấp đôi máu & đảm bảo đòn đánh gây 2 damage theo yêu cầu cân bằng
        if (patrolEnemy != null)
        {
            patrolEnemy.ScaleMaxHealth(healthMultiplier);
            patrolEnemy.SetDamage(2f);
        }

        var hp = GetComponent<Health>();
        if (hp != null) hp.Heal(hp.Max);

        telegraph = GetComponent<EnemyTelegraph>() ?? gameObject.AddComponent<EnemyTelegraph>();
        var p = FindAnyObjectByType<Player>();
        if (p != null) player = p.transform;

        // Hiện dấu chấm than bên trái thanh máu của quái tinh anh theo yêu cầu thiết kế
        CreateEliteBadge();
        if (affix == EliteAffix.LaserOrbs) CreateOrbitOrbs();
        nextTeleportTime = Time.time + 3.8f;

        // Làm mới HitFlash để nháy trúng cả sprite Ác Quỷ Địa Ngục mới
        var hf = GetComponent<HitFlash>();
        if (hf != null) hf.RefreshRenderers();
    }

    private void CreateEliteBadge()
    {
        if (eliteBadgeObj != null) return;

        // Tận dụng EliteHpBar đã bake sẵn trên Scene (hoặc tự tạo nếu chưa có)
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
            displayedHpRatio = 1f;
        }
        else if (GetComponentInChildren<UnityEngine.UI.Image>() == null && eliteHpBarRoot == null)
        {
            eliteHpBarRoot = new GameObject("EliteHpBar");
            eliteHpBarRoot.transform.SetParent(transform, false);
            eliteHpBarRoot.transform.localPosition = new Vector3(0f, 2.75f, 0f);

            var bgObj = new GameObject("Bg");
            bgObj.transform.SetParent(eliteHpBarRoot.transform, false);
            eliteHpBgSr = bgObj.AddComponent<SpriteRenderer>();
            eliteHpBgSr.sprite = GetSolidSprite();
            eliteHpBgSr.color = Color.black; // Viền đen sắc nét giống thanh máu quái thường
            bgObj.transform.localScale = new Vector3(1.68f, 0.28f, 1f);
            CombatVFX.ApplyVfxLayer(eliteHpBgSr, 110);

            var fillPivot = new GameObject("FillPivot");
            fillPivot.transform.SetParent(eliteHpBarRoot.transform, false);
            fillPivot.transform.localPosition = new Vector3(-0.76f, 0f, 0f);

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillPivot.transform, false);
            fillObj.transform.localPosition = new Vector3(0.76f, 0f, 0f);
            eliteHpFillSr = fillObj.AddComponent<SpriteRenderer>();
            eliteHpFillSr.sprite = GetSolidSprite();
            eliteHpFillSr.color = new Color(1f, 0.06f, 0.06f, 1f); // Đỏ tươi cố định giống ảnh 1
            fillObj.transform.localScale = new Vector3(1.52f, 0.16f, 1f);
            CombatVFX.ApplyVfxLayer(eliteHpFillSr, 111);
            eliteHpFill = fillPivot.transform;
            displayedHpRatio = 1f;
        }

        eliteBadgeObj = new GameObject("EliteExclamationBadge");
        eliteBadgeObj.transform.SetParent(transform, false);
        eliteBadgeObj.transform.localPosition = new Vector3(-1.15f, 2.75f, 0f);

        var tmp = eliteBadgeObj.AddComponent<TextMeshPro>();
        tmp.text = "<color=#FF6600><b>!</b></color>";
        tmp.fontSize = 7.0f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        CombatVFX.ApplyVfxLayer(tmp.renderer, 120);
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

    private void InstallHellDemonVisual()
    {
        int enemyLayerId = SortingLayer.NameToID("Enemy");
        var rootSr = GetComponent<SpriteRenderer>();

        // Nếu không có Animator gốc (Quái Tinh Anh độc lập trên map) -> dùng trực tiếp SpriteRenderer gốc!
        // Tránh tạo object con gây tách đôi khi di chuyển bằng phím W trong Scene View.
        if (hostAnimator == null && rootSr != null)
        {
            var oldChild = transform.Find("HellDemonVisual");
            if (oldChild != null) Destroy(oldChild.gameObject);

            visualObj = gameObject;
            visualSr = rootSr;
            visualSr.enabled = true;
            visualSr.sprite = cachedIdle;
            visualSr.color = Color.white;
            if (enemyLayerId != 0) visualSr.sortingLayerID = enemyLayerId;
            visualSr.sortingOrder = 6;
            return;
        }

        // Trường hợp nâng cấp từ quái có sẵn Animator -> ẩn SpriteRenderer gốc để Animator không đè hình
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr == null) continue;
            if (sr.gameObject == gameObject || (sr.transform.parent == transform && sr.gameObject.name != "HellDemonVisual"))
            {
                if (sr.sortingLayerID != 0) enemyLayerId = sr.sortingLayerID;
                sr.enabled = false;
            }
        }

        var existingChild = transform.Find("HellDemonVisual");
        if (existingChild != null)
        {
            visualObj = existingChild.gameObject;
            visualSr = visualObj.GetComponent<SpriteRenderer>();
            if (visualSr != null)
            {
                visualSr.enabled = true;
                if (cachedIdle != null && visualSr.sprite == null) visualSr.sprite = cachedIdle;
            }
            return;
        }

        visualObj = new GameObject("HellDemonVisual");
        visualObj.transform.SetParent(transform, false);
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localRotation = Quaternion.identity;
        visualObj.transform.localScale = Vector3.one * 1.16f;

        visualSr = visualObj.AddComponent<SpriteRenderer>();
        visualSr.sprite = cachedIdle;
        visualSr.color = Color.white;
        if (enemyLayerId != 0) visualSr.sortingLayerID = enemyLayerId;
        visualSr.sortingOrder = 6;
    }

    private void CreateEliteLabel()
    {
        // Gỡ bỏ chữ trên đầu theo yêu cầu để ngoại hình sừng, giáp và mắt rực lửa của quái tinh anh rõ nét
    }

    private void CreateOrbitOrbs()
    {
        for (int i = 0; i < 2; i++)
        {
            var orb = new GameObject("HellfireOrb_" + i);
            orb.transform.SetParent(transform);
            var sr = orb.AddComponent<SpriteRenderer>();
            sr.sprite = CombatVFX.GetCircleSprite();
            sr.color = new Color(1f, 0.35f, 0.08f, 0.96f); // Quả cầu lửa dung nham địa ngục
            CombatVFX.ApplyVfxLayer(sr, 56);
            orb.transform.localScale = Vector3.one * 0.46f;
            orbitOrbs[i] = orb.transform;
        }
    }

    private void Update()
    {
        if (rewarded) return;

        if (labelObj != null) labelObj.transform.rotation = Quaternion.identity;

        float topY = visualSr != null ? (visualSr.bounds.max.y + 0.30f) : (transform.position.y + 3.25f);

        if (eliteHpBarRoot != null)
        {
            eliteHpBarRoot.transform.rotation = Quaternion.identity;
            eliteHpBarRoot.transform.position = new Vector3(transform.position.x, topY, transform.position.z);
            if (eliteHpBgSr != null) eliteHpBgSr.color = Color.black;
            if (eliteHpFillSr != null) eliteHpFillSr.color = new Color(1f, 0.06f, 0.06f, 1f);
            if (eliteHpFill != null && patrolEnemy != null)
            {
                // Máu tụt xuống từ từ mượt mà, không đổi màu khi bị đánh trúng
                float targetRatio = Mathf.Clamp01(patrolEnemy.HpRatio);
                displayedHpRatio = Mathf.MoveTowards(displayedHpRatio, targetRatio, Time.deltaTime * 1.15f);
                eliteHpFill.localScale = new Vector3(displayedHpRatio, 1f, 1f);
            }
        }

        if (eliteBadgeObj != null)
        {
            eliteBadgeObj.transform.rotation = Quaternion.identity;
            float yWorld = topY;
            var hpImg = GetComponentInChildren<UnityEngine.UI.Image>();
            if (hpImg != null) yWorld = hpImg.transform.position.y;
            eliteBadgeObj.transform.position = new Vector3(transform.position.x - 1.12f, yWorld, transform.position.z);
        }

        // Hào quang tàn lửa địa ngục bay lên quanh thân Ác Quỷ
        if (UnityEngine.Random.value < 0.12f)
        {
            Color ember = UnityEngine.Random.value < 0.5f
                ? new Color(1f, 0.42f, 0.1f, 0.85f)
                : new Color(0.95f, 0.16f, 0.18f, 0.8f);
            Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(0.2f, 2.2f), 0f);
            CombatVFX.SpawnBurst(transform.position + offset, ember, 1, 0.5f, 1.8f, 0.14f, 0.38f, -2.0f);
        }

        UpdateHellDemonAnimation();

        if (player == null || (telegraph != null && telegraph.IsStunned)) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float dy = Mathf.Abs(transform.position.y - player.position.y);
        if (dist > 10f) return;

        // Chỉ dịch chuyển khi người chơi đang giao chiến trên CÙNG tầng đất & khu vực của Quái Tinh Anh
        // (Tuyệt đối không dịch chuyển xuyên vách đá hay bỏ vị trí về đầu map!)
        bool sameFloor = dy <= 2.2f
                      && Mathf.Abs(player.position.y - spawnOrigin.y) <= 2.5f
                      && Mathf.Abs(player.position.x - spawnOrigin.x) <= 12f;

        if (affix == EliteAffix.Teleporter && sameFloor && Time.time >= nextTeleportTime && dist > 3.2f && dist <= 8.5f)
        {
            nextTeleportTime = Time.time + 5.2f;
            StartCoroutine(TeleportBehindPlayerRoutine());
        }
        else if (affix == EliteAffix.LaserOrbs)
        {
            UpdateOrbitOrbs();
        }
    }

    private void UpdateHellDemonAnimation()
    {
        if (visualSr == null || visualObj == null) return;

        bool isChildVisual = visualObj != gameObject;
        bool isStunned = telegraph != null && telegraph.IsStunned;
        bool isAttacking = (hostAnimator != null && hostAnimator.GetBool("Attack1"))
                        || (patrolEnemy != null && patrolEnemy.IsAttacking);
        float speedX = Mathf.Abs(transform.position.x - lastPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        if (isStunned)
        {
            visualSr.sprite = cachedHurt != null ? cachedHurt : cachedIdle;
            if (isChildVisual)
            {
                float shake = Mathf.Sin(Time.time * 40f) * 0.04f;
                visualObj.transform.localPosition = new Vector3(shake, 0f, 0f);
                visualObj.transform.localScale = Vector3.one * 1.16f;
            }
            wasAttacking = false;
            return;
        }

        if (isAttacking)
        {
            if (!wasAttacking)
            {
                attackAnimTimer = 0f;
                wasAttacking = true;
            }
            attackAnimTimer += Time.deltaTime;
            float cycle = attackAnimTimer % 0.72f;

            // Giai đoạn 1 (0..0.28s): Vung đại kiếm qua vai tụ lực (hell_hurt)
            // Giai đoạn 2 (0.28..0.72s): Chém xả dung nham xuống (hell_attack) + lóe vệt lửa!
            if (cycle < 0.28f)
            {
                visualSr.sprite = cachedHurt != null ? cachedHurt : cachedIdle;
                if (isChildVisual) visualObj.transform.localPosition = new Vector3(-0.08f, 0f, 0f);
            }
            else
            {
                if (visualSr.sprite != cachedAttack && cachedAttack != null)
                {
                    bool faceR = patrolEnemy == null || patrolEnemy.IsFacingRight;
                    Vector3 slashPos = transform.position + new Vector3(faceR ? 1.35f : -1.35f, 1.1f, 0f);
                    CombatVFX.SpawnSlashArc(slashPos, faceR, 3, isCrit: true);
                }
                visualSr.sprite = cachedAttack != null ? cachedAttack : cachedIdle;
                if (isChildVisual) visualObj.transform.localPosition = new Vector3(0.14f, 0f, 0f);
            }
            if (isChildVisual) visualObj.transform.localScale = Vector3.one * 1.18f;
            return;
        }

        wasAttacking = false;
        if (isChildVisual) visualObj.transform.localPosition = Vector3.zero;

        if (speedX > 0.25f)
        {
            walkAnimTimer += Time.deltaTime * 6.5f;
            bool stepFrame = Mathf.FloorToInt(walkAnimTimer) % 2 == 1;
            visualSr.sprite = (stepFrame && cachedWalk != null) ? cachedWalk : cachedIdle;
            if (isChildVisual)
            {
                float bobY = Mathf.Abs(Mathf.Sin(walkAnimTimer * Mathf.PI)) * 0.08f;
                visualObj.transform.localPosition = new Vector3(0f, bobY, 0f);
                visualObj.transform.localScale = Vector3.one * 1.16f;
            }
        }
        else
        {
            visualSr.sprite = cachedIdle;
            if (isChildVisual)
            {
                float breathe = 1f + 0.025f * Mathf.Sin(Time.time * 3.5f);
                visualObj.transform.localScale = new Vector3(1.16f, 1.16f * breathe, 1f);
            }
        }
    }

    private IEnumerator TeleportBehindPlayerRoutine()
    {
        DamagePopup.SpawnText(transform.position + Vector3.up * 2.5f, "HELL BLINK!", new Color(1f, 0.35f, 0.12f), 5.8f);
        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 1.1f, new Color(1f, 0.3f, 0.1f, 0.92f), 0.3f, 2.2f, 0.28f);
        yield return new WaitForSeconds(0.35f);

        if (rewarded || player == null || (telegraph != null && telegraph.IsStunned)) yield break;

        var pComp = player.GetComponent<Player>();
        float behindDir = (pComp != null && pComp.IsFacingRight) ? -1f : 1f;
        Vector3 dest = player.position + new Vector3(behindDir * 2.2f, 0.15f, 0f);

        // Kiểm tra điểm đến vẫn nằm trên cùng nền đất & lãnh thổ của Quái Tinh Anh
        if (Mathf.Abs(dest.y - spawnOrigin.y) > 2.5f || Mathf.Abs(dest.x - spawnOrigin.x) > 13f) yield break;

        LayerMask groundMask = patrolEnemy != null ? patrolEnemy.GroundMask : (LayerMask)128;
        if (Physics2D.Raycast(dest + Vector3.up * 0.5f, Vector2.down, 2.5f, groundMask))
        {
            transform.position = dest;
            CombatVFX.SpawnShockwave(dest + Vector3.up * 1.1f, new Color(1f, 0.35f, 0.1f, 0.95f), 0.3f, 2.4f, 0.28f);
            CombatVFX.SpawnBurst(dest + Vector3.up * 1.0f, new Color(1f, 0.45f, 0.1f, 0.95f), 12, 2.5f, 6f, 0.2f, 0.35f, 2f);
        }
    }

    private void UpdateOrbitOrbs()
    {
        orbitAngle += 180f * Time.deltaTime;
        const float radius = 1.85f;

        for (int i = 0; i < orbitOrbs.Length; i++)
        {
            if (orbitOrbs[i] == null) continue;
            float rad = (orbitAngle + i * 180f) * Mathf.Deg2Rad;
            Vector3 worldPos = transform.position + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius * 0.72f + 1.1f, 0f);
            orbitOrbs[i].position = worldPos;

            if (UnityEngine.Random.value < 0.25f)
            {
                CombatVFX.SpawnBurst(worldPos, new Color(1f, 0.48f, 0.1f, 0.85f), 1, 0.4f, 1.2f, 0.12f, 0.2f, 0f);
            }

            if (Time.time >= nextOrbHitTime && player != null && Vector2.Distance(worldPos, player.position) < 0.9f)
            {
                nextOrbHitTime = Time.time + 0.85f;
                var pComp = player.GetComponent<Player>();
                if (pComp != null) pComp.TakeDamageP(2f, worldPos, gameObject);
            }
        }
    }

    /// <summary>Gọi khi Quái Tinh Anh Địa Ngục bị hạ gục — nổ lửa địa ngục, thưởng Gold/Cells & mở bảng chọn Mutation!</summary>
    public void OnEliteKilled()
    {
        if (rewarded) return;
        rewarded = true;

        foreach (var orb in orbitOrbs) if (orb != null) Destroy(orb.gameObject);
        if (labelObj != null) Destroy(labelObj);
        if (eliteBadgeObj != null) Destroy(eliteBadgeObj);
        if (eliteHpBarRoot != null) Destroy(eliteHpBarRoot);
        if (visualObj != null && visualObj != gameObject) Destroy(visualObj);

        RunManager.Get().AddGold(goldReward);
        RunManager.Get().AddCells(cellReward);

        DamagePopup.SpawnText(transform.position + Vector3.up * 2.0f, $"INFERNAL SLAIN! +{goldReward}G +{cellReward}C", new Color(1f, 0.55f, 0.18f), 7.2f);
        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 1.0f, new Color(1f, 0.35f, 0.1f, 0.95f), 0.4f, 4.2f, 0.45f);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 1.0f, new Color(1f, 0.45f, 0.12f, 1f), 24, 3.5f, 9f, 0.26f, 0.55f, 5f);

        if (!ScrollUpgradeUI.IsOpen)
        {
            MutationSystem.ShowMutationPicker("INFERNAL REWARD — CHOOSE MUTATION!");
        }
    }
}
