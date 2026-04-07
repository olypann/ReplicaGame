using UnityEngine;

public class CustomCrosshair : MonoBehaviour
{
    public Transform player;
    public Transform cam;

    public float followSpeed = 10f;
    public float offsetFromPlayer = 0.8f; // how far in front of player

    private enum Mode
    {
        FollowPlayer,
        CameraFocus
    }

    private Mode currentMode = Mode.FollowPlayer;

    void Update()
    {
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
}