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
    [SerializeField] private float combo1ResetTime = 0.6f;
    [SerializeField] private float combo2ResetTime = 0.8f;

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

    [Header("Range Visual")]
    [SerializeField] private Transform attackRangeVisual;

    private bool attackQueued;

    [Header("Targeting")]
    [SerializeField] private LayerMask enemyLayer;

    private bool isAttacking;

    [Header("Animations")]
    private Animator animator;
    private int animAttack1;
    private int animAttack2;
    private int animAttack3;

    private void Start()
    {
        currentStamina = maxStamina;

        animator = GetComponentInChildren<Animator>();

        animAttack1 = Animator.StringToHash("Attack1");
        animAttack2 = Animator.StringToHash("Attack2");
        animAttack3 = Animator.StringToHash("Attack3");

        if (attackRangeVisual != null)
        {
            attackRangeVisual.gameObject.SetActive(false);
        }
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
        if (comboStep == 1)
        {
            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0f)
            {
                comboStep = 0;
            }
        }

        if (comboStep == 2)
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
            attackQueued = true;
            return;
        }

        if (currentStamina < lightAttackCost)
        {
            return;
        }

        currentStamina -= lightAttackCost;

        comboStep += 1;

        int nextStep = comboStep + 1;

        if (nextStep > 3)
        {
            nextStep = 1;
        }

        StartCoroutine(DoAttack(nextStep));
        comboStep = nextStep;

        if (comboStep == 1)
        {
            comboTimer = combo1ResetTime;
        }

        if (comboStep == 2)
        {
            comboTimer = combo2ResetTime;
        }

        StartCoroutine(DoAttack(comboStep));
    }

    private IEnumerator DoAttack(int step)
    {
        isAttacking = true;

        float attackDuration = 0.4f;
        float radius = 1f;

        if (animator != null)
        {
            animator.ResetTrigger(animAttack1);
            animator.ResetTrigger(animAttack2);
            animator.ResetTrigger(animAttack3);

            if (step == 1)
            {
                animator.SetTrigger(animAttack1);
                attackDuration = 0.35f;
                radius = attack1Radius;
            }

            if (step == 2)
            {
                animator.SetTrigger(animAttack2);
                attackDuration = 0.45f;
                radius = attack2Radius;
            }

            if (step == 3)
            {
                animator.SetTrigger(animAttack3);
                attackDuration = attack3Duration;
                radius = attack3Radius;
            }
        }

        // show range visual
        if (attackRangeVisual != null)
        {
            attackRangeVisual.gameObject.SetActive(true);
            attackRangeVisual.localScale = Vector3.one * radius * 2f;
        }

        // damage
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

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;

        // hide range visual
        if (attackRangeVisual != null)
        {
            attackRangeVisual.gameObject.SetActive(false);
        }

        if (attackQueued)
        {
            attackQueued = false;

            comboStep += 1;

            if (comboStep > 3)
            {
                comboStep = 1;
            }

            if (comboStep == 1)
            {
                comboTimer = combo1ResetTime;
            }

            if (comboStep == 2)
            {
                comboTimer = combo2ResetTime;
            }

            StartCoroutine(DoAttack(comboStep));
        }
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