using System.Collections;
using UnityEngine;

public class EnemyCameraAttacker : MonoBehaviour
{
    [Header("target")]
    [SerializeField] private Transform cameraTarget;
    private CameraEntity cameraEntity;

    [SerializeField] private Transform player;
    [SerializeField] private float keepDistanceFromPlayer = 6f;

    [Header("movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stopDistance = 2f;

    [Header("combat")]
    [SerializeField] private float attackCooldown = 2f;

    [Header("attack settings")]
    [SerializeField] private float screenDamageAmount = 5f;

    [Header("projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float projectileSpeed = 12f;

    [Header("debug attack select")]
    [SerializeField] private int attackType = 1; // 0 rotate, 1 push,2 screen

    private float cooldown;

    private void Start()
    {
        
        // if (cameraTarget != null)
        // {
        //     cameraEntity = cameraTarget.GetComponent<CameraEntity>();
        // }

        if (cameraTarget == null)
        {
            CameraEntity camEntity = FindFirstObjectByType<CameraEntity>();

            if (camEntity != null)
            {
                cameraTarget = camEntity.transform;
                cameraEntity = camEntity;
            }
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void Update()
    {

        EnemyPossessionController poss = GetComponent<EnemyPossessionController>();
        if (poss != null && poss.IsPossessed())
        {
            return;
        }
        
        if (cameraTarget == null)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        // debug switching
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            attackType = 0;
            Debug.Log("camera enemy attack: rotate");
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            attackType = 2;
            Debug.Log("camera enemy attack: screen damage");
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            attackType = 1;
            Debug.Log("camera enemy attack: push");
        }

        cooldown -= Time.deltaTime;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        //stop near player
        if (distToPlayer > keepDistanceFromPlayer)
        {
            Vector3 dir = (player.position - transform.position).normalized;

            transform.position += dir * moveSpeed * Time.deltaTime;
        }
        else
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (cooldown > 0f)
        {
            return;
        }

        if (CrosshairEvents.Instance != null)
        {
            CrosshairEvents.Instance.TriggerCameraTarget();
        }

        cooldown = attackCooldown;

        StartCoroutine(DoAttack());
    }

    private IEnumerator DoAttack()
    {
        if (cameraTarget == null || projectilePrefab == null || shootPoint == null)
        {
            yield break;
        }

        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);

        Vector3 dir = (cameraTarget.position - shootPoint.position).normalized;

        Projectile projectile = proj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Init(dir, projectileSpeed, attackType, screenDamageAmount);
        }

        yield return null;
    }
}