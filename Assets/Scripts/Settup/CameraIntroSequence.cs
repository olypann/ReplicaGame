using System.Collections;
using UnityEngine;

public class CameraIntroSequence : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private EnemyWaveManager waveManager;

    [Header("camera points")]
    [SerializeField] private Transform introPoint;
    [SerializeField] private Transform gameplayPoint;

    [Header("ui canvases")]
    [SerializeField] private GameObject introCanvas;
    [SerializeField] private GameObject gameCanvas;
    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;

    [Header("movement")]
    [SerializeField] private float moveTime = 3f;

    private bool started;
    private bool returningToIntro;

    private void Start()
    {
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = false;
        }

        if (thirdPersonCamera != null && thirdPersonCamera.target != null)
        {
            playerStartPosition = thirdPersonCamera.target.position;
            playerStartRotation = thirdPersonCamera.target.rotation;
        }

        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }

        if (introPoint != null)
        {
            Camera.main.transform.position = introPoint.position;
            Camera.main.transform.rotation = introPoint.rotation;
        }
    }

    private void Update()
    {
        if (!started)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void StartGame()
    {
        
        if (started)
        {
            return;
        }

        started = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(MoveCameraToGameplay());
    }

    private IEnumerator MoveCameraToGameplay()
    {
        Transform cam = Camera.main.transform;

        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        Vector3 endPos = gameplayPoint.position;
        Quaternion endRot = gameplayPoint.rotation;

        float t = 0f;

        while (t < moveTime)
        {
            t += Time.deltaTime;

            float p = Mathf.SmoothStep(0f, 1f, t / moveTime);

            cam.position = Vector3.Lerp(startPos, endPos, p);
            cam.rotation = Quaternion.Slerp(startRot, endRot, p);

            yield return null;
        }

        cam.position = endPos;
        cam.rotation = endRot;

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = true;
        }

        if (gameCanvas != null)
        {
            gameCanvas.SetActive(true);
        }

        if (waveManager != null)
        {
            waveManager.BeginWaves();
        }
    }

    public void ReturnToIntro()
    {
        if (returningToIntro)
        {
            return;
        }

        returningToIntro = true;

        StartCoroutine(MoveCameraBackToIntro());
    }

    private IEnumerator MoveCameraBackToIntro()
    {
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = false;
        }

        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }

        Transform cam = Camera.main.transform;

        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        Vector3 endPos = introPoint.position;
        Quaternion endRot = introPoint.rotation;

        float t = 0f;

        while (t < moveTime)
        {
            
            t += Time.deltaTime;

            float p = Mathf.SmoothStep(0f, 1f, t / moveTime);

            cam.position = Vector3.Lerp(startPos, endPos, p);
            cam.rotation = Quaternion.Slerp(startRot, endRot, p);

            // if (t >= moveTime * 0.4f)
            // {
            //     if (thirdPersonCamera != null && thirdPersonCamera.target != null)
            //     {
            //         thirdPersonCamera.target.position = playerStartPosition;
            //         thirdPersonCamera.target.rotation = playerStartRotation;
            //     }
            // }

            yield return null;
        }

        cam.position = endPos;
        cam.rotation = endRot;

        // small hidden reset window so player teleport is not visible
        yield return new WaitForSeconds(0.3f);

        if (FindFirstObjectByType<PlayerStats>() != null)
        {
            PlayerStats player = FindFirstObjectByType<PlayerStats>();

            player.ResetPosition();
            player.ResetPlayer();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        returningToIntro = false;
        started = false;
    }
}