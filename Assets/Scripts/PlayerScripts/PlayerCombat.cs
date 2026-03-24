using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float lightAttackCost = 10f;

    private float currentStamina;

    [Header("Combo System")]
    [SerializeField] private float comboResetTime = 0.8f;

    private int comboStep = 0;
    private float comboTimer;

    [Header("Attack 1")]
    [SerializeField] private float attack1Damage = 10f;
    [SerializeField] private float attack1Radius = 1.5f;

    [Header("Attack 2")]
    [SerializeField] private float attack2Damage = 18f;
    [SerializeField] private float attack2Radius = 2.2f;

    [Header("Attack 3")]
    [SerializeField] private float attack3Damage = 30f;
    [SerializeField] private float attack3Radius = 3.2f;
    [SerializeField] private float attack3Duration = 0.6f;

    [Header("Targeting")]
    [SerializeField] private LayerMask enemyLayer;

    private bool isAttacking;

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        HandleStamina();
        HandleComboTimer();
        HandleAttackInput();
    }

    private void HandleStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
        }

        if (currentStamina > maxStamina)
        {
            currentStamina = maxStamina;
        }
    }

    private void HandleComboTimer()
    {
        if (comboStep > 0)
        {
            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0f)
            {
                comboStep = 0;
            }
        }
    }

    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (isAttacking)
        {
            return;
        }

        if (currentStamina < lightAttackCost)
        {
            return;
        }

        currentStamina -= lightAttackCost;

        comboStep += 1;

        if (comboStep > 3)
        {
            comboStep = 1;
        }

        comboTimer = comboResetTime;

        StartCoroutine(DoAttack(comboStep));
    }

    private IEnumerator DoAttack(int step)
    {
        isAttacking = true;

        if (step == 1)
        {
            DealDamage(attack1Damage, attack1Radius, 0.1f);
        }

        if (step == 2)
        {
            DealDamage(attack2Damage, attack2Radius, 0.15f);
        }

        if (step == 3)
        {
            float elapsed = 0f;

            while (elapsed < attack3Duration)
            {
                DealDamage(attack3Damage, attack3Radius, 0.1f);
                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }

        isAttacking = false;
    }

    private void DealDamage(float damage, float radius, float delay)
    {
        StartCoroutine(DamageTick(damage, radius, delay));
    }

    private IEnumerator DamageTick(float damage, float radius, float delay)
    {
        yield return new WaitForSeconds(delay);

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyScript enemy = hit.GetComponent<EnemyScript>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}