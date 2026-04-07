using System.Collections;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    private PlayerStats playerStats;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Unity.UI.Shaders.Sample.CustomSlider healthSlider;

    private float currentHealth;
    private float cooldownTimer;

    private bool isDead;
    private bool isAttacking;

    [SerializeField] private float uiSmoothSpeed = 8f;

    private float healthVisual;

    private void Start()
    {
        
        currentHealth = maxHealth;

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

        if (player == null)
        {
            return;
        }

        cooldownTimer -= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, player.position);

        RotateTowardsPlayer();

        if (distance > stopDistance)
        {
            MoveTowardsPlayer();
        }
        else
        {
            AttackPlayer();
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

    //player combat and attacks

    private void AttackPlayer()
    {
        if (cooldownTimer > 0f)
        {
            return;
        }

        cooldownTimer = attackCooldown;

        StartCoroutine(DoAttack());
    }

    private IEnumerator DoAttack()
    {
        isAttacking = true;

        yield return new WaitForSeconds(0.1f);

        PlayerCombat combat = playerStats.GetComponent<PlayerCombat>();
        if (combat != null && combat.IsInvulnerable())
        {
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

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        Debug.Log("Enemy died");

        Destroy(gameObject);
    }
}