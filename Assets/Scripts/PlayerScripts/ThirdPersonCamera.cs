using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float distance = 3.5f;
    public float height = 1.5f;

    public float mouseSensitivity = 3f;
    public float smoothSpeed = 10f;

    public LayerMask groundLayer;

    [Header("Dodge Impulse")]
    [SerializeField] private float forwardImpulse = 0.35f;
    [SerializeField] private float sideImpulse = 0.25f;
    [SerializeField] private float backImpulse = 0.15f;

    [Header("Return Feel")]
    [SerializeField] private float impulseReturnSpeed = 10f;
    [SerializeField] private float overshootStrength = 0.15f;

    private Vector3 dodgeImpulse;
    private Vector3 overshootVelocity;

    private float yaw;
    private float pitch = 10f;

    private Vector3 lastPosition;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;

        float newPitch = pitch - mouseY;
        newPitch = Mathf.Clamp(newPitch, -80f, 60f);

        Quaternion testRotation = Quaternion.Euler(newPitch, yaw, 0);
        Vector3 testOffset = testRotation * new Vector3(0, height, -distance);
        Vector3 testPosition = target.position + testOffset;

        Vector3 direction = testPosition - target.position;
        float distanceToTarget = direction.magnitude;

        if (!Physics.Raycast(target.position, direction.normalized, distanceToTarget, groundLayer))
        {
            pitch = newPitch;
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 offset = rotation * new Vector3(0, height, -distance);

        Vector3 desiredPosition = target.position + offset + dodgeImpulse;

        // overshoot based on movement direction
        Vector3 frameVelocity = (transform.position - lastPosition) / Time.deltaTime;
        Vector3 overshoot = frameVelocity * overshootStrength;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition + overshoot,
            Time.deltaTime * smoothSpeed
        );

        lastPosition = transform.position;

        dodgeImpulse = Vector3.Lerp(
            dodgeImpulse,
            Vector3.zero,
            Time.deltaTime * impulseReturnSpeed
        );

        transform.LookAt(target.position + Vector3.up * height);
    }

    public void AddDodgeImpulse(Vector3 dodgeDirection)
    {
        Vector3 dir = -dodgeDirection;
        dir.y = 0f;
        dir.Normalize();

        float strength = sideImpulse;

        // forward / back detection
        float forwardDot = Vector3.Dot(dodgeDirection, target.forward);

        if (forwardDot > 0.5f)
        {
            strength = forwardImpulse;
        }
        else if (forwardDot < -0.5f)
        {
            strength = backImpulse;
        }

        dodgeImpulse += dir * strength;
    }
}