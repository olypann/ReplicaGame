using System.Collections;
using UnityEngine;

public class TimeFreezeAbility : MonoBehaviour
{
    [Header("freeze settings")]
    [SerializeField] private float duration = 4f;
    [SerializeField] private float freezeTimeScale = 0.05f;
    [SerializeField] private float pushForce = 8f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject freezeUI;

    [Header("fx")]
    [SerializeField] private GameObject clickHitVFX;

    private bool isActive;
    private float timer;

    private PlayerCombat combat;

    private void Start()
    {
        combat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (AbilityStateManager.Instance != null &&
                AbilityStateManager.Instance.IsAnyAbilityActive())
            {
                return;
            }

            if (!isActive)
            {
                if (combat == null)
                {
                    return;
                }

                if (!combat.HasFullAbilityCharge())
                {
                    return;
                }

                combat.ConsumeAbilityCharge();
                StartCoroutine(RunFreeze());
            }
        }

        if (!isActive)
        {
            return;
        }

        HandleClick();
    }

    private IEnumerator RunFreeze()
    {
        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isFreezeAbilityActive = true;
        }

        isActive = true;
        AbilityStateManager.Instance.isAbilityActive = true;
        AbilityStateManager.Instance.isFreezeAbilityActive = true;
        timer = duration;

        if (freezeUI != null)
        {
            freezeUI.SetActive(true);
        }

        Time.timeScale = freezeTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Camera.main?.GetComponent<ThirdPersonCamera>()?.SetFrozen(true);

        while (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EndFreeze();
    }

    private void EndFreeze()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (freezeUI != null)
        {
            freezeUI.SetActive(false);
        }

        Camera.main?.GetComponent<ThirdPersonCamera>()?.SetFrozen(false);

        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isFreezeAbilityActive = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isActive = false;

        AbilityStateManager.Instance.isFreezeAbilityActive = false;
        AbilityStateManager.Instance.isAbilityActive = false;
    }

    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, enemyLayer))
        {
            EnemyScript enemy = hit.collider.GetComponentInParent<EnemyScript>();

            if (enemy != null)
            {
                Vector3 dir = (enemy.transform.position - Camera.main.transform.position).normalized;

                enemy.TakeDamage(10f);

                if (clickHitVFX != null)
                {
                    Instantiate(
                        clickHitVFX,
                        hit.point,
                        Quaternion.LookRotation(hit.normal)
                    );
                }

                SoundManager.Instance?.PlayFreezeClickHit();

                Rigidbody rb = enemy.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.AddForce(dir * pushForce, ForceMode.Impulse);
                }
                else
                {
                    enemy.transform.position += dir * pushForce * 0.1f;
                }
            }
        }
    }
}