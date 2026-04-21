using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Player Movement")]
    [SerializeField] private AudioSource jump;
    [SerializeField] private AudioSource land;
    [SerializeField] private AudioSource dodge;

    [Header("Footsteps")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] footstepClips;

    [SerializeField] private float walkStepDelay = 0.5f;
    [SerializeField] private float runStepDelay = 0.3f;

    private float footstepTimer;
    private int lastFootstepIndex = -1;

    [Header("Combat - Attacks")]
    [SerializeField] private AudioSource attack1Swing;
    [SerializeField] private AudioSource attack1Hit;

    [SerializeField] private AudioSource attack2Swing;
    [SerializeField] private AudioSource attack2Hit;

    [SerializeField] private AudioSource attack3Swing;
    [SerializeField] private AudioSource attack3Hit;

    [Header("Charged Attack")]
    [SerializeField] private AudioSource chargeLoop;
    [SerializeField] private AudioSource chargeReady;
    [SerializeField] private AudioSource chargedSwing;
    [SerializeField] private AudioSource chargedHit;

    [Header("Air Attack")]
    [SerializeField] private AudioSource airLandGround;
    [SerializeField] private AudioSource airHitEnemy;

    [Header("Player States")]
    [SerializeField] private AudioSource playerHit;
    [SerializeField] private AudioSource playerDeath;

    [Header("Ability")]
    [SerializeField] private AudioSource abilityFull;
    [SerializeField] private AudioSource abilityUse;

    private void Awake()
    {
        Instance = this;
    }

    // footsteps
    public void HandleFootsteps(float speedNormalized, bool isMoving)
    {
        if (!isMoving)
        {
            footstepTimer = 0f;
            return;
        }

        float stepDelay = Mathf.Lerp(walkStepDelay, runStepDelay, speedNormalized);

        footstepTimer -= Time.deltaTime;

        if (footstepTimer > 0f)
        {
            return;
        }

        footstepTimer = stepDelay;

        PlayRandomFootstep();
    }

    private void PlayRandomFootstep()
    {
        if (footstepClips.Length == 0 || footstepSource == null)
        {
            return;
        }

        int index = Random.Range(0, footstepClips.Length);

        if (index == lastFootstepIndex)
        {
            index = (index + 1) % footstepClips.Length;
        }

        lastFootstepIndex = index;

        footstepSource.pitch = Random.Range(0.9f, 1.1f);

        footstepSource.PlayOneShot(footstepClips[index]);
    }

    // movement
    public void PlayJump() => jump?.Play();
    public void PlayLand() => land?.Play();
    public void PlayDodge() => dodge?.Play();

    // attacks
    public void PlayAttack1Swing() => attack1Swing?.Play();
    public void PlayAttack1Hit() => attack1Hit?.Play();

    public void PlayAttack2Swing() => attack2Swing?.Play();
    public void PlayAttack2Hit() => attack2Hit?.Play();

    public void PlayAttack3Swing() => attack3Swing?.Play();
    public void PlayAttack3Hit() => attack3Hit?.Play();

    // charged
    public void PlayChargeLoop(bool state)
    {
        if (state)
        {
            chargeLoop?.Play();
        }
        else
        {
            chargeLoop?.Stop();
        }
    }

    public void PlayChargeReady() => chargeReady?.Play();
    public void PlayChargedSwing() => chargedSwing?.Play();
    public void PlayChargedHit() => chargedHit?.Play();

    // air attack
    public void PlayAirLand() => airLandGround?.Play();
    public void PlayAirHit() => airHitEnemy?.Play();

    // player states
    public void PlayPlayerHit() => playerHit?.Play();
    public void PlayPlayerDeath() => playerDeath?.Play();

    // ability
    public void PlayAbilityFull() => abilityFull?.Play();
    public void PlayAbilityUse() => abilityUse?.Play();
}