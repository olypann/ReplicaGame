using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;
    [SerializeField] private Unity.UI.Shaders.Sample.Meter healthMeter;

    private bool isDead;

    [Header("Animations")]
    private Animator animator;
    private int animHit;
    private int animDie;

    private Vector3 startPosition;

    private void Start()
    {
        currentHealth = maxHealth;

        startPosition = transform.position;

        if (healthMeter != null)
        {
            float normalized = currentHealth / maxHealth;
            healthMeter.SetValue(normalized);
        }

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

        if (healthMeter != null)
        {
            float normalized = currentHealth / maxHealth;
            healthMeter.SetValue(normalized);
        }

        Debug.Log("Player took " + damage + " damage. HP: " + currentHealth);
        SoundManager.Instance?.PlayPlayerHit();

        DamagePopupManager.Instance.SpawnDamagePopup(
            damage,
            transform,
            true,
            currentHealth <= 0f,
            false,
            false
        );

        if (currentHealth <= 0f)
        {
            SoundManager.Instance?.PlayPlayerDeath();
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

        FindFirstObjectByType<EnemyWaveManager>()?.OnPlayerDied();
    }


    public void ResetPlayer()
    {
        isDead = false;
        currentHealth = maxHealth;

        if (healthMeter != null)
        {
            float normalized = currentHealth / maxHealth;
            healthMeter.SetValue(normalized);
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }


    public void Revive()
    {
        isDead = false;

        currentHealth = maxHealth;

        if (healthMeter != null)
        {
            float normalized = currentHealth / maxHealth;
            healthMeter.SetValue(normalized);
        }

        Animator anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        Debug.Log("player revived");
    }


    public void ResetPosition()
    {
        transform.position = startPosition;
    }

    public float GetHealth()
    {
        return currentHealth;
    }
}