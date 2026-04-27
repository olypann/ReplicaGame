using UnityEngine;

public class WormBossHeadHealth : MonoBehaviour
{
    [Header("health")]
    [SerializeField] private float maxHealth = 500f;

    private float currentHealth;

    [Header("progression")]
    [SerializeField] private int totalSegments = 5;

    private int unlockedSegments = 0;
    private float segmentHealthValue;

    private void Start()
    {
        currentHealth = maxHealth;

        segmentHealthValue = maxHealth / totalSegments;
    }

    // Called by boss when a body part dies
    public void UnlockNextPhase()
    {
        if (unlockedSegments >= totalSegments)
            return;

        unlockedSegments++;

        Debug.Log("[Head] Unlocked segment " + unlockedSegments + "/" + totalSegments);
    }

    public void TakeDamage(float dmg)
    {
        float maxAllowedDamage = unlockedSegments * segmentHealthValue;

        float minHealthAllowed = maxHealth - maxAllowedDamage;

        // ❌ still locked
        if (unlockedSegments <= 0)
        {
            Debug.Log("[Head] Damage blocked (no segments unlocked)");
            return;
        }

        // clamp damage so head cannot go below allowed threshold
        float newHealth = currentHealth - dmg;

        if (newHealth < minHealthAllowed)
        {
            newHealth = minHealthAllowed;
        }

        currentHealth = newHealth;

        Debug.Log("[Head] HP: " + currentHealth + " / " + maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }

    private void Die()
    {
        Debug.Log("Boss head defeated - fight over");
        Destroy(gameObject);
    }
}