using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;


public class PatrolEnemy1 : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform checkPoint;
    [SerializeField] private float distance = 1f;
    [SerializeField] LayerMask layerMask;
    private bool facingRight = true;
    [SerializeField] private Transform player;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float retrieveDistance = 2.7f;
    [SerializeField] private float chaseSpeed = 4f;
    private Animator animator;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] LayerMask LayerPlayer;
    [SerializeField] float damageE = 1f;
    [SerializeField] private float maxHealth = 5f;
    private float currentHp;
    [SerializeField] private Image hpBar;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject filePrefab;
    [SerializeField] float dropChance = 1f; // 100%

    private EnemyTelegraph telegraph;
    public bool IsFacingRight => facingRight;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        telegraph = GetComponent<EnemyTelegraph>() ?? gameObject.AddComponent<EnemyTelegraph>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHp = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        UpdateHpBar();

        if (telegraph != null && telegraph.IsStunned)
        {
            animator.SetBool("Attack1", false);
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        //Trong phạm vi tấn công
        if (Vector2.Distance(transform.position,player.position) <= attackRange)
        {
            Chanse();
        }
        else
        {
            if (telegraph != null) telegraph.CancelWarning();
            Patrol();
        }
    }
    private void Chanse()
    {
        if (transform.position.x > player.position.x && facingRight == true)
        {
            transform.eulerAngles = new Vector3(0f, -180f, 0f);
            facingRight = false;
        }
        else if (transform.position.x < player.position.x && facingRight == false)
        {
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
            facingRight = true;
        }
        if (Vector2.Distance(transform.position, player.position) > retrieveDistance)
        {
            if (telegraph != null) telegraph.CancelWarning();
            RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, Vector2.down, distance, layerMask);

            if (hit == false)
            {
                transform.position = Vector2.MoveTowards(transform.position, player.position, 0f * Time.deltaTime); 
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);
            }
            animator.SetBool("Attack1", false);
        }
        else
        {
            if (telegraph != null && !telegraph.IsPreparingAttack)
            {
                telegraph.ShowWarning(0.35f, null);
            }
            animator.SetBool("Attack1", true);
        }

    }
    private void Patrol()
    {
        transform.Translate(Vector2.right * moveSpeed* Time.deltaTime);
        RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, Vector2.down, distance, layerMask);
        Collider2D hit1 = Physics2D.OverlapCircle(attackPoint.position, 0.2f, layerMask);
        if ((hit == false || hit1 == true) && facingRight)
        {
            transform.eulerAngles = new Vector3(0, -180, 0);
            facingRight = false;
        }
        else if ((hit == false || hit1 == true) && facingRight == false)
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            facingRight = true;
        }
    }
    private void Attack()
    {
        Collider2D collInfo = Physics2D.OverlapCircle(attackPoint.position, attackRadius, LayerPlayer);
        if (collInfo)
        {
            if (collInfo.gameObject.GetComponent<Player>() != null)
            {
                collInfo.gameObject.GetComponent<Player>().TakeDamageP(damageE);
            }
        }
    }
    public void Stun(float duration = 1.0f)
    {
        if (telegraph != null) telegraph.Stun(duration);
        animator.SetBool("Attack1", false);
        animator.SetTrigger("Hurt");
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void takeDamageE(float damage)
    {
        takeDamageE(damage, false);
    }

    public void takeDamageE(float damage, bool isCritical)
    {
        if (telegraph != null)
        {
            bool wasInterrupted = telegraph.TryInterrupt();
            if (wasInterrupted)
            {
                isCritical = true; // Thưởng crit khi ngắt đòn đối thủ!
                animator.SetBool("Attack1", false);
            }
        }

        GameFeel.OnHit(transform.position, damage, gameObject, isCritical);

        animator.SetTrigger("Hurt");
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);
        if (currentHp <= 0) Die();
    }
    private void Die()
    {
        if (telegraph != null) telegraph.CancelWarning();
        Vector3 spawnPosCoin = new Vector3(transform.position.x,transform.position.y+2f,0);
        Instantiate(coinPrefab, spawnPosCoin, Quaternion.identity);
        
        if (UnityEngine.Random.value < dropChance)
        {
            Vector3 spawnPosFile = new Vector3(transform.position.x + 0.5f, transform.position.y + 2f, 0);
            Instantiate(filePrefab, spawnPosFile, Quaternion.identity);
        }
        animator.SetTrigger("Die");
    }
    private void OnDieAnimationEnd()
    {
        Destroy(gameObject);
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
        if (checkPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(checkPoint.position, Vector2.down * distance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }

}
