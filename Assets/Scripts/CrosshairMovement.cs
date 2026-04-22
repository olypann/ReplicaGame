using UnityEngine;
using System.Collections;


public class CustomCrosshair : MonoBehaviour
{
    public Transform player;
    public Transform cam;

    public float followSpeed = 10f;
    public float offsetFromPlayer = 0.8f; 

    [Header("timers")]
    [SerializeField] private float playerTargetTimeout = 1.5f;
    [SerializeField] private float cameraTargetHoldTime = 2.5f;

    private float playerTimer;
    private float cameraTimer;

    private bool isVisible;
    private CrosshairUIAnimator animator;

    [SerializeField] private float resetToPlayerDelay = 2f;
    [SerializeField] private float hideDelay = 4f;

    private float resetTimer;
    private float hideTimer;

    [SerializeField] private float abilityHideFadeTime = 0.2f;

    private enum Mode
    {
        FollowPlayer,
        CameraFocus
    }

    private Mode currentMode = Mode.FollowPlayer;

    void Start()
    {
        animator = GetComponent<CrosshairUIAnimator>();

        if (CrosshairEvents.Instance != null)
        {
            CrosshairEvents.Instance.OnPlayerTargeted += OnPlayerTargeted;
            CrosshairEvents.Instance.OnCameraTargeted += OnCameraTargeted;
        }
    }

    void Update()
    {
        if (AbilityStateManager.Instance != null)
        {
            if (AbilityStateManager.Instance.isAbilityActive)
            {
                Hide();
                return;
            }
        }

        playerTimer -= Time.deltaTime;
        cameraTimer -= Time.deltaTime;

        resetTimer -= Time.deltaTime;
        hideTimer -= Time.deltaTime;

        if (cameraTimer > 0f)
        {
            currentMode = Mode.CameraFocus;
            Show();
        }
        else if (playerTimer > 0f)
        {
            currentMode = Mode.FollowPlayer;
            Show();
        }
        else if (resetTimer <= 0f)
        {
            currentMode = Mode.FollowPlayer;
        }

        if (hideTimer <= 0f && cameraTimer <= 0f && playerTimer <= 0f)
        {
            Hide();
        }


        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentMode = Mode.FollowPlayer;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentMode = Mode.CameraFocus;
        }

        if (currentMode == Mode.FollowPlayer)
        {
            FollowPlayerMode();
        }
        else if (currentMode == Mode.CameraFocus)
        {
            CameraFocusMode();
        }
    }

    void FollowPlayerMode()
    {
        Vector3 directionFromCameraToPlayer = (player.position - cam.position).normalized;

        Vector3 targetPosition = player.position - directionFromCameraToPlayer * offsetFromPlayer;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * followSpeed
        );

        transform.LookAt(cam);
    }

    void CameraFocusMode()
    {
        Ray ray = new Ray(cam.position, cam.forward);

        Vector3 targetPosition = ray.GetPoint(2f);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * followSpeed
        );

        transform.LookAt(cam);
    }


    void OnPlayerTargeted()
    {
        playerTimer = playerTargetTimeout;

        resetTimer = resetToPlayerDelay;
        hideTimer = hideDelay;

        currentMode = Mode.FollowPlayer;

        TriggerSpin();
        Show();
    }

    void OnCameraTargeted()
    {
        cameraTimer = cameraTargetHoldTime;

        hideTimer = hideDelay;

        currentMode = Mode.CameraFocus;

        TriggerSpin();
        Show();
    }


    void TriggerSpin()
    {
        if (animator != null)
        {
            animator.SendMessage("StartAnimation");
        }
    }

    void Show()
    {
        if (isVisible)
        {
            return;
        }

        gameObject.SetActive(true);
        isVisible = true;

        isVisible = true;

        TriggerSpin();

        //gameObject.SetActive(true);
    }

    void Hide()
    {
        if (!isVisible)
        {
            return;
        }

        isVisible = false;

        // spin out instead of instant hide
        TriggerSpin();

        StartCoroutine(HideAfterAnim());
    }

    IEnumerator HideAfterAnim()
    {
        yield return new WaitForSeconds(0.5f);

        gameObject.SetActive(false);
    }
}