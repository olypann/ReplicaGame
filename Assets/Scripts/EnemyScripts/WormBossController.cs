using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class WormBossController : MonoBehaviour
{
    [Header("target")]
    [SerializeField] private Transform player;

    [Header("body parts (0 = head, rest = body)")]
    [SerializeField] private List<Transform> parts = new List<Transform>();

    [Header("orbit movement")]
    [SerializeField] private float orbitRadius = 6f;
    [SerializeField] private float orbitHeight = 3f;
    [SerializeField] private float orbitSpeed = 1.5f;

    [Header("movement")]
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float segmentDistance = 1.2f;

    [Header("dive attack")]
    [SerializeField] private float diveSpeed = 18f;
    [SerializeField] private float diveDuration = 1.2f;

    [Header("split attack")]
    [SerializeField] private float splitDuration = 4f;
    [SerializeField] private float splitForce = 6f;

    private float orbitT;
    private float attackTimer;

    private bool isDiving;
    private bool isSplitting;

    private bool isSplit;

    private Vector3 diveTarget;

    private Coroutine attackRoutine;

    private void Start()
    {
        orbitT = 0f;
        attackTimer = Random.Range(2f, 4f);
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        Orbit();              // ALWAYS running (no stopping ever)
        UpdateChain();       // ALWAYS running

        HandleAttackLoop();  // decides when attacks happen
    }

    // =========================
    // ORBIT (continuous motion)
    // =========================
    private void Orbit()
    {
        orbitT += Time.deltaTime * orbitSpeed;

        Vector3 center = player.position;

        Vector3 offset = new Vector3(
            Mathf.Cos(orbitT),
            0f,
            Mathf.Sin(orbitT)
        ) * orbitRadius;

        Vector3 targetPos = center + offset;
        targetPos.y += orbitHeight;

        MoveHead(targetPos);
    }

    private void MoveHead(Vector3 pos)
    {
        pos = ApplyGroundClearance(pos);

        Transform head = parts[0];

        head.position = Vector3.Lerp(
            head.position,
            pos,
            Time.deltaTime * followSpeed
        );
    }

    // =========================
    // ATTACK LOOP (non-blocking)
    // =========================
    private void HandleAttackLoop()
    {
        if (isDiving || isSplitting)
        {
            return; // attacks don’t stack
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
        {
            return;
        }

        attackTimer = Random.Range(2f, 4f);

        float roll = Random.value;

        if (roll < 0.5f)
        {
            attackRoutine = StartCoroutine(DiveAttack());
        }
        else
        {
            attackRoutine = StartCoroutine(SplitAttack());
        }
    }

    // =========================
    // DIVE ATTACK
    // =========================
    private IEnumerator DiveAttack()
    {
        isDiving = true;

        Transform head = parts[0];

        Vector3 target = player.position + Vector3.down * 1f;

        float t = 0f;

        while (t < diveDuration)
        {
            t += Time.deltaTime;

            Vector3 pos = Vector3.MoveTowards(
                head.position,
                target,
                diveSpeed * Time.deltaTime
            );

            pos = ApplyGroundClearance(pos);

            head.position = pos;

            yield return null;
        }

        isDiving = false;
    }

    // =========================
    // SPLIT ATTACK
    // =========================
    private IEnumerator SplitAttack()
    {
        isSplitting = true;
        isSplit = true;

        foreach (Transform p in parts)
        {
            Rigidbody rb = p.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddForce(Random.insideUnitSphere * splitForce, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(splitDuration);

        isSplit = false;
        isSplitting = false;
    }

    // =========================
    // CHAIN FOLLOW
    // =========================
    private void UpdateChain()
    {
        if (isSplit)
        {
            return;
        }

        Transform prev = parts[0];

        for (int i = 1; i < parts.Count; i++)
        {
            Transform part = parts[i];

            Vector3 target = prev.position;

            Vector3 dir = (part.position - target).normalized;

            Vector3 followPos = target + dir * segmentDistance;

            part.position = Vector3.Lerp(
                part.position,
                followPos,
                Time.deltaTime * followSpeed
            );

            prev = part;
        }
    }

    // =========================
    // GROUND STABILITY
    // =========================
    private Vector3 ApplyGroundClearance(Vector3 position)
    {
        RaycastHit hit;

        Vector3 origin = position + Vector3.up * 50f;

        if (Physics.Raycast(origin, Vector3.down, out hit, 200f))
        {
            float targetHeight = hit.point.y + orbitHeight;

            position.y = Mathf.Lerp(position.y, targetHeight, Time.deltaTime * 10f);
        }

        return position;
    }


    public void DamagePart(int index, float dmg)
    {
        if (index < 0 || index >= parts.Count)
        {
            return;
        }

        WormBossPartHealth hp = parts[index].GetComponent<WormBossPartHealth>();

        if (hp != null)
        {
            hp.TakeDamage(dmg);
        }
    }
}