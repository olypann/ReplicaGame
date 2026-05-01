using System.Collections;
using UnityEngine;
using TMPro;

public class TimeFreezeAbility : MonoBehaviour
{
    [Header("freeze settings")]
    [SerializeField] private float duration = 4f;
    [SerializeField] private float freezeTimeScale = 0.05f;
    [SerializeField] private float pushForce = 8f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject freezeUI;

    [SerializeField] private TMP_Text abilityText;
    [SerializeField] private TextMeshProUGUI timerText;


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
            // block if something else is running
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

                // need full charge
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

        if (timerText != null)
        {
            timerText.text = Mathf.Ceil(timer).ToString();
        }

        isActive = true;

        AbilityStateManager.Instance.isAbilityActive = true;
        AbilityStateManager.Instance.isFreezeAbilityActive = true;

        timer = duration;

        if (freezeUI != null)
        {
            freezeUI.SetActive(true);
        }

        if (abilityText != null)
        {
            abilityText.text = "Click Frozen Enemies";
        }

        //slow everything down
        Time.timeScale = freezeTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Camera.main?.GetComponent<ThirdPersonCamera>()?.SetFrozen(true);

        while (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;

            if (timerText != null)
            {
                timerText.text = Mathf.Ceil(timer).ToString();
            }

            yield return null;
        }

        // allow cursor again before exiting
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EndFreeze();
    }



    private void EndFreeze()
    {
        // restore time back to normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (freezeUI != null)
        {
            freezeUI.SetActive(false);
        }

        if (abilityText != null)
        {
            abilityText.text = "";
        }

        if (timerText != null)
        {
            timerText.text = "";
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



    // clicking enemies while time is slowed
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


                // try physics push first, fallback if no rigidbody
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