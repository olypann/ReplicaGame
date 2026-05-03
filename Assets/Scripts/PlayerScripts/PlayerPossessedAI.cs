using UnityEngine;

public class PlayerPossessedAI : MonoBehaviour
{
    [Header("ai")]
    [SerializeField] private float followDistance = 2.5f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCheckRate = 0.2f;


    private Transform target;

    private PlayerCombat combat;
    private CharacterController controller;

    private float attackTimer;

    public bool isActive;



    private void Start()
    {
        combat = GetComponent<PlayerCombat>();
        controller = GetComponent<CharacterController>();

        // stays disabled until possession enables it
        enabled = false;
    }


    public void SetActive(bool state, Transform followTarget)
    {
        isActive = state;
        target = followTarget;
    }



    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        HandleMovement();
        HandleAttack();
    }



    private void HandleMovement()
    {
        EnemyScript closest = FindClosestEnemy();

        // default is follow original target
        Vector3 moveTarget = target.position;

        // if enemy found, override target
        if (closest != null)
        {
            moveTarget = closest.transform.position;
        }

        Vector3 dir = moveTarget - transform.position;
        dir.y = 0f;

        float distance = dir.magnitude;

        if (closest != null)
        {
            // stop slightly before reaching attack range
            if (distance <= attackRange * 0.9f)
            {
                FaceTarget(dir);
                return;
            }
        }

        else
        {
            // just following player
            if (distance < followDistance)
            {
                return;
            }
        }

        Vector3 move = dir.normalized * moveSpeed;

        if (controller != null)
        {
            controller.Move(move * Time.deltaTime);
        }

        FaceTarget(move);
    }



    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
        {
            return;
        }

        attackTimer = attackCheckRate;

        EnemyScript closest = FindClosestEnemy();

        if (closest == null)
        {
            return;
        }

        float dist = Vector3.Distance(transform.position, closest.transform.position);

        if (dist <= attackRange)
        {
            // force player combat to attack as ai
            combat.ForceAttack();
        }
    }



    private void FaceTarget(Vector3 dir)
    {
        if (dir == Vector3.zero)
        {
            return;
        }

        dir.y = 0f;

        transform.rotation = Quaternion.LookRotation(dir);
    }



    //simple nearest enemy search
    private EnemyScript FindClosestEnemy()
    {
        EnemyScript[] enemies = FindObjectsOfType<EnemyScript>();

        EnemyScript closest = null;
        float bestDist = Mathf.Infinity;

        foreach (EnemyScript e in enemies)
        {
            if (e == null)
            {
                continue;
            }

            float d = Vector3.Distance(transform.position, e.transform.position);

            if (d < bestDist)
            {
                bestDist = d;
                closest = e;
            }
        }

        return closest;
    }
}