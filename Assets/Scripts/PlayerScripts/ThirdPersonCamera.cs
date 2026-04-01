using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float distance = 3.5f;
    public float height = 1.5f;

    public float mouseSensitivity = 3f;
    public float smoothSpeed = 10f;

    public LayerMask groundLayer;

    private float yaw;
    private float pitch = 10f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;

        // try applying pitch first
        float newPitch = pitch - mouseY;
        newPitch = Mathf.Clamp(newPitch, -80f, 60f);

        // simulate camera position with this pitch
        Quaternion testRotation = Quaternion.Euler(newPitch, yaw, 0);
        Vector3 testOffset = testRotation * new Vector3(0, height, -distance);
        Vector3 testPosition = target.position + testOffset;

        // check if line of sight is blocked
        Vector3 direction = testPosition - target.position;
        float distanceToTarget = direction.magnitude;

        if (!Physics.Raycast(target.position, direction.normalized, distanceToTarget, groundLayer))
        {
            pitch = newPitch;
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 offset = rotation * new Vector3(0, height, -distance);
        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * smoothSpeed
        );

        transform.LookAt(target.position + Vector3.up * height);
    }
}