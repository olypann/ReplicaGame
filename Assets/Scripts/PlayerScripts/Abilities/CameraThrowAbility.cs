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
    [SerializeField] private TextMeshProUGUI instructionText;

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

    [Header("Camera Anchors")]
    [SerializeField] private Transform defaultCameraAnchor;

    [Header("Aiming Feel")]
    [SerializeField] private float aimRotateSpeed = 2f;
    [SerializeField] private float swayAmount = 0.05f;
    [SerializeField] private float swaySpeed = 4f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string removeWeaponAnim = "WeaponRemove";

    private Vector3 returnTargetPosition;

    private bool active;
    private bool aiming;
    private bool thrown;
    private bool hitEnemy;
    private bool returning;

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

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        if (instructionText != null)
        {
            instructionText.text = "";
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

        if (returning)
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
        if (AbilityStateManager.Instance != null &&
        AbilityStateManager.Instance.IsAnyAbilityActive())
        {
            return;
        }

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

        AbilityStateManager.Instance.isAbilityActive = true;
        AbilityStateManager.Instance.isCameraThrowActive = true;

        aiming = true;
        thrown = false;
        hitEnemy = false;
        returning = false;

        timer = abilityDuration;

        AbilityStateManager.Instance.isCameraThrowActive = true;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        if (instructionText != null)
        {
            instructionText.text = "Aim With Mouse and Press 'E' to Launch";
            instructionText.gameObject.SetActive(true);
        }

        if (animator != null)
        {
            animator.Play(removeWeaponAnim);
        }
        //SoundManager.Instance?.PlayCameraGrab();

        returnTargetPosition = defaultCameraAnchor.position;

        yield return MoveToAnchor();

        cam.AddShake(Random.insideUnitSphere * 0.15f);
    }

    private IEnumerator MoveToAnchor()
    {
        Vector3 velocity = Vector3.zero;

        SoundManager.Instance?.PlayCameraGrab();

        while (Vector3.Distance(cam.transform.position, aimAnchor.position) > 0.01f)
        {
            cam.transform.position = Vector3.SmoothDamp(
                cam.transform.position,
                aimAnchor.position,
                ref velocity,
                0.08f // lower = snappier, higher = smoother
            );

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

        cam.transform.RotateAround(player.position, Vector3.up, mx * aimRotateSpeed);
        cam.transform.RotateAround(player.position, cam.transform.right, -my * aimRotateSpeed);

        Vector3 sway =
            cam.transform.right * Mathf.Sin(Time.time * swaySpeed) * swayAmount +
            cam.transform.up * Mathf.Cos(Time.time * swaySpeed * 0.8f) * (swayAmount * 0.5f);

        cam.transform.position += sway;

        lastForward = cam.transform.forward;
    }

    private IEnumerator Throw()
    {
        
        SoundManager.Instance?.PlayCameraThrow();
        thrown = true;
        aiming = false;
        hitEnemy = false;

        bool hitSomething = false;

        float t = 0f;
        float speed = 0f;

        while (t < throwDuration || !hitSomething)
        {
            t += Time.deltaTime;

            speed += throwAcceleration * Time.deltaTime;
            speed = Mathf.Clamp(speed, 0f, throwSpeed);

            Vector3 move = lastForward * speed * Time.deltaTime;

            if (Physics.Raycast(cam.transform.position, lastForward, out RaycastHit hit, move.magnitude, hitMask, QueryTriggerInteraction.Ignore))
            {
                cam.transform.position = hit.point;
                hitSomething = true;

                SoundManager.Instance?.PlayCameraHit();

                DamageArea(hit.point);

                cam.AddShake(Random.insideUnitSphere * hitShake);

                break;
            }

            cam.transform.position += move;

            yield return null;
        }

        if (!hitSomething)
        {
            cam.AddThrowCameraImpulse(-cam.transform.forward * missShake);
            yield return new WaitForSeconds(lingerTime);
        }

        yield return ReturnCamera();
    }

    private void DamageArea(Vector3 point)
    {
        cam.PlayCameraHitVFX();
        Collider[] hits = Physics.OverlapSphere(point, hitRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyScript enemy = hit.GetComponentInParent<EnemyScript>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                Vector3 dir = (enemy.transform.position - point).normalized;

                enemy.ApplyKnockback(dir, knockback);

                hitEnemy = true;
            }
        }
    }

    private IEnumerator ReturnCamera()
    {
        if (returning)
        {
            yield break;
        }

        returning = true;

        AbilityStateManager.Instance.isCameraReturning = true;

        float t = 0f;

        Vector3 start = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;

            cam.transform.position = Vector3.Lerp(start, defaultCameraAnchor.position, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, defaultCameraAnchor.rotation, t);

            yield return null;
        }

        if (cam != null)
        {
            cam.ClearThrowCameraImpulse();
            cam.ResetAfterCameraThrow();
        }

        active = false;

        AbilityStateManager.Instance.isCameraThrowActive = false;
        AbilityStateManager.Instance.isAbilityActive = false;

        aiming = false;
        thrown = false;
        returning = false;

        AbilityStateManager.Instance.isCameraThrowActive = false;
        AbilityStateManager.Instance.isCameraReturning = false;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        if (instructionText != null)
        {
            instructionText.text = "";
        }
    }

    private void UpdateTimer()
    {
        timer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = Mathf.Ceil(timer).ToString();
        }

        if (timer <= 0f && !returning)
        {
            StartCoroutine(ReturnCamera());
        }
    }
}