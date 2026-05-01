using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombat : MonoBehaviour
{
    [Header("stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f;

    [SerializeField] private Unity.UI.Shaders.Sample.CustomSlider staminaSlider;
    [SerializeField] private Unity.UI.Shaders.Sample.CustomSlider abilitySlider;

    [SerializeField] private float uiSmoothSpeed = 8f;

    [SerializeField] private float lightAttackCost = 10f;

    public float currentStamina;

    private float staminaVisual;
    private float abilityVisual;



    [Header("ability charge")]
    [SerializeField] private float maxAbilityCharge = 100f;
    [SerializeField] public float abilityGainPerHit = 10f;

    public float currentAbilityCharge;



    [Header("combo")]
    [SerializeField] private float combo1ResetTime = 0.6f;
    [SerializeField] private float combo2ResetTime = 0.8f;

    private int comboStep = 0;
    private float comboTimer;



    [Header("attack damage")]
    [SerializeField] private float attack1Damage = 10f;
    [SerializeField] private float attack2Damage = 18f;
    [SerializeField] private float attack3Damage = 30f;
    [SerializeField] private float attack3Duration = 0.6f;



    [Header("attack vfx")]
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



    [Header("charge vfx")]
    [SerializeField] private GameObject chargeReadyObject;
    [SerializeField] private GameObject chargeFullObject;
    [SerializeField] private float chargeLoopStopTime = 0.3f;



    [Header("charged attack")]
    [SerializeField] private float chargeHoldThreshold = 0.25f;
    [SerializeField] private float chargeTime = 1.2f;
    [SerializeField] private float chargedDamage = 50f;



    [Header("air attack")]
    [SerializeField] private float airAttackDamage = 25f;
    [SerializeField] private float airAttackForce = 25f;
    [SerializeField] private float airAttackRadius = 3f;



    [Header("animation")]
    private Animator animator;

    private int animAttack1;
    private int animAttack2;
    private int animAttack3;
    private int animCharge;
    private int animAirAttack;

    private int animChargeStart;
    private int animChargeRelease;



    private WeaponScript weaponHitbox;
    private CharacterController controller;

    private ThirdPersonCamera cam;
    private PlayerPossessedAI playerAI;



    private bool attackQueued;
    private bool isAttacking;
    private bool canCombo;
    private bool isInvulnerable;

    private bool isCharging;
    private float chargeTimer;
    private bool chargeStarted;

    private bool didAirAttack;



    public int currentAttackStep;
    public float currentAttackDamage;

    private float nextAttackAnimTime;



    private void Start()
    {
        playerAI = GetComponent<PlayerPossessedAI>();
        cam = Camera.main.GetComponent<ThirdPersonCamera>();

        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        weaponHitbox = GetComponentInChildren<WeaponScript>();

        currentStamina = maxStamina;
        currentAbilityCharge = 0f;

        //animator params
        animAttack1 = Animator.StringToHash("Attack1");
        animAttack2 = Animator.StringToHash("Attack2");
        animAttack3 = Animator.StringToHash("Attack3");
        animCharge = Animator.StringToHash("ChargeAttack");
        animAirAttack = Animator.StringToHash("AirAttack");

        animChargeStart = Animator.StringToHash("ChargeStart");
        animChargeRelease = Animator.StringToHash("ChargeRelease");

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
        // block during camera throw
        if (AbilityStateManager.Instance != null &&
            AbilityStateManager.Instance.isCameraThrowActive)
        {
            return;
        }
        

        // freeze mode unlocks cursor
        if (AbilityStateManager.Instance != null &&
            AbilityStateManager.Instance.isFreezeAbilityActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        HandleStamina();
        HandleComboTimer();

        HandleAttackInput();
        HandleCharge();

        UpdateUI();
    }



    private void UpdateUI()
    {
        float staminaTarget = currentStamina / maxStamina;
        float abilityTarget = currentAbilityCharge / maxAbilityCharge;

        staminaVisual = Mathf.Lerp(
            staminaVisual,
            staminaTarget,
            Time.deltaTime * uiSmoothSpeed
        );

        abilityVisual = Mathf.Lerp(
            abilityVisual,
            abilityTarget,
            Time.deltaTime * uiSmoothSpeed
        );

        if (staminaSlider != null)
        {
            staminaSlider.SetValue(staminaVisual);
        }

        if (abilitySlider != null)
        {
            abilitySlider.SetValue(abilityVisual);
        }
    }



    // stamina regen over time
    private void HandleStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
        }

        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }



    // resets combo if player waits too long
    private void HandleComboTimer()
    {
        if (comboStep == 1 || comboStep == 2)
        {
            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0f)
            {
                comboStep = 0;
            }
        }
    }



    // mouse input + combo logic entry point
    private void HandleAttackInput()
    {
        if (playerAI != null && playerAI.isActive)
        {
            return;
        }

        bool isGrounded = controller.isGrounded;

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time < nextAttackAnimTime)
            {
                return;
            }

            if (!isGrounded)
            {
                StartCoroutine(DoAirAttack());
                return;
            }

            // start charge tracking
            chargeTimer = 0f;
            chargeStarted = false;
            isCharging = true;

            if (isAttacking)
            {
                if (canCombo)
                {
                    attackQueued = true;
                    nextAttackAnimTime = Time.time + 0.12f;
                }

                return;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!isGrounded)
            {
                return;
            }

            // release either charge or normal attack
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
        if (!isCharging)
        {
            return;
        }

        chargeTimer += Time.deltaTime;

        // reached hold threshold, start charge state
        if (!chargeStarted && chargeTimer >= chargeHoldThreshold)
        {
            chargeStarted = true;

            animator.SetTrigger(animChargeStart);

            if (chargeReadyObject != null)
            {
                chargeReadyObject.SetActive(true);
                SoundManager.Instance?.PlayChargeLoop(true);
            }

            if (animator != null)
            {
                animator.SetBool("isCharging", true);
            }

            cam?.SetChargeZoom(true);
        }


        // fully charged
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

        cam?.SetChargeZoom(false);
    }



    private void TryAttack()
    {
        if (currentStamina < lightAttackCost)
        {
            return;
        }

        currentStamina -= lightAttackCost;

        comboStep++;

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



    private IEnumerator DoAttack(int step)
    {
        isAttacking = true;
        attackQueued = false;
        canCombo = false;

        currentAttackStep = step;

        weaponHitbox?.ResetHits();

        isInvulnerable = false;

        float attackDuration = 0.4f;

        animator.ResetTrigger(animAttack1);
        animator.ResetTrigger(animAttack2);
        animator.ResetTrigger(animAttack3);

        // pick attack based on combo step
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

        // continue combo if queued
        if (attackQueued)
        {
            attackQueued = false;
            TryAttack();
        }
    }



    private IEnumerator DoChargedAttack()
    {
        isAttacking = true;
        isInvulnerable = true;

        animator.SetTrigger(animCharge);
        currentAttackDamage = chargedDamage;

        SoundManager.Instance?.PlayChargedSwing();

        weaponHitbox?.ResetHits();

        cam?.TriggerChargeKick();

        StartCoroutine(PlayVFXDelayed(chargedAttackVFX, chargedAttackVFXTime));
        StartCoroutine(StopChargeLoopDelayed());

        yield return new WaitForSeconds(0.8f);

        isAttacking = false;
        isInvulnerable = false;
    }



    private IEnumerator DoAirAttack()
    {
        isAttacking = true;
        isInvulnerable = true;
        didAirAttack = true;

        animator.SetTrigger(animAirAttack);

        // force downward until landing
        while (!controller.isGrounded)
        {
            controller.Move(
                Vector3.down * airAttackForce * Time.deltaTime
            );

            yield return null;
        }

        SoundManager.Instance?.PlayAirLand();

        StartCoroutine(PlayVFXDelayed(airAttackVFX, airAttackVFXTime));

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            airAttackRadius
        );

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



    private IEnumerator PlayVFXDelayed(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayAttackVFX(vfx);
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



    private IEnumerator StopChargeLoopDelayed()
    {
        yield return new WaitForSeconds(chargeLoopStopTime);
        StopChargeVFX();
    }



    // ability charge helpers

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

    public bool HasFullAbilityCharge()
    {
        return currentAbilityCharge >= maxAbilityCharge;
    }

    public void ConsumeAbilityCharge()
    {
        currentAbilityCharge = 0f;
    }



    //state getters
    public bool IsAttacking()
    {
        return isAttacking;
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

    public float GetCurrentDamage()
    {
        return currentAttackDamage;
    }



    //helper for forced attacks
    public void ForceAttack()
    {
        if (isAttacking)
        {
            return;
        }

        TryAttack();
    }
}