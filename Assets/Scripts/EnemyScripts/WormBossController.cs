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


    // target + main body stuff

    [Header("Target")]
    [SerializeField] private Transform player;

    // index 0 is always the head
    [Header("Body Parts (0 = head)")]
    [SerializeField] private List<Transform> parts = new List<Transform>();

    // enemy scripts attached to each segment
    [Header("Enemy Scripts on Parts")]
    [SerializeField] private List<EnemyScript> partEnemies = new List<EnemyScript>();


    // orbit behaviour settings

    [Header("Orbit Settings")]
    [SerializeField] private float fastRadius = 6f;
    [SerializeField] private float calmRadius = 4f;

    [SerializeField] private float fastHeight = 3f;
    [SerializeField] private float calmHeight = 1.5f;

    [SerializeField] private float fastSpeed = 2f;
    [SerializeField] private float calmSpeed = 0.7f;


    // general movement + chain follow

    [Header("Movement")]
    [SerializeField] private float headLerpSpeed = 6f;
    [SerializeField] private float chainSpeed = 10f;
    [SerializeField] private float segmentDistance = 1.2f;


    // dive attack

    [Header("Dive Attack")]
    [SerializeField] private float diveSpeed = 18f;
    [SerializeField] private float diveDuration = 1.2f;
    [SerializeField] private float diveDamageRange = 2.5f;
    [SerializeField] private float diveDamage = 20f;


    // split attack

    [Header("Split Attack")]
    [SerializeField] private float splitDuration = 4f;
    [SerializeField] private float splitForce = 6f;


    // misc helpers

    [Header("Ground Offset")]
    [SerializeField] private float groundOffset = 1.5f;

    [SerializeField] private float maxHeightAboveGround = 3f;


    // timing for switching states

    [Header("State Timing")]
    [SerializeField] private float minStateTime = 2f;
    [SerializeField] private float maxStateTime = 4f;

    [Range(0f, 1f)]
    [SerializeField] private float calmChance = 0.4f;


    // reset if player ignores boss too long

    [Header("Reset")]
    [SerializeField] private float noHitResetTime = 15f;
    [SerializeField] private Vector3 resetPosition = new Vector3(-0.18f, 2f, 0.02f);



    // runtime stuff

    private BossState state;
    private float stateTimer;
    private float orbitT;

    private bool isSplitting;
    private bool diveHit;

    private Vector3 diveTarget;
    private Vector3[] splitVelocities;

    private float lastHitTime;
    private int startingPartCount;



    void Start()
    {
        // try grab player automatically if not set
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        lastHitTime = Time.time;

        state = BossState.Orbit_Fast;
        stateTimer = Random.Range(minStateTime, maxStateTime);

        splitVelocities = new Vector3[Mathf.Max(1, parts.Count)];
        startingPartCount = parts.Count;

        // boss controls all segments by default
        SetBossControl(true);

        Debug.Log("start orbit fast");
    }

    void Update()
    {
        CleanupDestroyedParts();

        if (player == null || parts.Count == 0)
        {
            return;
        }

        stateTimer -= Time.deltaTime;

        // if player hasn't touched boss in a while, pull it back
        if (Time.time - lastHitTime >= noHitResetTime)
        {
            ForceReturnToCenter();
        }

        // basic state machine
        if (state == BossState.Orbit_Fast || state == BossState.Orbit_Calm)
        {
            Orbit();
            HandleTransitions();
        }
        else if (state == BossState.Dive)
        {
            Dive();
        }
        else if (state == BossState.Split)
        {
            Split();
        }

        // don't chain while split since parts are doing their own thing
        if (state != BossState.Split)
        {
            UpdateChain();
        }

        CheckBossDeath();
    }



    // orbiting around the player with some height variation
    private void Orbit()
    {
        bool calm = state == BossState.Orbit_Calm;

        float speed = calm ? calmSpeed : fastSpeed;
        float radius = calm ? calmRadius : fastRadius;

        orbitT += Time.deltaTime * speed;

        Vector3 center = player.position;
        center.y = GetGroundY(player.position);

        Vector3 offset = new Vector3(
            Mathf.Cos(orbitT),
            0f,
            Mathf.Sin(orbitT)
        ) * radius;

        Vector3 target = center + offset;

        float groundY = GetGroundY(target);

        int aliveBody = Mathf.Max(0, parts.Count - 1);
        bool headOnly = aliveBody <= 0;

        float height;
        float bob;

        // tweak movement depending on how many parts are left
        if (headOnly)
        {
            height = 0.08f;
            bob = Mathf.Sin(orbitT * 4f) * 0.05f;
        }
        else if (calm)
        {
            height = 0.45f;
            bob = Mathf.Sin(orbitT * 3f) * 0.28f;
        }
        else
        {
            height = 0.9f;
            bob = Mathf.Sin(orbitT * 2.5f) * 0.45f;
        }

        target.y = groundY + height + bob;

        if (headOnly)
        {
            target.y = Mathf.Clamp(target.y, groundY + 0.02f, groundY + 0.22f);
        }
        else
        {
            target.y = Mathf.Clamp(target.y, groundY + 0.03f, groundY + 1.2f);
        }

        MoveHead(target);
    }


    // smooth head movement + keeping it from flying too high
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



    private void StartDive()
    {
        state = BossState.Dive;
        stateTimer = diveDuration;

        diveTarget = player.position;
        diveHit = false;

        Debug.Log("[Boss] dive");
    }

    // straight line dive towards saved position
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

        // only damage once per dive
        if (!diveHit && Vector3.Distance(head.position, player.position) < diveDamageRange)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();

            if (stats != null)
            {
                stats.TakeDamage(diveDamage);
            }

            diveHit = true;
        }

        if (stateTimer <= 0f)
        {
            SetOrbitFast();
        }
    }



    private void StartSplit()
    {
        state = BossState.Split;
        stateTimer = splitDuration;
        isSplitting = true;

        // let each segment act on its own
        SetBossControl(false);

        Debug.Log("[Boss] split");

        for (int i = 1; i < parts.Count; i++)
        {
            splitVelocities[i] = Random.insideUnitSphere * splitForce;
        }
    }

    // segments scatter but still kinda drift toward player
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

            SetBossControl(true);
            SetOrbitFast();
        }
    }



    // makes segments follow each other like a chain
    private void UpdateChain()
    {
        if (parts == null || parts.Count == 0)
        {
            return;
        }

        Transform prev = parts[0];

        for (int i = 1; i < parts.Count; i++)
        {
            Transform part = parts[i];

            if (part == null || prev == null)
            {
                continue;
            }

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



    private void HandleTransitions()
    {
        if (stateTimer > 0f)
        {
            return;
        }

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
    }

    private void SetOrbitCalm()
    {
        state = BossState.Orbit_Calm;
    }



    // switch whether segments listen to boss or their own scripts
    private void SetBossControl(bool value)
    {
        foreach (EnemyScript e in partEnemies)
        {
            if (e != null)
            {
                e.controlledByBoss = value;
            }
        }
    }



    // raycast down to find ground height
    private float GetGroundY(Vector3 pos)
    {
        RaycastHit hit;
        Vector3 origin = pos + Vector3.up * 50f;

        if (Physics.Raycast(origin, Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }

        return pos.y;
    }



    private void CleanupDestroyedParts()
    {
        // go backwards so removing doesn't mess up indices
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            if (parts[i] == null)
            {
                parts.RemoveAt(i);

                if (i < partEnemies.Count)
                {
                    partEnemies.RemoveAt(i);
                }
            }
        }
    }

    private void CheckBossDeath()
    {
        bool alive = false;

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && parts[i].gameObject.activeInHierarchy)
            {
                alive = true;
                break;
            }
        }

        if (!alive)
        {
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
    }



    // slowly pulls boss back if player stops interacting
    private void ForceReturnToCenter()
    {
        Transform head = parts[0];

        if (head == null)
        {
            return;
        }

        head.position = Vector3.Lerp(
            head.position,
            resetPosition,
            Time.deltaTime * headLerpSpeed
        );

        UpdateChain();
    }

    public void NotifyBossHit()
    {
        lastHitTime = Time.time;
    }
}