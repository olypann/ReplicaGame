using UnityEngine;

public class WormBossPartHealth : MonoBehaviour
{
    [Header("health")]
    [SerializeField] private float maxHealth = 50f;

    [Header("ui")]
    [SerializeField] private Unity.UI.Shaders.Sample.CustomSlider healthSlider;

    private float currentHealth;
    private float healthVisual;

    private bool isDead;

    [SerializeField] private float uiSmoothSpeed = 8f;

    private void Start()
    {
        currentHealth = maxHealth;
        healthVisual = 1f;

        if (healthSlider != null)
        {
            healthSlider.SetValue(1f);
        }
    }

    private void Update()
    {
        UpdateUI();
    }

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

        gameObject.SetActive(false); // IMPORTANT: prevents revive visuals

        Debug.Log("Part died: " + gameObject.name);
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}