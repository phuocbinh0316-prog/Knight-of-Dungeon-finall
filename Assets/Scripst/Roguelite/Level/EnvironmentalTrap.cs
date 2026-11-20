using UnityEngine;

public enum TrapType
{
    RetractingSpikes, // Chông sắt trồi thụt theo nhịp có cảnh báo
    SpinningSaw,      // Lưỡi cưa xoay qua lại trên ray
    AcidPool          // Vũng axit độc gây sát thương + Poison
}

/// <summary>
/// Hệ thống Bẫy Môi Trường (Environmental Hazards) chuẩn Dead Cells:
/// Kéo thả vào bất kỳ object nào trong scene (chỉ cần Collider2D IsTrigger = true).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnvironmentalTrap : MonoBehaviour
{
    [Header("Trap Config")]
    [SerializeField] private TrapType trapType = TrapType.RetractingSpikes;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float damageInterval = 0.75f;

    [Header("Retracting Spikes")]
    [SerializeField] private float activeDuration = 1.4f;
    [SerializeField] private float inactiveDuration = 2.0f;
    [SerializeField] private float telegraphDuration = 0.45f;
    [SerializeField] private float spikePopHeight = 0.55f;

    [Header("Spinning Saw")]
    [SerializeField] private float sawMoveDistance = 3.5f;
    [SerializeField] private float sawMoveSpeed = 3.2f;
    [SerializeField] private float sawSpinSpeed = 540f;

    private Vector3 startPos;
    private float cycleTimer;
    private float nextDamageTime;
    private bool spikesExtended;
    private SpriteRenderer sr;
    private Color baseColor = Color.white;

    private void Awake()
    {
        startPos = transform.position;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    private void Update()
    {
        switch (trapType)
        {
            case TrapType.RetractingSpikes:
                UpdateSpikes();
                break;
            case TrapType.SpinningSaw:
                UpdateSaw();
                break;
            case TrapType.AcidPool:
                if (Random.value < 0.08f)
                {
                    CombatVFX.SpawnBurst(transform.position + new Vector3(Random.Range(-0.8f, 0.8f), 0.1f, 0f),
                        new Color(0.3f, 0.95f, 0.4f, 0.8f), 1, 0.4f, 1.4f, 0.15f, 0.35f, -1.5f);
                }
                break;
        }
    }

    private void UpdateSpikes()
    {
        float total = inactiveDuration + telegraphDuration + activeDuration;
        cycleTimer = (cycleTimer + Time.deltaTime) % total;

        if (cycleTimer < inactiveDuration)
        {
            spikesExtended = false;
            transform.position = Vector3.Lerp(transform.position, startPos - Vector3.up * spikePopHeight, Time.deltaTime * 14f);
            if (sr != null) sr.color = baseColor;
        }
        else if (cycleTimer < inactiveDuration + telegraphDuration)
        {
            // Cảnh báo rung nhẹ + nháy đỏ trước khi chông trồi lên
            spikesExtended = false;
            float shake = Mathf.Sin(Time.time * 60f) * 0.04f;
            transform.position = startPos - Vector3.up * (spikePopHeight * 0.72f) + Vector3.right * shake;
            if (sr != null) sr.color = Color.Lerp(baseColor, new Color(1f, 0.3f, 0.3f), 0.7f);
        }
        else
        {
            spikesExtended = true;
            transform.position = Vector3.Lerp(transform.position, startPos, Time.deltaTime * 22f);
            if (sr != null) sr.color = baseColor;
        }
    }

    private void UpdateSaw()
    {
        float offset = Mathf.PingPong(Time.time * sawMoveSpeed, sawMoveDistance) - sawMoveDistance * 0.5f;
        transform.position = startPos + Vector3.right * offset;
        transform.Rotate(0f, 0f, -sawSpinSpeed * Time.deltaTime);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextDamageTime) return;
        if (trapType == TrapType.RetractingSpikes && !spikesExtended) return;

        var player = other.GetComponentInParent<Player>();
        if (player != null)
        {
            nextDamageTime = Time.time + damageInterval;
            player.TakeDamageP(damage, transform.position, gameObject);

            var status = StatusEffectController.GetOrAdd(player.gameObject);
            if (status != null)
            {
                if (trapType == TrapType.AcidPool) status.Apply(StatusType.Poison, 3.0f, 0.45f);
                else if (trapType == TrapType.SpinningSaw) status.Apply(StatusType.Bleed, 2.5f, 0.4f);
            }
            return;
        }

        // Bẫy cũng có thể gây sát thương cho quái nếu người chơi dụ quái vào bẫy!
        var e1 = other.GetComponentInParent<PatrolEnemy1>();
        if (e1 != null)
        {
            nextDamageTime = Time.time + damageInterval;
            e1.takeDamageE(damage * 1.5f, false, transform.position, bypassShield: true);
            if (trapType == TrapType.AcidPool)
            {
                var st = StatusEffectController.GetOrAdd(e1.gameObject);
                if (st != null) st.Apply(StatusType.Poison, 3.5f, 0.65f);
            }
        }
    }
}
