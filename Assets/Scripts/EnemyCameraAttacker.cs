using System.Collections;
using UnityEngine;

public class EnemyCameraAttacker : MonoBehaviour
{
    [Header("target")]
    [SerializeField] private Transform cameraTarget;
    private CameraEntity cameraEntity;

    [Header("movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stopDistance = 2f;

    [Header("combat")]
    [SerializeField] private float attackCooldown = 2f;

    [Header("attack settings")]
    [SerializeField] private float screenDamageAmount = 5f;

    [Header("debug attack select")]
    [SerializeField] private int attackType = 1; // 0=rotate, 1=push, 2=screen

    private float cooldown;

    private void Start()
    {
        if (cameraTarget != null)
        {
            cameraEntity = cameraTarget.GetComponent<CameraEntity>();
        }
    }

    private void Update()
    {
        if (cameraTarget == null)
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

        float dist = Vector3.Distance(transform.position, cameraTarget.position);

        if (dist > stopDistance)
        {
            Vector3 dir = (cameraTarget.position - transform.position).normalized;

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

        cooldown = attackCooldown;

        StartCoroutine(DoAttack());
    }

    private IEnumerator DoAttack()
    {
        if (cameraEntity == null)
        {
            yield break;
        }

        Vector3 dir = (cameraTarget.position - transform.position).normalized;

        if (attackType == 0)
        {
            cameraEntity.HitRotate(dir);
        }
        else if (attackType == 1)
        {
            cameraEntity.HitPush(dir);
        }
        else if (attackType == 2)
        {
            cameraEntity.HitScreen(screenDamageAmount);
        }

        yield return null;
    }
}