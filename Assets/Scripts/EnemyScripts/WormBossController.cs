using System.Collections.Generic;
using UnityEngine;

public class WormBossController : MonoBehaviour
{
    public enum BossState
    {
        Orbit_Fast,
        Orbit_Calm,
        Dive,
        Split
    }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Body Parts (0 = head)")]
    [SerializeField] private List<Transform> parts = new List<Transform>();

    [Header("Enemy Scripts on Parts")]
    [SerializeField] private List<EnemyScript> partEnemies = new List<EnemyScript>();

    [Header("Orbit Settings")]
    [SerializeField] private float fastRadius = 6f;
    [SerializeField] private float calmRadius = 4f;

    [SerializeField] private float fastHeight = 3f;
    [SerializeField] private float calmHeight = 1.5f;

    [SerializeField] private float fastSpeed = 2f;
    [SerializeField] private float calmSpeed = 0.7f;

    [Header("Movement")]
    [SerializeField] private float headLerpSpeed = 6f;
    [SerializeField] private float chainSpeed = 10f;
    [SerializeField] private float segmentDistance = 1.2f;

    [Header("Dive Attack")]
    [SerializeField] private float diveSpeed = 18f;
    [SerializeField] private float diveDuration = 1.2f;
    [SerializeField] private float diveDamageRange = 2.5f;
    [SerializeField] private float diveDamage = 20f;

    [Header("Split Attack")]
    [SerializeField] private float splitDuration = 4f;
    [SerializeField] private float splitForce = 6f;

    [Header("Ground Offset")]
    [SerializeField] private float groundOffset = 1.5f;

    [Header("State Timing")]
    [SerializeField] private float minStateTime = 2f;
    [SerializeField] private float maxStateTime = 4f;

    [Range(0f, 1f)]
    [SerializeField] private float calmChance = 0.4f;

    // runtime
    private BossState state;
    private float stateTimer;
    private float orbitT;

    private bool isSplitting;
    private bool diveHit;

    private Vector3 diveTarget;
    private Vector3[] splitVelocities;

    [SerializeField] private float maxHeightAboveGround = 3f;
    [SerializeField] private float heightClampSpeed = 10f;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }


        state = BossState.Orbit_Fast;
        stateTimer = Random.Range(minStateTime, maxStateTime);

        splitVelocities = new Vector3[Mathf.Max(1, parts.Count)];

        SetBossControl(true);

        Debug.Log("[Boss] START → ORBIT FAST");
    }

    private void Update()
    {
        CleanupDestroyedParts();

        if (player == null || parts.Count == 0)
            return;

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case BossState.Orbit_Fast:
            case BossState.Orbit_Calm:
                Orbit();
                HandleTransitions();
                break;

            case BossState.Dive:
                Dive();
                break;

            case BossState.Split:
                Split();
                break;
        }

        if (state != BossState.Split)
            UpdateChain();

        CheckBossDeath();
    }

    // ================= ORBIT =================

    private void Orbit()
    {
        bool calm = state == BossState.Orbit_Calm;

        float speed = calm ? calmSpeed : fastSpeed;
        float radius = calm ? calmRadius : fastRadius;
        float height = calm ? calmHeight : fastHeight;

        orbitT += Time.deltaTime * speed;

        Vector3 center = player.position;
        center.y = GetGroundY(center);

        Vector3 offset = new Vector3(
            Mathf.Cos(orbitT),
            0f,
            Mathf.Sin(orbitT)
        ) * radius;

        Vector3 target = center + offset;
        float groundY = GetGroundY(target);

        // desired height above ground
        float desiredY = groundY + height;

        // clamp against runaway floating
        float currentY = target.y;

        // smooth correction instead of snapping
        target.y = Mathf.Lerp(
            currentY,
            Mathf.Min(desiredY, groundY + maxHeightAboveGround),
            Time.deltaTime * heightClampSpeed
        );

        MoveHead(target);
    }

    private void MoveHead(Vector3 target)
    {
        Transform head = parts[0];

        head.position = Vector3.Lerp(
            head.position,
            target,
            Time.deltaTime * headLerpSpeed
        );

        Vector3 pos = head.position;

        float ground = GetGroundY(pos);
        float maxY = ground + maxHeightAboveGround;

        if (pos.y > maxY)
        {
            pos.y = Mathf.Lerp(pos.y, maxY, Time.deltaTime * 5f);
        }

        head.position = pos;
    }

    // ================= DIVE =================

    private void StartDive()
    {
        state = BossState.Dive;
        stateTimer = diveDuration;

        diveTarget = player.position;
        diveHit = false;

        Debug.Log("[Boss] STATE → DIVE");
    }

    private void Dive()
    {
        Transform head = parts[0];

        Vector3 target = diveTarget;
        target.y = GetGroundY(target) + groundOffset;

        head.position = Vector3.MoveTowards(
            head.position,
            target,
            diveSpeed * Time.deltaTime
        );

        if (!diveHit && Vector3.Distance(head.position, player.position) < diveDamageRange)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();

            if (stats != null)
            {
                stats.TakeDamage(diveDamage);
                Debug.Log("[Boss] DIVE HIT PLAYER");
            }

            diveHit = true;
        }

        if (stateTimer <= 0f)
        {
            SetOrbitFast();
        }
    }

    // ================= SPLIT =================

    private void StartSplit()
    {
        state = BossState.Split;
        stateTimer = splitDuration;
        isSplitting = true;

        SetBossControl(false); // 🔥 ENEMY AI ENABLED

        Debug.Log("[Boss] STATE → SPLIT (ENEMY AI ENABLED)");

        for (int i = 1; i < parts.Count; i++)
        {
            splitVelocities[i] = Random.insideUnitSphere * splitForce;
        }
    }

    private void Split()
    {
        Transform head = parts[0];

        Vector3 headTarget = player.position;
        headTarget.y = GetGroundY(headTarget) + fastHeight;

        head.position = Vector3.Lerp(
            head.position,
            headTarget,
            Time.deltaTime * headLerpSpeed
        );

        for (int i = 1; i < parts.Count; i++)
        {
            Transform part = parts[i];

            Vector3 dir = (player.position - part.position).normalized;
            Vector3 move = (dir + splitVelocities[i]).normalized;

            part.position += move * (headLerpSpeed * 0.7f) * Time.deltaTime;

            splitVelocities[i] = Vector3.Lerp(
                splitVelocities[i],
                Random.insideUnitSphere,
                Time.deltaTime * 0.5f
            );
        }

        if (stateTimer <= 0f)
        {
            isSplitting = false;
            SetBossControl(true); // 🔥 RETURN CONTROL

            SetOrbitFast();

            Debug.Log("[Boss] SPLIT END → ORBIT FAST");
        }
    }

    // ================= CHAIN =================

    private void UpdateChain()
    {
        if (parts == null || parts.Count == 0)
            return;

        Transform prev = parts[0];

        if (prev == null)
            return;

        for (int i = 1; i < parts.Count; i++)
        {
            Transform part = parts[i];

            if (part == null || prev == null)
                continue;

            Vector3 target = prev.position;

            Vector3 dir = (part.position - target).normalized;

            Vector3 follow = target + dir * segmentDistance;

            part.position = Vector3.Lerp(
                part.position,
                follow,
                Time.deltaTime * chainSpeed
            );

            prev = part;
        }
    }

    // ================= TRANSITIONS =================

    private void HandleTransitions()
    {
        if (stateTimer > 0f)
            return;

        stateTimer = Random.Range(minStateTime, maxStateTime);

        float roll = Random.value;

        if (roll < calmChance)
        {
            SetOrbitCalm();
        }
        else if (roll < 0.65f)
        {
            StartDive();
        }
        else
        {
            StartSplit();
        }
    }

    private void SetOrbitFast()
    {
        state = BossState.Orbit_Fast;
        Debug.Log("[Boss] STATE → ORBIT FAST");
    }

    private void SetOrbitCalm()
    {
        state = BossState.Orbit_Calm;
        Debug.Log("[Boss] STATE → ORBIT CALM");
    }

    // ================= CONTROL =================

    private void SetBossControl(bool value)
    {
        foreach (EnemyScript e in partEnemies)
        {
            if (e != null)
                e.controlledByBoss = value;
        }
    }

    // ================= GROUND =================

    private float GetGroundY(Vector3 pos)
    {
        RaycastHit hit;

        Vector3 origin = pos + Vector3.up * 50f;

        if (Physics.Raycast(origin, Vector3.down, out hit, 200f))
            return hit.point.y;

        return pos.y;
    }

    private void CleanupDestroyedParts()
    {
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            if (parts[i] == null)
            {
                parts.RemoveAt(i);

                if (i < partEnemies.Count)
                    partEnemies.RemoveAt(i);

                Debug.Log("[Boss] Removed destroyed part safely");
            }
        }
    }


    private void CheckBossDeath()
    {
        bool anyAlive = false;

        // head + body all must be null OR dead
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && parts[i].gameObject.activeInHierarchy)
            {
                anyAlive = true;
                break;
            }
        }

        if (!anyAlive)
        {
            Debug.Log("[Boss] ALL PARTS DEAD → DESTROY BOSS ROOT");

            Destroy(gameObject);
        }
    }

    public void NotifyPartDeath(Transform part)
    {
        if (parts.Contains(part))
        {
            parts.Remove(part);
        }

        EnemyScript enemy = part.GetComponent<EnemyScript>();
        if (enemy != null)
        {
            partEnemies.Remove(enemy);
        }

        Debug.Log("[Boss] Part removed: " + part.name);
    }
}