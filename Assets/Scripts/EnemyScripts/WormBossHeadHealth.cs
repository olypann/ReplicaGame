using UnityEngine;

public class WormBossHeadHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 500f;

    private float currentHealth;

    private int unlockedSegments = 0;

    private float segmentSize;

    private void Start()
    {
        currentHealth = maxHealth;
        segmentSize = maxHealth * 0.2f;
    }

    public void TakeDamage(float dmg)
    {
        float allowedDamage = unlockedSegments * segmentSize;

        float minHealth = maxHealth - allowedDamage;

        if (currentHealth <= minHealth)
        {
            return;
        }

        currentHealth -= dmg;

        if (currentHealth < minHealth)
        {
            currentHealth = minHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void UnlockNextPhase()
    {
        if (unlockedSegments >= 5)
        {
            return;
        }

        unlockedSegments++;
    }

    private void Die()
    {
        Debug.Log("boss head dead - fight over");
    }
}