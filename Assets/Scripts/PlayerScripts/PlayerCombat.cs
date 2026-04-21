using System.Collections;
using UnityEngine;

using UnityEngine.UI;

public class PlayerCombat : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f;

    [SerializeField] private Unity.UI.Shaders.Sample.CustomSlider staminaSlider;
    [SerializeField] private Unity.UI.Shaders.Sample.CustomSlider abilitySlider;

    [SerializeField] private float uiSmoothSpeed = 8f;

    private float staminaVisual;
    private float abilityVisual;


    [SerializeField] private float lightAttackCost = 10f;

    public float currentStamina;

    [Header("Ability Charge")]
    [SerializeField] private float maxAbilityCharge = 100f;
    [SerializeField] public float abilityGainPerHit = 10f;
    

    public float currentAbilityCharge;

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

    [Header("Attack VFX")]
    [SerializeField] private GameObject attack1VFX;
    [SerializeField] private GameObject attack2VFX;
    [SerializeField] private GameObject attack3VFX;
    [SerializeField] private GameObject airAttackVFX;
    [SerializeField] private GameObject chargedAttackVFX;

    [SerializeField] private Transform vfxSpawnPoint;

    [SerializeField] private float attack1VFXTime = 0.15f;
    [SerializeField] private float attack2VFXTime = 0.2f;
    [SerializeField] private float attack3VFXTime = 0.25f;
    [SerializeField] private float airAttackVFXTime = 0f;
    [SerializeField] private float chargedAttackVFXTime = 0.2f;

    private GameObject currentVFX;

    [Header("Charge VFX")]
    [SerializeField] private GameObject chargeReadyObject;
    [SerializeField] private GameObject chargeFullObject;
    [SerializeField] private float chargeLoopStopTime = 0.3f;

    [Header("Charged Attack")]
    [SerializeField] private float chargeHoldThreshold = 0.25f;
    [SerializeField] private float chargeTime = 1.2f;
    [SerializeField] private float chargedDamage = 50f;

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
        currentAbilityCharge = 0f;

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

        staminaVisual = currentStamina / maxStamina;
        abilityVisual = currentAbilityCharge / maxAbilityCharge;

        if (staminaSlider != null)
        {
            staminaSlider.SetValue(staminaVisual);
        }

        if (abilitySlider != null)
        {
            abilitySlider.SetValue(abilityVisual);
        }

        if (chargeReadyObject != null)
        {
            chargeReadyObject.SetActive(false);
        }

        if (chargeFullObject != null)
        {
            chargeFullObject.SetActive(false);
        }
    }

    private void Update()
    {
        // main update loop
        HandleStamina();
        
        HandleComboTimer();
        HandleAttackInput();
        HandleCharge();

        if (Input.GetKeyDown(KeyCode.E))
        {
            UseAbility();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        float staminaTarget = currentStamina / maxStamina;
        float abilityTarget = currentAbilityCharge / maxAbilityCharge;

        staminaVisual = Mathf.Lerp(staminaVisual, staminaTarget, Time.deltaTime * uiSmoothSpeed);
        abilityVisual = Mathf.Lerp(abilityVisual, abilityTarget, Time.deltaTime * uiSmoothSpeed);

        if (staminaSlider != null)
        {
            staminaSlider.SetValue(staminaVisual);
        }

        if (abilitySlider != null)
        {
            abilitySlider.SetValue(abilityVisual);
        }
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

            StopChargeVFX();
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

            if (chargeReadyObject != null)
            {
                chargeReadyObject.SetActive(true);

                //SoundManager.Instance?.PlayChargeReady();
                SoundManager.Instance?.PlayChargeLoop(true);
            }

            if (animator != null)
            {
                animator.SetBool("isCharging", true);
            }

            Camera.main.GetComponent<ThirdPersonCamera>()?.SetChargeZoom(true);
            
        }

        // full charge effect
        if (chargeStarted && chargeTimer >= chargeTime)
        {
            if (chargeFullObject != null && !chargeFullObject.activeSelf)
            {
                chargeFullObject.SetActive(true);
            }
        }
    }

    private void StopChargeVFX()
    {
        if (chargeReadyObject != null)
        {
            chargeReadyObject.SetActive(false);
        }

        if (chargeFullObject != null)
        {
            chargeFullObject.SetActive(false);
        }

        SoundManager.Instance?.PlayChargeLoop(false);
        SoundManager.Instance?.PlayChargeReady();

        if (animator != null)
        {
            animator.SetBool("isCharging", false);
        }

        Camera.main.GetComponent<ThirdPersonCamera>()?.SetChargeZoom(false);

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

    private void PlayAttackVFX(GameObject vfx)
    {
        if (currentVFX != null)
        {
            Destroy(currentVFX);
        }

        if (vfx == null || vfxSpawnPoint == null)
        {
            return;
        }

        currentVFX = Instantiate(
            vfx,
            vfxSpawnPoint.position,
            vfxSpawnPoint.rotation
        );
    }

    private IEnumerator PlayVFXDelayed(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);

        PlayAttackVFX(vfx);
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

            StartCoroutine(PlayVFXDelayed(attack1VFX, attack1VFXTime));

            SoundManager.Instance?.PlayAttack1Swing();
        }
        else if (step == 2)
        {
            animator.SetTrigger(animAttack2);
            currentAttackDamage = attack2Damage;
            attackDuration = 0.45f;

            StartCoroutine(PlayVFXDelayed(attack2VFX, attack2VFXTime));

            SoundManager.Instance?.PlayAttack2Swing();
        }
        else if (step == 3)
        {
            animator.SetTrigger(animAttack3);
            currentAttackDamage = attack3Damage;
            attackDuration = attack3Duration;
            isInvulnerable = true;

            StartCoroutine(PlayVFXDelayed(attack3VFX, attack3VFXTime));

            SoundManager.Instance?.PlayAttack3Swing();
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

        SoundManager.Instance?.PlayChargedSwing();

        if (weaponHitbox != null)
        {
            weaponHitbox.ResetHits();
        }

        Camera.main.GetComponent<ThirdPersonCamera>()?.TriggerChargeKick();

        StartCoroutine(PlayVFXDelayed(chargedAttackVFX, chargedAttackVFXTime));
        StartCoroutine(StopChargeLoopDelayed());

        yield return new WaitForSeconds(0.8f);

        isAttacking = false;
        isInvulnerable = false;
        
    }

    private IEnumerator StopChargeLoopDelayed()
    {
        yield return new WaitForSeconds(chargeLoopStopTime);

        StopChargeVFX();
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

        SoundManager.Instance?.PlayAirLand();

        StartCoroutine(PlayVFXDelayed(airAttackVFX, airAttackVFXTime));

        // apply damage on landing
        Collider[] hits = Physics.OverlapSphere(transform.position, airAttackRadius);

        foreach (Collider hit in hits)
        {
            EnemyScript enemy = hit.GetComponent<EnemyScript>();

            if (enemy != null)
            {
                enemy.TakeDamage(airAttackDamage);

                AddAbilityCharge(abilityGainPerHit);
            }
        }

        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
        isInvulnerable = false;
    }

    private void UseAbility()
    {
        currentAbilityCharge = 0f;

        SoundManager.Instance?.PlayAbilityUse();
    }

    public void AddAbilityCharge(float amount)
    {
        currentAbilityCharge += amount;

        if (currentAbilityCharge > maxAbilityCharge)
        {
            currentAbilityCharge = maxAbilityCharge;
        }
    }

    public float GetAbilityChargeNormalized()
    {
        return currentAbilityCharge / maxAbilityCharge;
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

    public bool IsCharging()
    {
        return isCharging;
    }
}