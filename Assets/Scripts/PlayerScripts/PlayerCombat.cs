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

    [Header("Attack 2")]
    [SerializeField] private float attack2Damage = 18f;

    [Header("Attack 3")]
    [SerializeField] private float attack3Damage = 30f;
    [SerializeField] private float attack3Duration = 0.6f;

    private bool attackQueued;
    private bool isAttacking;
    private bool canCombo;

    public int currentAttackStep;
    public float currentAttackDamage;

    private bool isInvulnerable;

    [Header("Animations")]
    private Animator animator;
    private int animAttack1;
    private int animAttack2;
    private int animAttack3;

    private WeaponScript weaponHitbox;

    private void Start()
    {
        currentStamina = maxStamina;

        animator = GetComponentInChildren<Animator>();

        animAttack1 = Animator.StringToHash("Attack1");
        animAttack2 = Animator.StringToHash("Attack2");
        animAttack3 = Animator.StringToHash("Attack3");

        weaponHitbox = GetComponentInChildren<WeaponScript>();
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
            currentStamina += staminaRegenRate * Time.deltaTime;

        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }

    private void HandleComboTimer()
    {
        if (comboStep == 1 || comboStep == 2)
        {
            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0f)
                comboStep = 0;
        }
    }

    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isAttacking)
            {
                if (canCombo)
                    attackQueued = true;

                return;
            }

            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (currentStamina < lightAttackCost)
            return;

        currentStamina -= lightAttackCost;

        comboStep++;

        if (comboStep > 3)
            comboStep = 1;

        if (comboStep == 1)
            comboTimer = combo1ResetTime;

        if (comboStep == 2)
            comboTimer = combo2ResetTime;

        StartCoroutine(DoAttack(comboStep));
    }

    private IEnumerator DoAttack(int step)
    {
        isAttacking = true;
        attackQueued = false;
        canCombo = false;

        currentAttackStep = step;

        if (weaponHitbox != null)
            weaponHitbox.ResetHits();

        isInvulnerable = false;

        float attackDuration = 0.4f;

        if (animator != null)
        {
            animator.ResetTrigger(animAttack1);
            animator.ResetTrigger(animAttack2);
            animator.ResetTrigger(animAttack3);

            if (step == 1)
            {
                animator.SetTrigger(animAttack1);
                currentAttackDamage = attack1Damage;
                attackDuration = 0.35f;
            }
            else if (step == 2)
            {
                animator.SetTrigger(animAttack2);
                currentAttackDamage = attack2Damage;
                attackDuration = 0.45f;
            }
            else if (step == 3)
            {
                animator.SetTrigger(animAttack3);
                currentAttackDamage = attack3Damage;
                attackDuration = attack3Duration;

                isInvulnerable = true;
            }
        }


        yield return new WaitForSeconds(attackDuration * 0.5f);
        canCombo = true;

    
        yield return new WaitForSeconds(attackDuration * 0.5f);

        isAttacking = false;
        isInvulnerable = false;

        if (attackQueued)
        {
            attackQueued = false;
            TryAttack();
        }
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public float GetCurrentDamage()
    {
        return currentAttackDamage;
    }

    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }
}