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

    private bool isActive;

    private void Start()
    {
        combat = GetComponent<PlayerCombat>();
        controller = GetComponent<CharacterController>();
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
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        float distance = dir.magnitude;

        if (distance < followDistance)
        {
            return;
        }

        Vector3 move = dir.normalized * moveSpeed;

        if (controller != null)
        {
            controller.Move(move * Time.deltaTime);
        }

        if (move != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(move);
        }
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
            //combat.SendMessage("TryAttack", SendMessageOptions.DontRequireReceiver);
            combat.ForceAttack();
        }
    }

    private EnemyScript FindClosestEnemy()
    {
        EnemyScript[] enemies = FindObjectsOfType<EnemyScript>();

        EnemyScript closest = null;
        float bestDist = Mathf.Infinity;

        foreach (EnemyScript e in enemies)
        {
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