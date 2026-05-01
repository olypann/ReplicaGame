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


    [Header("ui")]
    [SerializeField] private GameObject introCanvas;
    [SerializeField] private GameObject gameCanvas;


    [Header("movement")]
    [SerializeField] private float moveTime = 3f;



    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;

    private bool started;
    private bool returningToIntro;



    private void Start()
    {
        // disable gameplay camera at boot
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = false;
        }

        // store player starting transform (for reset later)
        if (thirdPersonCamera != null && thirdPersonCamera.target != null)
        {
            playerStartPosition = thirdPersonCamera.target.position;
            playerStartRotation = thirdPersonCamera.target.rotation;
        }

        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }

        // snap camera to intro position at start
        if (introPoint != null)
        {
            Camera.main.transform.position = introPoint.position;
            Camera.main.transform.rotation = introPoint.rotation;
        }
    }



    private void Update()
    {

        // keep cursor free while still in menu state
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

        // switch into gameplay mode
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

            yield return null;
        }

        cam.position = endPos;
        cam.rotation = endRot;


        // small buffer so player reset isnt visually noticeable
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