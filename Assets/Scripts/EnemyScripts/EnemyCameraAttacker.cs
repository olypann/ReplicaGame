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
    [SerializeField] private int attackType = 3;
    // 0 rotate, 1 push, 2 screen, 3 combo


    private float cooldown;



    private void Start()
    {
        // try auto-assign camera target if not set in inspector
        if (cameraTarget == null)
        {
            CameraEntity camEntity = FindFirstObjectByType<CameraEntity>();

            if (camEntity != null)
            {
                cameraTarget = camEntity.transform;
                cameraEntity = camEntity;
            }

        }

        // fallback player reference if scene setup missed it
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

        // if enemy is currently possessed, AI should fully stop
        if (poss != null && poss.IsPossessed())
        {
            return;
        }

        // safety checks so we don't run AI without required refs
        if (cameraTarget == null)
        {
            return;
        }

        if (player == null)
        {
            return;
        }




        // debug switching for quickly testing attack behaviours
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            attackType = 0;
            Debug.Log("camera enemy attack: rotate");
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            attackType = 1;
            Debug.Log("camera enemy attack: push");
        }


        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            attackType = 2;
            Debug.Log("camera enemy attack: screen damage");
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            attackType = 3;
            Debug.Log("camera enemy attack: push + screen damage");
        }



        cooldown -= Time.deltaTime;

        float distToPlayer = Vector3.Distance(transform.position, player.position);




        // keep distance first, otherwise go into attack state
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
        // basic cooldown gate so we don't spam attacks every frame
        if (cooldown > 0f)
        {
            return;
        }

        // optional crosshair feedback when enemy commits to attack
        if (CrosshairEvents.Instance != null)
        {
            CrosshairEvents.Instance.TriggerCameraTarget();
        }

        cooldown = attackCooldown;

        StartCoroutine(DoAttack());
    }



    private IEnumerator DoAttack()
    {
        // hard safety check so projectile logic doesn't break mid-fight
        if (cameraTarget == null || projectilePrefab == null || shootPoint == null)
        {
            yield break;
        }


        GameObject proj = Instantiate(
            projectilePrefab,
            shootPoint.position,
            Quaternion.identity
        );

        // shoot direction is always aimed at camera target
        Vector3 dir = (cameraTarget.position - shootPoint.position).normalized;

        Projectile projectile = proj.GetComponent<Projectile>();


        // pass all behaviour data into projectile so it handles impact logic itself
        if (projectile != null)
        {
            projectile.Init(dir, projectileSpeed, attackType, screenDamageAmount);
        }

        yield return null;
    }
}