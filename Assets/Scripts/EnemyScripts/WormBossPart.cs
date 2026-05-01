using UnityEngine;

public class WormBossPart : MonoBehaviour
{
    public enum PartType
    {
        Head,
        Body
    }

    public PartType partType;

    [SerializeField] private float maxHealth = 50f;

    private float currentHealth;

    private bool dead;

    private WormBossHeadHealth headHealth;

    private void Start()
    {
        currentHealth = maxHealth;

        headHealth = GetComponentInParent<WormBossHeadHealth>();
    }
    

    public void TakeDamage(float dmg)
    {
        if (dead)
        {
            return;
        }

        currentHealth -= dmg;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        dead = true;

        if (partType == PartType.Body)
        {
            if (headHealth != null)
            {
                headHealth.UnlockNextPhase();
            }
        }

        Destroy(gameObject);
    }
}