using System.Collections;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    public bool cameraOnlyEnemy;
    [Header("Target")]
    [SerializeField] private Transform player;

    private PlayerStats playerStats;

    [Header("Boss Control")]
    public bool controlledByBoss = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackLungeDistance = 1.2f;
    [SerializeField] private float attackLungeTime = 0.12f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Unity.UI.Shaders.Sample.CustomSlider healthSlider;

    [Header("AI")]
    [SerializeField] private float aggroRadius = 10f;
    [SerializeField] private float patrolRadius = 6f;
    [SerializeField] private float patrolChangeTime = 3f;

    private Vector3 startPos;
    private Vector3 patrolTarget;
    private float patrolTimer;
    private bool isAggroed;

    private float currentHealth;
    private float cooldownTimer;

    private bool isDead;
    private bool isAttacking;
    private bool isStaggered;

    private Vector3 knockbackVelocity;
    private float knockbackDecay = 10f;

    [SerializeField] private float uiSmoothSpeed = 8f;

    private float healthVisual;

    [SerializeField] private GameObject damageVFX;
    [SerializeField] private Transform damageVFXPoint;
    [SerializeField] private float damageVFXLife = 1.5f;

    private void Start()
    {
        currentHealth = maxHealth;

        startPos = transform.position;
        patrolTarget = GetRandomPatrolPoint();

        if (healthSlider != null)
        {
            float normalized = currentHealth / maxHealth;
            healthSlider.SetValue(normalized);
        }

        healthVisual = currentHealth / maxHealth;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        Debug.Log("Player Object: " + playerObject);

        if (playerObject != null)
        {
            Debug.Log("Components on Player:");

            Component[] comps = playerObject.GetComponents<Component>();

            foreach (Component c in comps)
            {
                Debug.Log(c.GetType().Name);
            }

            player = playerObject.transform;

            playerStats = playerObject.GetComponent<PlayerStats>();

            Debug.Log("PlayerStats found: " + playerStats);
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        EnemyPossessionController poss = GetComponent<EnemyPossessionController>();
        if (poss != null && poss.IsPossessed())
        {
            return;
        }

        if (controlledByBoss)
        return;

        cooldownTimer -= Time.deltaTime;

        //enemy just patrols and ignores player logic
        if (cameraOnlyEnemy)
        {
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.position);

                if (distance <= aggroRadius)
                {
                    isAggroed = true;
                }
                else
                {
                    isAggroed = false;
                }

                if (isAggroed)
                {
                    RotateTowardsPlayer();
                }
                else
                {
                    Patrol();
                }
            }
            else
            {
                Patrol();
            }
        }
        else if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= aggroRadius)
            {
                isAggroed = true;
            }
            else
            {
                isAggroed = false;
            }

            RotateTowardsPlayer();

            if (isAggroed)
            {
                if (distance > stopDistance)
                {
                    MoveTowardsPlayer();
                }
                else
                {
                    AttackPlayer();
                }
            }
            else
            {
                Patrol();
            }
        }
        else
        {
            Patrol();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        float target = currentHealth / maxHealth;

        healthVisual = Mathf.Lerp(healthVisual, target, Time.deltaTime * uiSmoothSpeed);

        if (healthSlider != null)
        {
            healthSlider.SetValue(healthVisual);
        }
    }

    //player movement system 

    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;

        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    private void RotateTowardsPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    //patrol system

    private void Patrol()
    {
        patrolTimer += Time.deltaTime;

        if (patrolTimer >= patrolChangeTime)
        {
            patrolTimer = 0f;
            patrolTarget = GetRandomPatrolPoint();
        }

        Vector3 direction = (patrolTarget - transform.position).normalized;

        transform.position += direction * (moveSpeed * 0.5f) * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 random = Random.insideUnitCircle * patrolRadius;

        return new Vector3(startPos.x + random.x, transform.position.y, startPos.z + random.y);
    }

    //player combat and attacks

    private void AttackPlayer()
    {
        if (cooldownTimer > 0f)
        {
            return;
        }

        if (isStaggered)
        {
            cooldownTimer = 0.15f;
            return;
        }

        if (CrosshairEvents.Instance != null)
        {
            CrosshairEvents.Instance.TriggerPlayerTarget();
        }

        cooldownTimer = attackCooldown;

        StartCoroutine(DoAttack());
    }

    private IEnumerator DoAttack()
    {
        isAttacking = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos;

        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            targetPos = startPos + dir * attackLungeDistance;
        }

        float t = 0f;

        while (t < attackLungeTime)
        {
            t += Time.deltaTime;

            float progress = t / attackLungeTime;

            float height = Mathf.Sin(progress * Mathf.PI) * 0.4f;

            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            transform.position += Vector3.up * height;

            yield return null;
        }

        transform.position = targetPos;

        PlayerCombat combat = playerStats.GetComponent<PlayerCombat>();
        if (combat != null && combat.IsInvulnerable())
        {
            isAttacking = false;
            yield break;
        }

        if (playerStats != null)
        {
            playerStats.TakeDamage(attackDamage);
        }

        Debug.Log("Enemy attacked player for " + attackDamage);

        isAttacking = false;
    }

    //player stats and healthh

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;

        if (damage > 0f)
        {
            PlayDamageVFX();
        }

        StartCoroutine(Stagger());
        StartCoroutine(SquishEffect());

        Debug.Log("Enemy took " + damage + " damage. HP: " + currentHealth);

        DamagePopupManager.Instance.SpawnDamagePopup(
            damage,
            transform,
            false,
            currentHealth <= 0f,
            false,
            false
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator Stagger()
    {
        isStaggered = true;

        yield return new WaitForSeconds(0.25f);

        isStaggered = false;
    }

    private IEnumerator SquishEffect()
    {
        Vector3 originalScale = transform.localScale;

        Vector3 squish = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.75f, originalScale.z * 1.15f);

        transform.localScale = squish;

        yield return new WaitForSeconds(0.1f);

        transform.localScale = originalScale;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        StopAllCoroutines();
        enabled = false;

        if (controlledByBoss)
        {
            gameObject.SetActive(false); // instead of Destroy
            return;
        }

        Destroy(gameObject, 0.05f);
    }

    public bool IsDead()
    {
        return isDead;
    }


    public void ApplyKnockback(Vector3 dir, float force)
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(dir.normalized * force, ForceMode.Impulse);
            return;
        }

        StartCoroutine(KnockbackMove(dir, force));
    }

    private IEnumerator KnockbackMove(Vector3 dir, float force)
    {
        float t = 0.15f;

        while (t > 0f)
        {
            t -= Time.deltaTime;

            transform.position += dir.normalized * force * Time.deltaTime;

            yield return null;
        }
    }

    public void ForceStopMovement()
    {
        StopAllCoroutines();
    }

    private void PlayDamageVFX()
    {
        if (damageVFX == null)
        {
            return;
        }

        Transform spawnPoint = transform;

        if (damageVFXPoint != null)
        {
            spawnPoint = damageVFXPoint;
        }

        GameObject vfx = Instantiate(
            damageVFX,
            spawnPoint.position,
            Quaternion.identity
        );

        Destroy(vfx, damageVFXLife);
    }
}