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



    [Header("possession timer")]
    [SerializeField] private float possessionDuration = 15f;

    private float possessionTimer;
    private float cooldown;



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

        // sync global ability state so other systems know player control has shifted
        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isAbilityActive = state;
        }
        

        if (state)
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

        // rotate into movement direction so possessed enemy feels responsive
        if (move.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(move);
        }

        // character controller fallback so it still works if controller missing
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

        // quick forward burst attack
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


        // small aoe hit after lunge connects
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

            // timer runs out, force exit possession
            if (possessionTimer <= 0f)
            {
                SetPossessed(false, playerCamera);
                yield break;
            }

            yield return null;
        }
    }



    public bool IsValidTarget()
    {
        return this != null && gameObject != null;
    }
}