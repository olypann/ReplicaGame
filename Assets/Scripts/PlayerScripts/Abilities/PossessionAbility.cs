using System.Collections;
using UnityEngine;
using TMPro;

public class PossessionAbility : MonoBehaviour
{
    [Header("settings")]
    [SerializeField] private float maxChargeRequired = 100f;
    [SerializeField] private float possessionDuration = 15f;

    [SerializeField] private TMP_Text abilityText;
    [SerializeField] private TextMeshProUGUI timerText;


    //refs

    private PlayerMovement movement;
    private PlayerCombat combat;
    private ThirdPersonCamera cam;
    private PlayerPossessedAI playerAI;


    // runtime

    private bool isPossessing;
    private float possessionTimer;

    private EnemyPossessionController currentPossessed;

    public bool IsPossessing => isPossessing;



    private void Start()
    {
        combat = GetComponent<PlayerCombat>();
        cam = Camera.main.GetComponent<ThirdPersonCamera>();
        movement = GetComponent<PlayerMovement>();
        playerAI = GetComponent<PlayerPossessedAI>();
    }


    private void Update()
    {
        HandleInput();
        HandlePossessionUpdate();
    }



    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // block if another ability is running
            if (AbilityStateManager.Instance != null &&
                AbilityStateManager.Instance.IsAnyAbilityActive())
            {
                return;
            }

            if (isPossessing)
            {
                return;
            }

            if (combat == null)
            {
                return;
            }

            // need full charge
            if (!combat.HasFullAbilityCharge())
            {
                return;
            }

            combat.ConsumeAbilityCharge();
            StartPossession();
        }

        if (!isPossessing)
        {
            return;
        }

        // switch target while active
        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchTarget();
        }
    }



    private void StartPossession()
    {
        AbilityStateManager.Instance.isAbilityActive = true;

        isPossessing = true;
        possessionTimer = possessionDuration;

        // immediately grab something
        SwitchTarget();

        if (abilityText != null)
        {
            abilityText.text = "Press 'E' to Possess A Different Enemy";
        }

        if (timerText != null)
        {
            timerText.text = Mathf.Ceil(possessionTimer).ToString();
        }

        // enable player ai override while possessing
        if (playerAI != null)
        {
            playerAI.enabled = true;
            playerAI.SetActive(true, transform);
        }

        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isAbilityActive = true;
        }
    }



    private void HandlePossessionUpdate()
    {
        if (!isPossessing)
        {
            return;
        }

        possessionTimer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = Mathf.Ceil(possessionTimer).ToString();
        }

        if (possessionTimer <= 0f)
        {
            EndPossession();
        }
    }



    private void EndPossession()
    {
        AbilityStateManager.Instance.isAbilityActive = false;

        isPossessing = false;

        // release current enemy
        if (currentPossessed != null)
        {
            currentPossessed.SetPossessed(false, null);
            currentPossessed = null;
        }

        // reset camera back to player
        if (cam != null)
        {
            cam.RestoreNormalControl();
            cam.SnapToTargetInstant(transform);
        }

        if (timerText != null)
        {
            timerText.text = "";
        }

        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isAbilityActive = false;
        }

        // disable ai override
        if (playerAI != null)
        {
            playerAI.SetActive(false, null);
            playerAI.enabled = false;
        }

        if (abilityText != null)
        {
            abilityText.text = "";
        }
    }



    // finds closest valid enemy and switches to it
    private void SwitchTarget()
    {
        EnemyScript[] enemies = FindObjectsOfType<EnemyScript>();

        float closestDist = Mathf.Infinity;
        EnemyScript closest = null;

        foreach (EnemyScript e in enemies)
        {
            if (e == null)
            {
                continue;
            }

            EnemyPossessionController ep = e.GetComponent<EnemyPossessionController>();

            // skip already possessed enemies
            if (ep != null && ep.IsPossessed())
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, e.transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = e;
            }
        }

        if (closest == null)
        {
            return;
        }

        // release previous
        if (currentPossessed != null)
        {
            currentPossessed.SetPossessed(false, null);
        }

        EnemyPossessionController controller = closest.GetComponent<EnemyPossessionController>();

        if (controller == null)
        {
            controller = closest.gameObject.AddComponent<EnemyPossessionController>();
        }

        controller.SetPossessed(true, Camera.main.transform);

        currentPossessed = controller;

        if (cam != null)
        {
            cam.SetTarget(closest.transform);
        }
    }



    private void LateUpdate()
    {
        ValidateCurrentPossession();
    }


    // makes sure current target is still valid
    private void ValidateCurrentPossession()
    {
        if (!isPossessing)
        {
            return;
        }

        if (!currentPossessed.IsValidTarget())
        {
            SwitchTargetOrEnd();
        }

        if (currentPossessed == null)
        {
            SwitchTargetOrEnd();
            return;
        }

        EnemyScript enemy = null;

        if (currentPossessed != null)
        {
            enemy = currentPossessed.GetComponent<EnemyScript>();
        }

        // if enemy died or invalid, switch or end
        if (enemy == null || enemy.IsDead())
        {
            SwitchTargetOrEnd();
        }
    }



    private void SwitchTargetOrEnd()
    {
        EnemyScript[] enemies = FindObjectsOfType<EnemyScript>();

        EnemyScript next = null;
        float closest = Mathf.Infinity;

        foreach (var e in enemies)
        {
            if (e == null)
            {
                continue;
            }

            EnemyPossessionController ep = e.GetComponent<EnemyPossessionController>();

            if (ep != null && ep.IsPossessed())
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, e.transform.position);

            if (dist < closest)
            {
                closest = dist;
                next = e;
            }
        }

        if (next != null)
        {
            SwitchTo(next);
            return;
        }

        //  end ability
        cam?.RestoreNormalControl();
        cam?.SnapToTargetInstant(transform);

        EndPossession();
    }



    private void SwitchTo(EnemyScript target)
    {
        SoundManager.Instance?.PlayPossessionSwitch();

        if (currentPossessed != null)
        {
            currentPossessed.SetPossessed(false, null);
        }

        EnemyPossessionController controller = target.GetComponent<EnemyPossessionController>();

        if (controller == null)
        {
            controller = target.gameObject.AddComponent<EnemyPossessionController>();
        }

        controller.SetPossessed(true, Camera.main.transform);

        currentPossessed = controller;

        cam?.SnapToTargetInstant(target.transform);
    }
}