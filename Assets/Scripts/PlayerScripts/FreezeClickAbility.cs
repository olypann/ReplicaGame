using System.Collections;
using UnityEngine;

public class FreezeClickAbility : MonoBehaviour
{
    [Header("timing")]
    [SerializeField] private float duration = 6f;
    private float timer;

    [Header("combat")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float pushForce = 8f;

    [Header("ui")]
    [SerializeField] private GameObject freezeUI;

    private bool active;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (!active)
            {
                StartCoroutine(RunAbility());
            }
        }

        if (!active)
        {
            return;
        }

        HandleClick();
    }

    private IEnumerator RunAbility()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        active = true;
        timer = duration;

        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isFreezeAbilityActive = true;
        }

        if (freezeUI != null)
        {
            freezeUI.SetActive(true);
        }

        Camera.main?.GetComponent<ThirdPersonCamera>()?.AddScreenShake(0.6f, 8f, duration);

        while (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }

        EndAbility();
    }

    private void EndAbility()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Camera.main?.GetComponent<ThirdPersonCamera>()?.ResetShake();

        

        active = false;

        if (AbilityStateManager.Instance != null)
        {
            AbilityStateManager.Instance.isFreezeAbilityActive = false;
        }

        if (freezeUI != null)
        {
            freezeUI.SetActive(false);
        }
    }

    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            EnemyScript enemy = hit.collider.GetComponentInParent<EnemyScript>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                Vector3 dir = (enemy.transform.position - Camera.main.transform.position);
                dir.y = 0f;

                enemy.ApplyKnockback(dir, pushForce);
            }
        }
    }

    private IEnumerator PushCC(CharacterController cc, Vector3 dir)
    {
        float t = 0f;

        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            cc.Move(dir * pushForce * Time.unscaledDeltaTime);
            yield return null;
        }
    }
}