using UnityEngine;

public class WormBossHeadHealth : MonoBehaviour
{
    [Header("health")]
    [SerializeField] private float maxHealth = 500f;

    private float currentHealth;


    // how many segments need to die before head can fully die
    [Header("progression")]
    [SerializeField] private int totalSegments = 5;

    private int unlockedSegments = 0;
    private float segmentHealthValue;



    private void Start()
    {
        currentHealth = maxHealth;

        // each segment basically unlocks a chunk of damage
        segmentHealthValue = maxHealth / totalSegments;
    }


    // called externally when a body segment dies
    public void UnlockNextPhase()
    {
        if (unlockedSegments >= totalSegments)
        {
            return;
        }

        unlockedSegments++;

        Debug.Log("[Head] Unlocked segment " + unlockedSegments + "/" + totalSegments);
    }


    public void TakeDamage(float dmg)
    {
        // notify boss so it doesn't reset due to inactivity
        WormBossController boss = GetComponentInParent<WormBossController>();

        if (boss != null)
        {
            boss.NotifyBossHit();
        }

        // how much total damage is currently allowed based on unlocked segments
        float maxAllowedDamage = unlockedSegments * segmentHealthValue;

        float minHealthAllowed = maxHealth - maxAllowedDamage;


        // can't damage head at all until at least one segment is gone
        if (unlockedSegments <= 0)
        {
            Debug.Log("[Head] Damage blocked (no segments unlocked)");
            return;
        }

        float newHealth = currentHealth - dmg;

        // prevent going below the current phase limit
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


    // simple helper for UI 
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