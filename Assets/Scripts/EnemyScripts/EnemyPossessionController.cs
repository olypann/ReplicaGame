using System.Collections;
using UnityEngine;
using TMPro;

public class EnemyPossessionController : MonoBehaviour
{
    private bool isPossessed;

    private Transform playerCamera;
    private CharacterController controller;

    [Header("movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("attack")]
    [SerializeField] private float lungeForce = 10f;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private float attackDamage = 20f;

    private float cooldown;

    [Header("ability timer")]
    [SerializeField] private float possessionDuration = 15f;

    private float possessionTimer;
    [Header("ui")]
    [SerializeField] private TextMeshProUGUI possessionTimerText;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void SetPossessed(bool state, Transform cam)
    {
        isPossessed = state;
        playerCamera = cam;

        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isAbilityActive = state;
        }

        if (state == true)
        {
            possessionTimer = possessionDuration;
            if (possessionTimerText != null)
            {
                possessionTimerText.gameObject.SetActive(true);
            }
            StartCoroutine(PossessionCountdown());

            
        }
        else
        {
            if (possessionTimerText != null)
            {
                possessionTimerText.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!isPossessed)
        {
            return;
        }

        HandleMovement();
        HandleAttack();
    }

    private void HandleMovement()
    {
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        Vector3 forward = playerCamera.forward;
        Vector3 right = playerCamera.right;

        forward.y = 0f;
        right.y = 0f;

        Vector3 move = forward * input.y + right * input.x;

        if (move.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(move);
        }

        if (controller != null)
        {
            controller.Move(move.normalized * moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.position += move.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleAttack()
    {
        cooldown -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            if (cooldown > 0f)
            {
                return;
            }

            cooldown = attackCooldown;

            StartCoroutine(DoLunge());
        }
    }

    private IEnumerator DoLunge()
    {
        float time = 0.2f;
        float t = 0f;

        Vector3 dir = transform.forward;

        while (t < time)
        {
            t += Time.deltaTime;

            if (controller != null)
            {
                controller.Move(dir * lungeForce * Time.deltaTime);
            }
            else
            {
                transform.position += dir * lungeForce * Time.deltaTime;
            }

            yield return null;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, 2f);

        foreach (Collider hit in hits)
        {
            EnemyScript enemy = hit.GetComponent<EnemyScript>();

            if (enemy != null && enemy.gameObject != gameObject)
            {
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    public bool IsPossessed()
    {
        return isPossessed;
    }

    private IEnumerator PossessionCountdown()
    {
        while (isPossessed)
        {
            possessionTimer -= Time.deltaTime;

            if (possessionTimerText != null)
            {
                possessionTimerText.text = Mathf.Ceil(possessionTimer).ToString();
            }

            if (possessionTimer <= 0f)
            {
                SetPossessed(false, playerCamera);
                yield break;
            }

            yield return null;
        }
    }
}