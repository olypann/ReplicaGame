using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    private bool isDead;

    [Header("Animations")]
    private Animator animator;
    private int animHit;
    private int animDie;

    private void Start()
    {
        currentHealth = maxHealth;

        animator = GetComponentInChildren<Animator>();

        animHit = Animator.StringToHash("Hit");
        animDie = Animator.StringToHash("Die");
    }

    public void TakeDamage(float damage)
    {
        PlayerCombat combat = GetComponent<PlayerCombat>();

        if (combat != null && combat.IsInvulnerable())
        {
            return;
        }
        
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;

        Debug.Log("Player took " + damage + " damage. HP: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }

        if (animator != null)
        {
            animator.SetTrigger(animHit);
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        Debug.Log("Player died");

        if (animator != null)
        {
            animator.SetTrigger(animDie);
        }
        
    }

    public float GetHealth()
    {
        return currentHealth;
    }
}