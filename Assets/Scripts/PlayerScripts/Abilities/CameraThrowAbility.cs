using System.Collections;
using UnityEngine;
using TMPro;

public class CameraThrowAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private Transform player;
    [SerializeField] private Transform aimAnchor;
    [SerializeField] private ThirdPersonCamera cam;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Timer")]
    [SerializeField] private float abilityDuration = 10f;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Throw")]
    [SerializeField] private float throwSpeed = 35f;
    [SerializeField] private float throwAcceleration = 90f;
    [SerializeField] private float throwDuration = 0.25f;

    [Header("Hit")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float knockback = 12f;
    [SerializeField] private float hitRadius = 5f;

    [Header("Camera FX")]
    [SerializeField] private float aimShake = 0.02f;
    [SerializeField] private float hitShake = 0.5f;
    [SerializeField] private float missShake = 0.25f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 8f;
    [SerializeField] private float lingerTime = 0.6f;

    private bool active;
    private bool aiming;
    private bool thrown;
    private bool hitEnemy;

    private float timer;
    private Vector3 lastForward;

    private void Start()
    {
        if (cam == null)
        {
            cam = Camera.main.GetComponent<ThirdPersonCamera>();
        }

        if (combat == null)
        {
            combat = GetComponent<PlayerCombat>();
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryStart();
        }

        if (!active)
        {
            return;
        }

        HandleAim();

        if (Input.GetKeyDown(KeyCode.E) && aiming && !thrown)
        {
            StartCoroutine(Throw());
        }

        UpdateTimer();
    }

    private void TryStart()
    {
        if (active)
        {
            return;
        }

        if (combat == null || combat.currentAbilityCharge < 100f)
        {
            return;
        }

        combat.currentAbilityCharge = 0f;

        StartCoroutine(StartAbility());
    }

    private IEnumerator StartAbility()
    {
        active = true;
        aiming = true;
        thrown = false;
        hitEnemy = false;

        timer = abilityDuration;

        AbilityStateManager.Instance.isCameraThrowActive = true;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        yield return MoveToAnchor();
    }

    private IEnumerator MoveToAnchor()
    {
        float t = 0f;

        Vector3 start = cam.transform.position;
        Vector3 target = aimAnchor.position;

        while (t < 1f)
        {
            t += Time.deltaTime * 8f;

            cam.transform.position = Vector3.Lerp(start, target, t);

            ApplyShake(aimShake);

            yield return null;
        }
    }

    private void HandleAim()
    {
        if (!aiming || thrown)
        {
            return;
        }

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        cam.transform.RotateAround(player.position, Vector3.up, mx * 2f);
        cam.transform.RotateAround(player.position, cam.transform.right, -my * 2f);

        ApplyShake(aimShake);

        lastForward = cam.transform.forward;
    }

    private IEnumerator Throw()
    {
        thrown = true;
        aiming = false;
        hitEnemy = false;

        float t = 0f;
        float speed = 0f;

        while (t < throwDuration)
        {
            t += Time.deltaTime;

            speed += throwAcceleration * Time.deltaTime;
            speed = Mathf.Clamp(speed, 0f, throwSpeed);

            Vector3 move = lastForward * speed * Time.deltaTime;

            if (Physics.Raycast(cam.transform.position, lastForward, out RaycastHit hit, move.magnitude, hitMask, QueryTriggerInteraction.Ignore))
            {
                Debug.Log($"Camera HIT: {hit.collider.name} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)} | Point: {hit.point}");

                cam.transform.position = hit.point;
                break;
            }
            else
            {
                Debug.Log("Camera HIT: NOTHING");
            }

            cam.transform.position += move;

            ApplyShake(0.03f);

            yield return null;
        }

        if (!hitEnemy)
        {
            ApplyShake(missShake);
            yield return new WaitForSeconds(lingerTime);
        }
        else
        {
            ApplyShake(hitShake);
        }

        yield return ReturnCamera();
    }

    private IEnumerator ReturnCamera()
    {
        float t = 0f;

        Vector3 start = cam.transform.position;
        Vector3 target = player.position + Vector3.up * 1.6f - player.forward * 3.2f;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;

            cam.transform.position = Vector3.Lerp(start, target, t);

            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        aiming = false;
        active = false;
        thrown = false;

        AbilityStateManager.Instance.isCameraThrowActive = false;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    private void UpdateTimer()
    {
        if (!active)
        {
            return;
        }

        timer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = Mathf.Ceil(timer).ToString();
        }

        if (timer <= 0f)
        {
            StartCoroutine(ReturnCamera());
        }
    }

    private void ApplyShake(float strength)
    {
        if (cam == null)
        {
            return;
        }

        cam.AddShake(Random.insideUnitSphere * strength);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!thrown) return;

        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;

        EnemyScript enemy = other.GetComponentInParent<EnemyScript>();

        if (enemy == null)
            return;

        hitEnemy = true;

        enemy.TakeDamage(damage);

        Vector3 dir = (enemy.transform.position - cam.transform.position).normalized;
        enemy.ApplyKnockback(dir, knockback);

        ApplyShake(hitShake);
    }
}