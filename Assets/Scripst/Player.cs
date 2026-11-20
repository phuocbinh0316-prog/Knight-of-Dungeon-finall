using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
using Unity.VisualScripting;

public class Player : MonoBehaviour
{
    [Header("Run SFX")]
    [SerializeField] private float runSfxInterval = 0.35f; // nhịp bước chân
    [SerializeField] private float runSfxVolume = 0.6f;    // nhỏ hơn jump/attack
    private float runSfxTimer = 0f;
    private GameManager gameManager;
    private Rigidbody2D rb;
    private Animator animator;
    private float moveMent;
    [SerializeField] private float moveSpeed = 10f;
    private bool facingRight = true;
    [SerializeField] private float jumpHight = 10f;
    private bool isGround = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float distancePoint = 1f;
    private bool jumpPressed = false;
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float attackCooldown = 0.5f;
    private float nextAttackTime = 0f;
    private float isAttackingTimer = 0f;
    private bool canDealDamage = false;

    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] LayerMask LayerPlayer;
    [SerializeField] float damageP = 1f;
    private float currentHp;
    [SerializeField] private Image hpBar;
    private Vector3 startPos;

    [Header("Pro Movement Feel (Dead Cells)")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float variableJumpCut = 0.52f;
    [SerializeField] private float wallCheckDistance = 0.62f;
    [SerializeField] private float wallSlideMaxFallSpeed = 2.4f;
    [SerializeField] private float wallJumpHorizontalForce = 10.5f;

    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;
    private bool wasGrounded = true;
    private bool isWallSliding = false;
    private int wallDirection = 0; // 1 = tường bên phải, -1 = tường bên trái
    private float wallJumpLockTimer = 0f;
    private float wallSlideDustTimer = 0f;

    private DashRoll dash;
    private WeaponHolder weapons;
    private GroundSlam groundSlam;
    private ParryShield parryShield;
    private RallyAndFlask rallyAndFlask;
    private ActiveSkills activeSkills;
    private Collider2D[] ownColliders;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private const float ComboResetTime = 0.85f;

    public float GetBaseDamage() => (weapons != null ? weapons.Damage : damageP);
    public bool IsFacingRight => facingRight;
    public bool IsGround => isGround;
    public bool IsWallSliding => isWallSliding;
    public float CurrentHp => currentHp;
    public float MaxHealth => maxHealth;
    public int ComboStep => comboStep;

    private int jumpCount = 0;
    private bool doubleJumpPressed = false;
    private bool wallJumpPressed = false;
    private int wallJumpKickDir = 0;

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();

        // Tự động gắn đủ bộ cơ chế Dead Cells chuyên nghiệp (không làm hỏng prefab cũ)
        dash = GetComponent<DashRoll>() ?? gameObject.AddComponent<DashRoll>();
        weapons = GetComponent<WeaponHolder>();
        groundSlam = GetComponent<GroundSlam>() ?? gameObject.AddComponent<GroundSlam>();
        parryShield = GetComponent<ParryShield>() ?? gameObject.AddComponent<ParryShield>();
        rallyAndFlask = GetComponent<RallyAndFlask>() ?? gameObject.AddComponent<RallyAndFlask>();
        activeSkills = GetComponent<ActiveSkills>() ?? gameObject.AddComponent<ActiveSkills>();

        if (GetComponent<CameraLookAhead>() == null) gameObject.AddComponent<CameraLookAhead>();
    }

    void Start()
    {
        maxHealth += MetaUpgrades.BonusHealth + ScrollUpgradeUI.SurvivalBonusHealth;
        currentHp = maxHealth;
        startPos = transform.position;

        // Khởi tạo hệ thống Pro HUD & KillStreak tự động
        ProCombatHUD.EnsureExists(this);
        KillStreakManager.Get();
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHealth, currentHp + amount);
    }

    public void IncreaseMaxHealth(float extraHp, bool fullHeal = false)
    {
        maxHealth += extraHp;
        currentHp = fullHeal ? maxHealth : Mathf.Min(maxHealth, currentHp + extraHp);
    }

    public void NotifyDamageDealt(float damageDealt)
    {
        if (rallyAndFlask != null) rallyAndFlask.OnPlayerDealtDamage(damageDealt);
        KillStreakManager.Get().RegisterDamageDealt(damageDealt);
    }

    public void SetSpawn(Vector3 pos)
    {
        transform.position = pos;
        startPos = pos;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        UpdateHpBar();
        Movement();
        CheckWallSlide();
        jump();
        Animation();
        HandleRunSFX();
    }

    private void FixedUpdate()
    {
        if (jumpPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(new Vector2(0f, jumpHight), ForceMode2D.Impulse);
            jumpPressed = false;
        }
        else if (wallJumpPressed)
        {
            rb.linearVelocity = new Vector2(wallJumpKickDir * wallJumpHorizontalForce, 0f);
            rb.AddForce(new Vector2(0f, jumpHight * 0.95f), ForceMode2D.Impulse);
            wallJumpPressed = false;
        }
        else if (doubleJumpPressed)
        {
            // Triệt tiêu vận tốc rơi trước khi bật nhảy lần 2 để cú nhảy đúp mượt mà và nảy bốc
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(new Vector2(0f, jumpHight * 0.95f), ForceMode2D.Impulse);
            doubleJumpPressed = false;
        }

        // Hãm tốc độ rơi khi đang bám tường trượt (Wall Slide)
        if (isWallSliding && rb.linearVelocity.y < -wallSlideMaxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideMaxFallSpeed);
        }
    }

    private void Movement()
    {
        if (dash != null && dash.IsDashing) { moveMent = 0f; return; }
        if (groundSlam != null && groundSlam.IsSlamming) { moveMent = 0f; return; }

        if (wallJumpLockTimer > 0f)
        {
            wallJumpLockTimer -= Time.deltaTime;
            return;
        }

        moveMent = Input.GetAxis("Horizontal");
        transform.position += new Vector3(moveMent, 0f, 0f) * moveSpeed * Time.deltaTime;
        if (moveMent < 0f && facingRight)
        {
            transform.eulerAngles = new Vector3(0f, -180f, 0f);
            facingRight = false;
        }
        else if (moveMent > 0f && facingRight == false)
        {
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
            facingRight = true;
        }
    }

    private void CheckWallSlide()
    {
        isWallSliding = false;
        wallDirection = 0;

        if (isGround || (dash != null && dash.IsDashing) || (groundSlam != null && groundSlam.IsSlamming)) return;
        if (rb == null || rb.linearVelocity.y >= 0.1f) return;

        // Quét tường hai bên ở độ cao thân nhân vật
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.15f;
        bool wallRight = IsSolidWall(Physics2D.RaycastAll(origin, Vector2.right, wallCheckDistance, groundLayer));
        bool wallLeft = IsSolidWall(Physics2D.RaycastAll(origin, Vector2.left, wallCheckDistance, groundLayer));

        if (wallRight && moveMent > 0.1f)
        {
            isWallSliding = true;
            wallDirection = 1;
        }
        else if (wallLeft && moveMent < -0.1f)
        {
            isWallSliding = true;
            wallDirection = -1;
        }

        if (isWallSliding)
        {
            wallSlideDustTimer -= Time.deltaTime;
            if (wallSlideDustTimer <= 0f)
            {
                wallSlideDustTimer = 0.14f;
                Vector3 wallPos = transform.position + new Vector3(wallDirection * 0.45f, 0.1f, 0f);
                CombatVFX.SpawnBurst(wallPos, new Color(0.85f, 0.85f, 0.9f, 0.65f), 2, 0.5f, 1.5f, 0.14f, 0.22f, 2f);
            }
        }
    }

    private bool IsSolidWall(RaycastHit2D[] hits)
    {
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.gameObject == gameObject || h.collider.transform.IsChildOf(transform)) continue;
            // Không bám vào bệ nhảy một chiều mỏng
            if (h.collider.GetComponent<PlatformEffector2D>() != null) continue;
            return true;
        }
        return false;
    }

    private void jump()
    {
        if (groundSlam != null && groundSlam.IsSlamming) return;

        bool prevGround = isGround;
        isGround = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, 0.22f, groundLayer);

        // Hiệu ứng bụi khi vừa tiếp đất
        if (!prevGround && isGround && groundCheck != null)
        {
            CombatVFX.SpawnFootDust(groundCheck.position, 7, false);
        }
        wasGrounded = isGround;

        // Coyote Time: vừa rời mép vực vẫn nhảy được thêm 0.12s
        if (isGround)
        {
            coyoteTimer = coyoteTime;
            jumpCount = 0;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // Nhấn [S + Space] khi đang ĐỨNG TRÊN BỆ MỎNG -> Nhảy xuyên xuống dưới (Drop-Through Platform)
        bool downHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetAxisRaw("Vertical") < -0.5f;
        if (isGround && downHeld && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)))
        {
            if (TryDropThroughPlatform())
            {
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                return;
            }
        }

        // Jump Buffering: bấm Space sớm trước khi chạm đất vẫn ăn phím ngay khi đáp
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // Ưu tiên 1: Bật tường (Wall Jump) khi đang trượt tường
        if (jumpBufferTimer > 0f && isWallSliding)
        {
            wallJumpPressed = true;
            wallJumpKickDir = -wallDirection;
            wallJumpLockTimer = 0.14f;
            jumpBufferTimer = 0f;
            isWallSliding = false;
            jumpCount = 1; // Cho phép Double Jump sau khi Wall Jump!

            // Xoay mặt sang hướng bật tường
            facingRight = wallJumpKickDir > 0;
            transform.eulerAngles = new Vector3(0f, facingRight ? 0f : -180f, 0f);

            animator.ResetTrigger("Attack1");
            animator.SetBool("Jump", true);
            CombatVFX.SpawnFootDust(transform.position + new Vector3(wallDirection * 0.4f, 0f, 0f), 6, true);
            if (AudioManager.Instance != null) AudioManager.Instance.SFX_Jump();
            return;
        }

        // Ưu tiên 2: Nhảy từ mặt đất / Coyote Time
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && !downHeld)
        {
            jumpPressed = true;
            jumpCount = 1;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            animator.ResetTrigger("Attack1");
            if (groundCheck != null) CombatVFX.SpawnFootDust(groundCheck.position, 5, false);
            if (AudioManager.Instance != null) AudioManager.Instance.SFX_Jump();
        }
        // Ưu tiên 3: Nhảy đúp trên không (Double Jump)
        else if (Input.GetButtonDown("Jump") && !isGround && !downHeld && jumpCount <= 1)
        {
            doubleJumpPressed = true;
            jumpCount = 2;
            jumpBufferTimer = 0f;
            animator.ResetTrigger("Attack1");
            animator.SetBool("Jump", true);
            CombatVFX.SpawnFootDust(groundCheck != null ? groundCheck.position : transform.position, 8, isDoubleJump: true);
            if (AudioManager.Instance != null) AudioManager.Instance.SFX_Jump();
        }

        // Variable Jump Height: Nhả phím Space sớm khi đang bay lên -> cắt bớt lực nhảy để nhảy thấp linh hoạt
        if (Input.GetButtonUp("Jump") && rb != null && rb.linearVelocity.y > 0.5f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * variableJumpCut);
        }
    }

    private bool TryDropThroughPlatform()
    {
        if (groundCheck == null || ownColliders == null) return false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, 0.35f, groundLayer);
        var platforms = new List<Collider2D>();

        foreach (var h in hits)
        {
            if (h != null && h.GetComponent<PlatformEffector2D>() != null)
            {
                platforms.Add(h);
            }
        }

        if (platforms.Count == 0) return false;
        StartCoroutine(DropThroughRoutine(platforms));
        return true;
    }

    private IEnumerator DropThroughRoutine(List<Collider2D> platforms)
    {
        foreach (var plat in platforms)
        {
            if (plat == null) continue;
            foreach (var pCol in ownColliders)
            {
                if (pCol != null && !pCol.isTrigger) Physics2D.IgnoreCollision(pCol, plat, true);
            }
        }
        if (rb != null) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -4f);

        yield return new WaitForSeconds(0.28f);

        foreach (var plat in platforms)
        {
            if (plat == null) continue;
            foreach (var pCol in ownColliders)
            {
                if (pCol != null) Physics2D.IgnoreCollision(pCol, plat, false);
            }
        }
    }

    private void Animation()
    {
        if (isAttackingTimer > 0f)
        {
            isAttackingTimer -= Time.deltaTime;
        }

        bool isJumping = !isGround;

        if (isAttackingTimer > 0f)
        {
            animator.SetBool("Jump", false);
            animator.SetFloat("Run", 0f);
        }
        else
        {
            animator.SetBool("Jump", isJumping);
            if (Mathf.Abs(moveMent) > 0.1f)
            {
                animator.SetFloat("Run", 1f);
            }
            else
            {
                animator.SetFloat("Run", 0f);
            }
        }

        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + (weapons != null ? weapons.Cooldown : attackCooldown);
            isAttackingTimer = 0.35f;
            canDealDamage = true;

            // Xử lý chuỗi combo 3 đòn kiểu Dead Cells
            if (Time.time - lastAttackTime > ComboResetTime)
            {
                comboStep = 1;
            }
            else
            {
                comboStep = (comboStep % 3) + 1;
            }
            lastAttackTime = Time.time;

            // Đòn combo 2 & 3 hơi bước tới trước nhẹ tạo lực chém
            if (comboStep == 2 || comboStep == 3)
            {
                transform.position += (facingRight ? Vector3.right : Vector3.left) * (comboStep == 3 ? 0.32f : 0.22f);
            }

            // Vệt chém lưỡi liềm VFX ngay tại attackPoint
            Vector3 slashPos = attackPoint != null ? attackPoint.position : transform.position + (facingRight ? Vector3.right : Vector3.left) * 0.9f;
            CombatVFX.SpawnSlashArc(slashPos, facingRight, comboStep, comboStep == 3);

            animator.ResetTrigger("Attack1");
            animator.SetTrigger("Attack1");
            if (AudioManager.Instance != null) AudioManager.Instance.SFX_Attack();
            Attack();
        }
    }

    private void HandleRunSFX()
    {
        bool isRunning = Mathf.Abs(moveMent) > 0.1f && isGround;

        if (!isRunning)
        {
            runSfxTimer = 0f;
            return;
        }

        runSfxTimer -= Time.deltaTime;
        if (runSfxTimer <= 0f)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.run, runSfxVolume);
            runSfxTimer = runSfxInterval;
        }
    }

    private void Attack()
    {
        if (!canDealDamage) return;
        canDealDamage = false;

        float baseDmg = (weapons != null ? weapons.Damage : damageP) * MetaUpgrades.DamageMultiplier * ScrollUpgradeUI.BrutalityDamageMultiplier;
        float radius = weapons != null ? weapons.Range : attackRadius;

        Vector2 origin = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, LayerPlayer);
        var done = new HashSet<GameObject>();

        foreach (var h in hits)
        {
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (root == gameObject || !done.Add(root)) continue;

            bool isBackstab = false;
            bool isStunned = false;
            bool hasSynergy = false;

            var telegraph = root.GetComponent<EnemyTelegraph>();
            if (telegraph != null) isStunned = telegraph.IsStunned;

            var status = root.GetComponent<StatusEffectController>();
            if (status != null && status.HasAnyAffliction) hasSynergy = true;

            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null)
            {
                // Đánh lén sau lưng khi Player và Quái cùng nhìn về một hướng
                isBackstab = (facingRight == e1.IsFacingRight);
            }

            // Điều kiện chí mạng: Đòn Finisher thứ 3 HOẶC đánh sau lưng HOẶC đánh quái đang bị Stun
            bool isCrit = (comboStep == 3) || isBackstab || isStunned;
            float finalDmg = baseDmg * (isCrit ? 1.75f : 1f);

            // Dead Cells Synergy: +40% sát thương khi đánh mục tiêu đang dính Bleed / Burn / Poison / Frost
            if (hasSynergy)
            {
                finalDmg *= 1.4f;
                DamagePopup.SpawnText(root.transform.position + Vector3.up * 1.15f, "SYNERGY!", new Color(1f, 0.55f, 0.95f), 5.6f);
            }

            if (comboStep == 3)
            {
                CameraShake.Shake(0.24f);
            }

            // Tia lửa bắn ra khi đánh trúng
            CombatVFX.SpawnBurst(root.transform.position, isCrit ? new Color(1f, 0.85f, 0.15f, 1f) : new Color(0.95f, 0.3f, 0.25f, 0.9f), isCrit ? 10 : 5, 2.5f, 6.5f, 0.16f, 0.28f, 6f);

            if (e1 != null)
            {
                bool landed = e1.takeDamageE(finalDmg, isCrit, transform.position, bypassShield: false);
                if (landed)
                {
                    if (comboStep == 3)
                    {
                        var st = StatusEffectController.GetOrAdd(root);
                        if (st != null) st.Apply(StatusType.Bleed, 3.0f, 0.55f);
                    }
                    NotifyDamageDealt(finalDmg);
                }
                continue;
            }

            var boss = root.GetComponent<PatrolBoss>();
            if (boss != null)
            {
                boss.takeDamageE(finalDmg, isCrit);
                if (comboStep == 3)
                {
                    var st = StatusEffectController.GetOrAdd(root);
                    if (st != null) st.Apply(StatusType.Bleed, 3.0f, 0.65f);
                }
                NotifyDamageDealt(finalDmg);
                continue;
            }

            var d = root.GetComponent<IDamageable>();
            if (d != null)
            {
                d.TakeDamage(finalDmg, root.transform.position);
                DamagePopup.Spawn(root.transform.position, finalDmg, Color.white, isCrit);
                if (comboStep == 3)
                {
                    var st = StatusEffectController.GetOrAdd(root);
                    if (st != null) st.Apply(StatusType.Bleed, 3.0f, 0.55f);
                }
                NotifyDamageDealt(finalDmg);
            }
        }
    }

    public void TakeDamageP(float damage)
    {
        TakeDamageP(damage, transform.position, null);
    }

    public void TakeDamageP(float damage, Vector2 attackerPosition, GameObject attacker)
    {
        if (dash != null && dash.IsInvulnerable) return;
        if (groundSlam != null && groundSlam.IsSlamming) return;

        // Kiểm tra Parry trước khi nhận sát thương!
        if (parryShield != null && parryShield.TryInterceptDamage(damage, attacker))
        {
            return;
        }

        GameFeel.OnHit(transform.position, damage, gameObject);
        animator.SetTrigger("Hurt");
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Hit();

        float prevHp = currentHp;
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        float actualLost = prevHp - currentHp;
        if (actualLost > 0f)
        {
            if (rallyAndFlask != null) rallyAndFlask.OnPlayerDamaged(actualLost);
            KillStreakManager.Get().OnPlayerHit();
        }

        if (currentHp <= 0) Die();
    }

    private void Die()
    {
        if (gameManager != null) gameManager.GameOver();
    }

    private void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = maxHealth > 0f ? currentHp / maxHealth : 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SFX_TakeCoin();
            Destroy(collision.gameObject);
            if (gameManager != null) gameManager.AddScore(1);
        }
        if (collision.CompareTag("Key"))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SFX_Win();
            Destroy(collision.gameObject);
            if (gameManager != null) gameManager.GameWin();
        }
        if (collision.CompareTag("checkroi"))
        {
            animator.SetTrigger("Hurt");
            currentHp -= 10f;
            currentHp = Mathf.Max(currentHp, 0);
            KillStreakManager.Get().OnPlayerHit();
            if (currentHp <= 0) Die();
            transform.position = startPos;
        }
        if (collision.CompareTag("File"))
        {
            currentHp += 10f;
            if (currentHp > maxHealth) currentHp = maxHealth;
            Destroy(collision.gameObject);
        }
    }
}
