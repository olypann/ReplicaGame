using UnityEngine;

public class WormBossPartHealth : MonoBehaviour
{
    [Header("health")]
    [SerializeField] private float maxHealth = 50f;

    private float currentHealth;
    private bool isDead;


    [Header("ui")]
    [SerializeField] private Unity.UI.Shaders.Sample.CustomSlider healthSlider;

    private float healthVisual; // smoothed value for the bar
    [SerializeField] private float uiSmoothSpeed = 8f;



    private void Start()
    {
        currentHealth = maxHealth;
        healthVisual = 1f;

        // start full
        if (healthSlider != null)
        {
            healthSlider.SetValue(1f);
        }
    }

    private void Update()
    {
        UpdateUI();
    }


    // smooth health bar so it doesn't snap instantly
    private void UpdateUI()
    {
        float target = currentHealth / maxHealth;

        healthVisual = Mathf.Lerp(
            healthVisual,
            target,
            Time.deltaTime * uiSmoothSpeed
        );

        if (healthSlider != null)
        {
            healthSlider.SetValue(healthVisual);
        }
    }


    public void TakeDamage(float dmg)
    {
        WormBossController boss = GetComponentInParent<WormBossController>();

        // let boss know it got hit so it doesn't reset
        if (boss != null)
        {
            boss.NotifyBossHit();
        }

        if (isDead)
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
        isDead = true;

        WormBossController boss = GetComponentInParent<WormBossController>();

        if (boss != null)
        {
            boss.NotifyPartDeath(transform);
        }

        // disable instead of destroy so nothing weird tries to revive it visually
        gameObject.SetActive(false);

        Debug.Log("Part died: " + gameObject.name);
    }


    public bool IsDead()
    {
        return isDead;
    }


    // mostly for ui or debug
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}