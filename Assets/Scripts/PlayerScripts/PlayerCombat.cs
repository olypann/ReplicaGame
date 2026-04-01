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

    [Header("Charged Attack")]
    [SerializeField] private float chargeHoldThreshold = 0.25f;
    [SerializeField] private float chargeTime = 1.2f;
    [SerializeField] private float chargedDamage = 50f;
    [SerializeField] private ParticleSystem chargeStartParticles;
    [SerializeField] private ParticleSystem chargeReadyParticles;

    [Header("Air Attack")]
    [SerializeField] private float airAttackDamage = 25f;
    [SerializeField] private float airAttackForce = 25f;
    [SerializeField] private float airAttackRadius = 3f;

    private bool didAirAttack;

    private bool attackQueued;
    private bool isAttacking;
    private bool canCombo;
    private bool isInvulnerable;

    private bool isCharging;
    private float chargeTimer;
    private bool chargeStarted;

    public int currentAttackStep;
    public float currentAttackDamage;

    [Header("Animations")]
    private Animator animator;
    private int animAttack1;
    private int animAttack2;
    private int animAttack3;
    private int animCharge; //

    private int animChargeStart;
    private int animChargeRelease;
    
    private int animAirAttack;

    private WeaponScript weaponHitbox;
    private CharacterController controller;

    private void Start()
    {
        // setup references and starting values
        currentStamina = maxStamina;

        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();

        animAttack1 = Animator.StringToHash("Attack1");
        animAttack2 = Animator.StringToHash("Attack2");
        animAttack3 = Animator.StringToHash("Attack3");
        animCharge = Animator.StringToHash("ChargeAttack"); //
        animAirAttack = Animator.StringToHash("AirAttack");

        animChargeStart = Animator.StringToHash("ChargeStart");
        animChargeRelease = Animator.StringToHash("ChargeRelease");

        weaponHitbox = GetComponentInChildren<WeaponScript>();
    }

    private void Update()
    {
        // main update loop
        HandleStamina();
        HandleComboTimer();
        HandleAttackInput();
        HandleCharge();
    }

    private void HandleStamina()
    {
        // regenerate stamina over time
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
        }

        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }

    private void HandleComboTimer()
    {
        // reset combo if timer runs out
        if (comboStep == 1 || comboStep == 2)
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
        bool isGrounded = controller.isGrounded;

        // left click pressed
        if (Input.GetMouseButtonDown(0))
        {
            // air attack case
            if (!isGrounded)
            {
                StartCoroutine(DoAirAttack());
                return;
            }

            // reset charge state
            chargeTimer = 0f;
            chargeStarted = false;
            isCharging = true;

            // queue combo if currently attacking
            if (isAttacking)
            {
                if (canCombo)
                {
                    attackQueued = true;
                }

                return;
            }
        }

        // left click released
        if (Input.GetMouseButtonUp(0))
        {
            if (!isGrounded)
            {
                return;
            }

            // charged attack or normal attack
            if (chargeStarted && chargeTimer >= chargeTime)
            {
                animator.SetTrigger(animChargeRelease);
                StartCoroutine(DoChargedAttack());
            }
            else
            {
                TryAttack();
            }

            StopChargeParticles();
            isCharging = false;
        }
    }

    private void HandleCharge()
    {
        // handle charge buildup
        if (!isCharging)
        {
            return;
        }

        chargeTimer += Time.deltaTime;

        // start charge effect
        if (!chargeStarted && chargeTimer >= chargeHoldThreshold)
        {
            chargeStarted = true;
            animator.SetTrigger(animChargeStart);

            if (chargeStartParticles != null)
            {
                chargeStartParticles.Play();
            }
        }

        // ready charge effect
        if (chargeStarted && chargeTimer >= chargeTime)
        {
            if (chargeReadyParticles != null && !chargeReadyParticles.isPlaying)
            {
                chargeReadyParticles.Play();
            }
        }
    }

    private void StopChargeParticles()
    {
        // stop charge visuals
        if (chargeStartParticles != null)
        {
            chargeStartParticles.Stop();
        }

        if (chargeReadyParticles != null)
        {
            chargeReadyParticles.Stop();
        }
    }

    private void TryAttack()
    {
        // stamina check
        if (currentStamina < lightAttackCost)
        {
            return;
        }

        currentStamina -= lightAttackCost;

        // increase combo step
        comboStep++;

        if (comboStep > 3)
        {
            comboStep = 1;
        }

        // set combo timers
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
        // start attack state
        isAttacking = true;
        attackQueued = false;
        canCombo = false;

        currentAttackStep = step;

        if (weaponHitbox != null)
        {
            weaponHitbox.ResetHits();
        }

        isInvulnerable = false;

        float attackDuration = 0.4f;

        animator.ResetTrigger(animAttack1);
        animator.ResetTrigger(animAttack2);
        animator.ResetTrigger(animAttack3);

        // choose attack type
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

        yield return new WaitForSeconds(attackDuration * 0.5f);
        canCombo = true;

        yield return new WaitForSeconds(attackDuration * 0.6f);

        isAttacking = false;
        isInvulnerable = false;

        // handle queued combo
        if (attackQueued)
        {
            attackQueued = false;
            TryAttack();
        }
    }

    private IEnumerator DoChargedAttack()
    {
        // charged attack state
        isAttacking = true;
        isInvulnerable = true;

        animator.SetTrigger(animCharge);
        currentAttackDamage = chargedDamage;

        if (weaponHitbox != null)
        {
            weaponHitbox.ResetHits();
        }

        yield return new WaitForSeconds(0.8f);

        isAttacking = false;
        isInvulnerable = false;
    }

    private IEnumerator DoAirAttack()
    {
        // air attack start
        isAttacking = true;
        isInvulnerable = true;
        didAirAttack = true;

        animator.SetTrigger(animAirAttack);

        // push player downward while airborne
        while (!controller.isGrounded)
        {
            controller.Move(Vector3.down * airAttackForce * Time.deltaTime);
            yield return null;
        }

        // apply damage on landing
        Collider[] hits = Physics.OverlapSphere(transform.position, airAttackRadius);

        foreach (Collider hit in hits)
        {
            EnemyScript enemy = hit.GetComponent<EnemyScript>();

            if (enemy != null)
            {
                enemy.TakeDamage(airAttackDamage);
            }
        }

        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
        isInvulnerable = false;
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

    public bool DidAirAttack()
    {
        return didAirAttack;
    }

    public void ResetAirAttackFlag()
    {
        didAirAttack = false;
    }
}