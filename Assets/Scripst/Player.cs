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
    private DashRoll dash;
    private WeaponHolder weapons;
    private GroundSlam groundSlam;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private const float ComboResetTime = 0.85f;

    public float GetBaseDamage() => (weapons != null ? weapons.Damage : damageP);
    public bool IsFacingRight => facingRight;
    public bool IsGround => isGround;

    private int jumpCount = 0;
    private bool doubleJumpPressed = false;

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<DashRoll>();
        weapons = GetComponent<WeaponHolder>();
        groundSlam = GetComponent<GroundSlam>() ?? gameObject.AddComponent<GroundSlam>();
    }
    void Start()
    {
        maxHealth += MetaUpgrades.BonusHealth;
        currentHp = maxHealth;
        startPos = transform.position;
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHealth, currentHp + amount);
    }

    public void SetSpawn(Vector3 pos)
    {
        transform.position = pos;
        startPos = pos;
    }

    // Update is called once per frame
    void Update()
    {

        UpdateHpBar();
        Movement();
        jump();
        Animation();
        HandleRunSFX(); 
    }
    private void FixedUpdate()
    {
        if (jumpPressed)
        {
            rb.AddForce(new Vector2(0f, jumpHight), ForceMode2D.Impulse);
            jumpPressed = false;
        }
        else if (doubleJumpPressed)
        {
            // Triệt tiêu vận tốc rơi trước khi bật nhảy lần 2 để cú nhảy đúp mượt mà và nảy bốc
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(new Vector2(0f, jumpHight * 0.95f), ForceMode2D.Impulse);
            doubleJumpPressed = false;
        }
    }
    private void Movement()
    {
        if (dash != null && dash.IsDashing) { moveMent = 0f; return; }
        if (groundSlam != null && groundSlam.IsSlamming) { moveMent = 0f; return; }

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
    private void jump()
    {
        if (groundSlam != null && groundSlam.IsSlamming) return;

        isGround = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        
        if (isGround)
        {
            jumpCount = 0;
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (isGround)
            {
                jumpPressed = true;
                jumpCount = 1;
                animator.ResetTrigger("Attack1");
                if (AudioManager.Instance != null) AudioManager.Instance.SFX_Jump();
            }
            else if (jumpCount <= 1) // Lần nhảy thứ 2 trên không: Double Jump
            {
                doubleJumpPressed = true;
                jumpCount = 2;
                animator.ResetTrigger("Attack1");
                animator.SetBool("Jump", true);
                if (AudioManager.Instance != null) AudioManager.Instance.SFX_Jump();
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

            // Đòn combo 2 hơi bước tới trước nhẹ
            if (comboStep == 2)
            {
                transform.position += (facingRight ? Vector3.right : Vector3.left) * 0.25f;
            }

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

        float baseDmg = (weapons != null ? weapons.Damage : damageP) * MetaUpgrades.DamageMultiplier;
        float radius = weapons != null ? weapons.Range : attackRadius;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, radius, LayerPlayer);
        var done = new System.Collections.Generic.HashSet<GameObject>();

        foreach (var h in hits)
        {
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (!done.Add(root)) continue;

            bool isBackstab = false;
            bool isStunned = false;

            var telegraph = root.GetComponent<EnemyTelegraph>();
            if (telegraph != null)
            {
                isStunned = telegraph.IsStunned;
            }

            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null)
            {
                // Đánh lén sau lưng khi Player và Quái cùng nhìn về một hướng
                isBackstab = (facingRight == e1.IsFacingRight);
            }

            // Điều kiện chí mạng: Đòn Finisher thứ 3 HOẶC đánh sau lưng HOẶC đánh quái đang bị Stun
            bool isCrit = (comboStep == 3) || isBackstab || isStunned;
            float finalDmg = baseDmg * (isCrit ? 1.75f : 1f);

            if (comboStep == 3)
            {
                CameraShake.Shake(0.22f);
            }

            if (e1 != null)
            {
                e1.takeDamageE(finalDmg, isCrit);
                continue;
            }

            var boss = root.GetComponent<PatrolBoss>();
            if (boss != null)
            {
                boss.takeDamageE(finalDmg);
                DamagePopup.Spawn(root.transform.position, finalDmg, Color.white, isCrit);
                continue;
            }

            var d = root.GetComponent<IDamageable>();
            if (d != null)
            {
                d.TakeDamage(finalDmg, root.transform.position);
                DamagePopup.Spawn(root.transform.position, finalDmg, Color.white, isCrit);
            }
        }
    }

    public void TakeDamageP(float damage)
    {
        if (dash != null && dash.IsInvulnerable) return;
        if (groundSlam != null && groundSlam.IsSlamming) return;

        GameFeel.OnHit(transform.position, damage, gameObject);
        animator.SetTrigger("Hurt");
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Hit();

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);
        if (currentHp <= 0) Die();
    }
    private void Die()
    {
        //Destroy(this.gameObject);
        if (gameManager != null) gameManager.GameOver();
    }
    private void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHp / maxHealth;
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
