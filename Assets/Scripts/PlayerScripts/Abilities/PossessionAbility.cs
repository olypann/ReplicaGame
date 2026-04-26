using System.Collections;
using UnityEngine;

public class PossessionAbility : MonoBehaviour
{
    [Header("settings")]
    [SerializeField] private float maxChargeRequired = 100f;
    [SerializeField] private float possessionDuration = 15f;

    private PlayerCombat combat;
    private ThirdPersonCamera cam;

    private bool isPossessing;
    private float possessionTimer;

    private EnemyPossessionController currentPossessed;

    public bool IsPossessing => isPossessing;

    private void Start()
    {
        combat = GetComponent<PlayerCombat>();
        cam = Camera.main.GetComponent<ThirdPersonCamera>();
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
            if (isPossessing)
            {
                return;
            }

            if (combat == null)
            {
                return;
            }

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

        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchTarget();
        }
    }

    private void StartPossession()
    {
        isPossessing = true;
        possessionTimer = possessionDuration;

        SwitchTarget();

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

        if (possessionTimer <= 0f)
        {
            EndPossession();
        }
    }

    private void EndPossession()
    {
        isPossessing = false;

        if (currentPossessed != null)
        {
            currentPossessed.SetPossessed(false, null);
            currentPossessed = null;
        }

        if (cam != null)
        {
            cam.SetTarget(transform);
        }

        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isAbilityActive = false;
        }
    }

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
}