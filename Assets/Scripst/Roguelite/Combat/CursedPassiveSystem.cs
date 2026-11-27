using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CursedSkillId
{
    StormCloud,   // Đám mây giựt sét quái vật
    HellfireOrbs, // Các cầu lửa địa ngục bay xung quanh
    VoidBlades,   // Kiếm hư không tự phóng truy đuổi quái
    FrostNova     // Vòng băng hồn bộc phá & mảnh băng làm chậm
}

public struct CursedSkillInfo
{
    public CursedSkillId Id;
    public string Name;
    public string Tag;
    public string IconPath;
    public Color AccentColor;
    public string Description;
    public string UpgradeHint;
}

/// <summary>
/// Hệ thống Kỹ Năng Bị Động từ Rương Nguyền Rủa (Cursed Passive Skills):
/// - Lưu trạng thái xuyên suốt 1 run (Map 1 -> Map 2 -> Map 3), tự gắn vào Player khi qua màn.
/// - Sử dụng toàn bộ sprite Hi-bit pixel-art từ Resources/CursedSkills/ (chế tác qua agent-sprite-forge).
/// </summary>
public static class CursedPassiveSystem
{
    public const int MaxLevel = 3;

    private static readonly Dictionary<CursedSkillId, int> levels = new Dictionary<CursedSkillId, int>();
    public static event Action OnSkillsChanged;

    public static readonly CursedSkillInfo[] Catalog =
    {
        new CursedSkillInfo
        {
            Id = CursedSkillId.StormCloud,
            Name = "LÔI VÂN NGUYỀN RỦA",
            Tag = "ĐÁM MÂY GIỰT SÉT",
            IconPath = "CursedSkills/storm_cloud",
            AccentColor = new Color(0.28f, 0.85f, 1f),
            Description = "• Triệu hồi đám mây sấm sét bay phía trên nhân vật.\n• Tự động giáng tia sét xuống quái gần nhất (25% gây choáng nhẹ).",
            UpgradeHint = "Tăng sát thương sét, giảm hồi chiêu & giật đa mục tiêu ở Cấp 3!"
        },
        new CursedSkillInfo
        {
            Id = CursedSkillId.HellfireOrbs,
            Name = "HỎA CẦU ĐỊA NGỤC",
            Tag = "CẦU LỬA XOAY QUANH",
            IconPath = "CursedSkills/fire_orb",
            AccentColor = new Color(1f, 0.42f, 0.14f),
            Description = "• Triệu hồi các quả cầu lửa dung nham xoay tròn quanh thân.\n• Thiêu đốt mọi quái chạm vào & gây hiệu ứng Bỏng (Burn).",
            UpgradeHint = "Cấp 1: 2 cầu lửa -> Cấp 2: 3 cầu lửa -> Cấp 3: 4 cầu lửa cực nhanh!"
        },
        new CursedSkillInfo
        {
            Id = CursedSkillId.VoidBlades,
            Name = "HUYẾT KIẾM HƯ KHÔNG",
            Tag = "KIẾM MA TỰ PHÓNG",
            IconPath = "CursedSkills/void_blade",
            AccentColor = new Color(0.76f, 0.34f, 1f),
            Description = "• Triệu hồi thanh kiếm bóng tối lơ lửng sau lưng nhân vật.\n• Tự động khóa mục tiêu & phóng xuyên quái (35% Chí mạng).",
            UpgradeHint = "Mỗi cấp tăng thêm +1 thanh kiếm phóng liên hoàn & tăng sát thương!"
        },
        new CursedSkillInfo
        {
            Id = CursedSkillId.FrostNova,
            Name = "BĂNG HỒN BỘC PHÁ",
            Tag = "VÒNG BĂNG LÀM CHẬM",
            IconPath = "CursedSkills/frost_shard",
            AccentColor = new Color(0.38f, 0.92f, 0.98f),
            Description = "• Định kỳ kích nổ vòng băng hồn & phóng mảnh tinh thể xung quanh.\n• Gây sát thương băng và làm chậm 45% tốc độ quái (Frost).",
            UpgradeHint = "Giảm hồi chiêu, tăng số lượng mảnh băng tinh thể & sát thương!"
        }
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        levels.Clear();
        OnSkillsChanged = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureRunnerOnPlayer();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (levels.Count > 0) EnsureRunnerOnPlayer();
    }

    public static void ResetAll()
    {
        levels.Clear();
        OnSkillsChanged?.Invoke();
    }

    public static int GetLevel(CursedSkillId id)
    {
        return levels.TryGetValue(id, out int lv) ? lv : 0;
    }

    public static IReadOnlyDictionary<CursedSkillId, int> ActiveSkills => levels;

    public static CursedSkillInfo GetInfo(CursedSkillId id)
    {
        foreach (var item in Catalog)
            if (item.Id == id) return item;
        return Catalog[0];
    }

    public static void GrantOrUpgrade(CursedSkillId id)
    {
        int current = GetLevel(id);
        int next = Mathf.Min(current + 1, MaxLevel);
        levels[id] = next;

        var runner = EnsureRunnerOnPlayer();
        if (runner != null) runner.SyncVisuals();

        var info = GetInfo(id);
        var p = UnityEngine.Object.FindAnyObjectByType<Player>();
        if (p != null)
        {
            string tag = current == 0 ? $"NEW: {info.Name}!" : $"{info.Name} LV.{next}!";
            DamagePopup.SpawnText(p.transform.position + Vector3.up * 2.2f, tag, info.AccentColor, 6.8f);
            CombatVFX.SpawnShockwave(p.transform.position + Vector3.up * 0.8f, info.AccentColor, 0.35f, 3.6f, 0.38f);
            CombatVFX.SpawnBurst(p.transform.position + Vector3.up * 0.8f, info.AccentColor, 18, 2.5f, 7.5f, 0.22f, 0.45f, 3f);
        }

        OnSkillsChanged?.Invoke();
    }

    public static CursedPassiveRunner EnsureRunnerOnPlayer()
    {
        var p = UnityEngine.Object.FindAnyObjectByType<Player>();
        if (p == null) return null;
        var runner = p.GetComponent<CursedPassiveRunner>() ?? p.gameObject.AddComponent<CursedPassiveRunner>();
        runner.SyncVisuals();
        return runner;
    }
}

/// <summary>
/// Chạy toàn bộ kỹ năng bị động nguyền rủa gắn trực tiếp lên nhân vật Player.
/// </summary>
public class CursedPassiveRunner : MonoBehaviour
{
    private static Sprite sCloud, sBolt, sOrb, sBlade, sFrost;
    private static bool spritesLoaded;

    // 1. Storm Cloud state
    private GameObject cloudObj;
    private SpriteRenderer cloudSr;
    private float nextLightningTime;
    private Vector3 cloudTargetPos;
    private float cloudStrikeHoldUntil;

    // 2. Hellfire Orbs state
    private readonly List<Transform> orbTransforms = new List<Transform>();
    private float orbAngle;
    private readonly Dictionary<int, float> orbHitCooldowns = new Dictionary<int, float>();

    // 3. Void Blades state
    private readonly List<Transform> hoverBlades = new List<Transform>();
    private float nextBladeVolleyTime;

    // 4. Frost Nova state
    private float nextFrostNovaTime;

    private static void LoadSprites()
    {
        if (spritesLoaded) return;
        spritesLoaded = true;
        sCloud = Resources.Load<Sprite>("CursedSkills/storm_cloud");
        sBolt = Resources.Load<Sprite>("CursedSkills/lightning_bolt");
        sOrb = Resources.Load<Sprite>("CursedSkills/fire_orb");
        sBlade = Resources.Load<Sprite>("CursedSkills/void_blade");
        sFrost = Resources.Load<Sprite>("CursedSkills/frost_shard");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSpriteCache()
    {
        spritesLoaded = false;
        sCloud = sBolt = sOrb = sBlade = sFrost = null;
    }

    private void Awake()
    {
        LoadSprites();
    }

    private void Start()
    {
        SyncVisuals();
    }

    public void SyncVisuals()
    {
        LoadSprites();

        // Sync Storm Cloud visual
        int cloudLv = CursedPassiveSystem.GetLevel(CursedSkillId.StormCloud);
        if (cloudLv > 0 && cloudObj == null)
        {
            cloudObj = new GameObject("CursedStormCloud");
            cloudObj.transform.position = transform.position + new Vector3(0f, 2.8f, 0f);
            cloudSr = cloudObj.AddComponent<SpriteRenderer>();
            cloudSr.sprite = sCloud ?? CombatVFX.GetCircleSprite();
            cloudSr.color = Color.white;
            CombatVFX.ApplyVfxLayer(cloudSr, 68);
            cloudObj.transform.localScale = Vector3.one * 0.95f;
        }
        else if (cloudLv == 0 && cloudObj != null)
        {
            Destroy(cloudObj);
            cloudObj = null;
        }

        // Sync Hellfire Orbs count (Lv1: 2, Lv2: 3, Lv3: 4)
        int orbLv = CursedPassiveSystem.GetLevel(CursedSkillId.HellfireOrbs);
        int desiredOrbs = orbLv > 0 ? (1 + orbLv) : 0;
        while (orbTransforms.Count < desiredOrbs)
        {
            var orb = new GameObject("CursedHellfireOrb_" + orbTransforms.Count);
            var sr = orb.AddComponent<SpriteRenderer>();
            sr.sprite = sOrb ?? CombatVFX.GetCircleSprite();
            sr.color = Color.white;
            CombatVFX.ApplyVfxLayer(sr, 65);
            orb.transform.localScale = Vector3.one * 0.72f;
            orbTransforms.Add(orb.transform);
        }
        while (orbTransforms.Count > desiredOrbs)
        {
            int last = orbTransforms.Count - 1;
            if (orbTransforms[last] != null) Destroy(orbTransforms[last].gameObject);
            orbTransforms.RemoveAt(last);
        }

        // Sync Hovering Void Blades behind shoulder (1..3 blades)
        int bladeLv = CursedPassiveSystem.GetLevel(CursedSkillId.VoidBlades);
        while (hoverBlades.Count < bladeLv)
        {
            var b = new GameObject("CursedHoverBlade_" + hoverBlades.Count);
            var sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = sBlade ?? CombatVFX.GetCircleSprite();
            sr.color = new Color(1f, 1f, 1f, 0.92f);
            CombatVFX.ApplyVfxLayer(sr, 64);
            b.transform.localScale = Vector3.one * 0.72f;
            hoverBlades.Add(b.transform);
        }
        while (hoverBlades.Count > bladeLv)
        {
            int last = hoverBlades.Count - 1;
            if (hoverBlades[last] != null) Destroy(hoverBlades[last].gameObject);
            hoverBlades.RemoveAt(last);
        }
    }

    private void OnDestroy()
    {
        if (cloudObj != null) Destroy(cloudObj);
        foreach (var t in orbTransforms) if (t != null) Destroy(t.gameObject);
        foreach (var t in hoverBlades) if (t != null) Destroy(t.gameObject);
    }

    private void Update()
    {
        int cloudLv = CursedPassiveSystem.GetLevel(CursedSkillId.StormCloud);
        if (cloudLv > 0) UpdateStormCloud(cloudLv);

        int orbLv = CursedPassiveSystem.GetLevel(CursedSkillId.HellfireOrbs);
        if (orbLv > 0) UpdateHellfireOrbs(orbLv);

        int bladeLv = CursedPassiveSystem.GetLevel(CursedSkillId.VoidBlades);
        if (bladeLv > 0) UpdateVoidBlades(bladeLv);

        int frostLv = CursedPassiveSystem.GetLevel(CursedSkillId.FrostNova);
        if (frostLv > 0) UpdateFrostNova(frostLv);
    }

    // ==================== 1. LÔI VÂN NGUYỀN RỦA (STORM CLOUD) ====================
    private void UpdateStormCloud(int lv)
    {
        if (cloudObj == null) { SyncVisuals(); if (cloudObj == null) return; }

        float bob = Mathf.Sin(Time.time * 3.2f) * 0.16f;
        if (Time.time >= cloudStrikeHoldUntil)
        {
            cloudTargetPos = transform.position + new Vector3(-0.35f, 2.85f + bob, 0f);
        }
        cloudObj.transform.position = Vector3.Lerp(cloudObj.transform.position, cloudTargetPos, Time.deltaTime * 7.5f);

        // Tia lửa điện nhỏ lách tách dưới đám mây
        if (UnityEngine.Random.value < 0.14f)
        {
            CombatVFX.SpawnBurst(cloudObj.transform.position + new Vector3(UnityEngine.Random.Range(-0.55f, 0.55f), -0.3f, 0f),
                new Color(0.35f, 0.92f, 1f, 0.9f), 1, 0.6f, 1.8f, 0.1f, 0.22f, 2.5f);
        }

        if (Time.time < nextLightningTime) return;

        var targets = FindNearestEnemies(transform.position, 9.2f, lv >= 3 ? 2 : 1);
        if (targets.Count == 0) return;

        float cd = Mathf.Max(1.1f, 2.2f - (lv - 1) * 0.4f) * ScrollUpgradeUI.TacticsCooldownMultiplier;
        nextLightningTime = Time.time + cd;

        float dmg = (3.0f + (lv - 1) * 1.5f) * ScrollUpgradeUI.TacticsSkillDamageMultiplier;
        var primary = targets[0];
        cloudTargetPos = primary.position + new Vector3(0f, 3.1f, 0f);
        cloudStrikeHoldUntil = Time.time + 0.55f;

        foreach (var target in targets)
        {
            StartCoroutine(StrikeLightningRoutine(target, dmg));
        }
    }

    private IEnumerator StrikeLightningRoutine(Transform target, float dmg)
    {
        if (cloudSr != null) cloudSr.color = new Color(0.75f, 0.98f, 1f, 1f);
        yield return new WaitForSeconds(0.08f);
        if (target == null)
        {
            if (cloudSr != null) cloudSr.color = Color.white;
            yield break;
        }

        Vector3 strikeTop = cloudObj != null ? cloudObj.transform.position + Vector3.down * 0.25f : target.position + Vector3.up * 3.2f;
        Vector3 strikeBottom = target.position + Vector3.up * 0.7f;
        SpawnLightningBoltVfx(strikeTop, strikeBottom);

        bool stun = UnityEngine.Random.value < 0.25f;
        DealSkillDamage(target.gameObject, dmg, strikeBottom, new Color(0.35f, 0.92f, 1f), isCrit: stun);
        if (stun)
        {
            var tg = target.GetComponent<EnemyTelegraph>();
            if (tg != null) tg.Stun(0.35f);
        }

        CombatVFX.SpawnShockwave(strikeBottom, new Color(0.3f, 0.92f, 1f, 0.95f), 0.22f, 2.1f, 0.22f);
        CombatVFX.SpawnBurst(strikeBottom, new Color(0.55f, 0.96f, 1f, 1f), 10, 2.2f, 6.2f, 0.14f, 0.3f, 3f);

        yield return new WaitForSeconds(0.12f);
        if (cloudSr != null) cloudSr.color = Color.white;
    }

    private void SpawnLightningBoltVfx(Vector3 top, Vector3 bottom)
    {
        var go = new GameObject("CursedLightningStrike");
        Vector3 mid = (top + bottom) * 0.5f;
        Vector3 delta = bottom - top;
        float dist = Mathf.Max(0.8f, delta.magnitude);
        go.transform.position = mid;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 90f;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sBolt ?? CombatVFX.GetCircleSprite();
        sr.color = Color.white;
        CombatVFX.ApplyVfxLayer(sr, 75);

        float spriteH = sr.sprite.bounds.size.y;
        float scaleY = spriteH > 0.01f ? dist / spriteH : dist;
        go.transform.localScale = new Vector3(1.1f, scaleY, 1f);

        StartCoroutine(FadeAndDestroy(go, sr, 0.2f));
    }

    // ==================== 2. HỎA CẦU ĐỊA NGỤC (HELLFIRE ORBS) ====================
    private void UpdateHellfireOrbs(int lv)
    {
        if (orbTransforms.Count == 0) { SyncVisuals(); if (orbTransforms.Count == 0) return; }

        float speed = 175f + (lv - 1) * 45f;
        orbAngle += speed * Time.deltaTime;
        float radius = 2.15f;
        float dmg = (2.2f + (lv - 1) * 1.0f) * ScrollUpgradeUI.TacticsSkillDamageMultiplier;

        var enemies = FindNearestEnemies(transform.position, radius + 1.6f, 8);

        for (int i = 0; i < orbTransforms.Count; i++)
        {
            var orb = orbTransforms[i];
            if (orb == null) continue;

            float rad = (orbAngle + i * (360f / orbTransforms.Count)) * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius * 0.72f + 0.85f, 0f);
            orb.position = pos;
            orb.Rotate(0f, 0f, -320f * Time.deltaTime);

            if (UnityEngine.Random.value < 0.22f)
            {
                CombatVFX.SpawnBurst(pos, new Color(1f, 0.45f, 0.1f, 0.85f), 1, 0.4f, 1.3f, 0.1f, 0.2f, -0.8f);
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                Vector3 eCenter = enemy.position + Vector3.up * 0.8f;
                if (Vector2.Distance(pos, eCenter) <= 1.05f)
                {
                    int eid = enemy.gameObject.GetInstanceID();
                    if (!orbHitCooldowns.TryGetValue(eid, out float readyAt) || Time.time >= readyAt)
                    {
                        orbHitCooldowns[eid] = Time.time + 0.52f;
                        DealSkillDamage(enemy.gameObject, dmg, eCenter, new Color(1f, 0.46f, 0.12f));
                        StatusEffectController.GetOrAdd(enemy.gameObject)?.Apply(StatusType.Burn, 3.0f, 0.75f);
                        CombatVFX.SpawnBurst(eCenter, new Color(1f, 0.5f, 0.12f, 1f), 8, 2.0f, 5.5f, 0.14f, 0.28f, 2f);
                    }
                }
            }
        }
    }

    // ==================== 3. HUYẾT KIẾM HƯ KHÔNG (VOID BLADES) ====================
    private void UpdateVoidBlades(int lv)
    {
        if (hoverBlades.Count == 0) { SyncVisuals(); if (hoverBlades.Count == 0) return; }

        var nearestList = FindNearestEnemies(transform.position, 10.5f, 1);
        Transform aimTarget = nearestList.Count > 0 ? nearestList[0] : null;

        for (int i = 0; i < hoverBlades.Count; i++)
        {
            var b = hoverBlades[i];
            if (b == null) continue;
            float offsetX = -0.85f - i * 0.35f;
            float offsetY = 1.45f + (i - (hoverBlades.Count - 1) * 0.5f) * 0.42f + Mathf.Sin(Time.time * 4f + i) * 0.12f;
            Vector3 desired = transform.position + new Vector3(offsetX, offsetY, 0f);
            b.position = Vector3.Lerp(b.position, desired, Time.deltaTime * 10f);

            if (aimTarget != null)
            {
                Vector2 dir = (aimTarget.position + Vector3.up * 0.8f - b.position).normalized;
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                b.rotation = Quaternion.Lerp(b.rotation, Quaternion.Euler(0f, 0f, ang), Time.deltaTime * 14f);
            }
            else
            {
                b.rotation = Quaternion.Lerp(b.rotation, Quaternion.identity, Time.deltaTime * 8f);
            }
        }

        if (aimTarget == null || Time.time < nextBladeVolleyTime) return;

        float cd = Mathf.Max(1.2f, 2.3f - (lv - 1) * 0.38f) * ScrollUpgradeUI.TacticsCooldownMultiplier;
        nextBladeVolleyTime = Time.time + cd;

        float dmg = (3.2f + (lv - 1) * 1.4f) * ScrollUpgradeUI.TacticsSkillDamageMultiplier;
        StartCoroutine(LaunchVoidBladesRoutine(lv, dmg));
    }

    private IEnumerator LaunchVoidBladesRoutine(int count, float dmg)
    {
        for (int i = 0; i < count; i++)
        {
            var targets = FindNearestEnemies(transform.position, 11f, 1);
            if (targets.Count == 0) yield break;

            Vector3 spawnPos = (i < hoverBlades.Count && hoverBlades[i] != null)
                ? hoverBlades[i].position
                : transform.position + new Vector3(0f, 1.5f, 0f);

            var proj = new GameObject("VoidBladeProjectile");
            proj.transform.position = spawnPos;
            var sr = proj.AddComponent<SpriteRenderer>();
            sr.sprite = sBlade ?? CombatVFX.GetCircleSprite();
            sr.color = Color.white;
            CombatVFX.ApplyVfxLayer(sr, 72);
            proj.transform.localScale = Vector3.one * 0.85f;

            var runner = proj.AddComponent<CursedBladeProjectile>();
            runner.Init(targets[0], dmg, sr);

            yield return new WaitForSeconds(0.14f);
        }
    }

    // ==================== 4. BĂNG HỒN BỘC PHÁ (FROST NOVA) ====================
    private void UpdateFrostNova(int lv)
    {
        if (Time.time < nextFrostNovaTime) return;

        var nearby = FindNearestEnemies(transform.position, 6.2f, 12);
        if (nearby.Count == 0) return;

        float cd = Mathf.Max(2.1f, 4.0f - (lv - 1) * 0.65f) * ScrollUpgradeUI.TacticsCooldownMultiplier;
        nextFrostNovaTime = Time.time + cd;

        float dmg = (2.4f + (lv - 1) * 1.1f) * ScrollUpgradeUI.TacticsSkillDamageMultiplier;
        Vector3 center = transform.position + Vector3.up * 0.8f;

        CombatVFX.SpawnShockwave(center, new Color(0.35f, 0.92f, 1f, 0.95f), 0.35f, 5.2f, 0.38f);
        CombatVFX.SpawnBurst(center, new Color(0.55f, 0.96f, 1f, 1f), 16, 2.5f, 7.5f, 0.2f, 0.42f, 1.5f);

        // Phóng vòng mảnh băng tinh thể xoay tròn tỏa ra xung quanh
        int shards = 6 + lv * 2;
        for (int i = 0; i < shards; i++)
        {
            float ang = i * (360f / shards) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            var shard = new GameObject("FrostShardVfx");
            shard.transform.position = center;
            var sr = shard.AddComponent<SpriteRenderer>();
            sr.sprite = sFrost ?? CombatVFX.GetCircleSprite();
            sr.color = Color.white;
            CombatVFX.ApplyVfxLayer(sr, 70);
            shard.transform.localScale = Vector3.one * 0.62f;
            StartCoroutine(MoveFrostShardRoutine(shard, sr, dir, 7.2f, 0.42f));
        }

        // Gây sát thương băng & làm chậm 45% toàn bộ quái trong bán kính
        foreach (var enemy in nearby)
        {
            if (enemy == null) continue;
            Vector3 ePos = enemy.position + Vector3.up * 0.8f;
            DealSkillDamage(enemy.gameObject, dmg, ePos, new Color(0.4f, 0.95f, 1f));
            StatusEffectController.GetOrAdd(enemy.gameObject)?.Apply(StatusType.Frost, 2.8f + (lv - 1) * 0.6f);
        }
    }

    private IEnumerator MoveFrostShardRoutine(GameObject go, SpriteRenderer sr, Vector2 dir, float speed, float life)
    {
        float t = 0f;
        while (t < life && go != null)
        {
            t += Time.deltaTime;
            go.transform.position += (Vector3)(dir * speed * Time.deltaTime);
            go.transform.Rotate(0f, 0f, 420f * Time.deltaTime);
            if (sr != null)
            {
                var c = sr.color;
                c.a = 1f - (t / life);
                sr.color = c;
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private IEnumerator FadeAndDestroy(GameObject go, SpriteRenderer sr, float duration)
    {
        float t = 0f;
        while (t < duration && go != null)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                float flicker = (Mathf.FloorToInt(t * 45f) % 2 == 0) ? 1f : 0.65f;
                sr.color = new Color(1f, 1f, 1f, (1f - t / duration) * flicker);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ==================== HELPERS ====================
    public static List<Transform> FindNearestEnemies(Vector3 origin, float radius, int maxCount)
    {
        var candidates = new List<(Transform tf, float dist)>();
        float sqrR = radius * radius;

        foreach (var pe in FindObjectsByType<PatrolEnemy1>(FindObjectsSortMode.None))
        {
            if (pe == null || pe.IsDead) continue;
            float d2 = (pe.transform.position - origin).sqrMagnitude;
            if (d2 <= sqrR) candidates.Add((pe.transform, d2));
        }
        foreach (var pb in FindObjectsByType<PatrolBoss>(FindObjectsSortMode.None))
        {
            if (pb == null || !pb.IsAlive) continue;
            float d2 = (pb.transform.position - origin).sqrMagnitude;
            if (d2 <= sqrR) candidates.Add((pb.transform, d2));
        }
        foreach (var hp in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (hp == null || hp.IsDead) continue;
            if (hp.GetComponent<Player>() != null || hp.GetComponent<PatrolEnemy1>() != null || hp.GetComponent<PatrolBoss>() != null) continue;
            float d2 = (hp.transform.position - origin).sqrMagnitude;
            if (d2 <= sqrR) candidates.Add((hp.transform, d2));
        }

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
        var result = new List<Transform>(Mathf.Min(maxCount, candidates.Count));
        for (int i = 0; i < candidates.Count && i < maxCount; i++)
            result.Add(candidates[i].tf);
        return result;
    }

    public static void DealSkillDamage(GameObject target, float damage, Vector3 hitPos, Color popupColor, bool isCrit = false)
    {
        if (target == null) return;
        float finalDmg = isCrit ? damage * 1.5f : damage;

        var pe = target.GetComponent<PatrolEnemy1>();
        if (pe != null && !pe.IsDead)
        {
            pe.takeDamageE(finalDmg, isCrit, hitPos, bypassShield: true);
            return;
        }

        var pb = target.GetComponent<PatrolBoss>();
        if (pb != null && pb.IsAlive)
        {
            pb.takeDamageE(finalDmg, isCrit);
            return;
        }

        var hp = target.GetComponent<Health>();
        if (hp != null && !hp.IsDead)
        {
            hp.TakeDamage(finalDmg, hitPos);
            DamagePopup.Spawn(hitPos, finalDmg, popupColor, isCrit);
        }
    }
}

/// <summary>Đường bay truy đuổi của Huyết Kiếm Hư Không.</summary>
public class CursedBladeProjectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    private SpriteRenderer sr;
    private Vector2 dir = Vector2.right;
    private float life = 1.25f;
    private bool hitDone;

    public void Init(Transform targetEnemy, float dmg, SpriteRenderer renderer)
    {
        target = targetEnemy;
        damage = dmg;
        sr = renderer;
        if (target != null)
        {
            dir = ((Vector2)(target.position + Vector3.up * 0.8f - transform.position)).normalized;
        }
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        if (target != null && !hitDone)
        {
            Vector2 desired = ((Vector2)(target.position + Vector3.up * 0.8f - transform.position)).normalized;
            dir = Vector2.Lerp(dir, desired, Time.deltaTime * 12f).normalized;
        }

        transform.position += (Vector3)(dir * 16.5f * Time.deltaTime);
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);

        if (sr != null && UnityEngine.Random.value < 0.45f)
        {
            CombatVFX.SpawnAfterimage(sr, new Color(0.75f, 0.25f, 1f, 0.5f), 0.14f);
        }

        if (!hitDone && target != null && Vector2.Distance(transform.position, target.position + Vector3.up * 0.8f) <= 1.05f)
        {
            hitDone = true;
            bool crit = UnityEngine.Random.value < 0.35f;
            Vector3 hitPos = target.position + Vector3.up * 0.8f;
            CursedPassiveRunner.DealSkillDamage(target.gameObject, damage, hitPos, new Color(0.8f, 0.35f, 1f), isCrit: crit);
            CombatVFX.SpawnShockwave(hitPos, new Color(0.78f, 0.3f, 1f, 0.95f), 0.2f, 1.8f, 0.2f);
            CombatVFX.SpawnBurst(hitPos, new Color(0.82f, 0.35f, 1f, 1f), 10, 2.2f, 6.2f, 0.14f, 0.28f, 2f);
            Destroy(gameObject);
        }
    }
}
