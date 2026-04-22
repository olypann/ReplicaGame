using System.Collections;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float distance = 3.5f;
    public float height = 1.5f;

    public float mouseSensitivity = 3f;
    public float smoothSpeed = 10f;

    public LayerMask groundLayer;
    [Header("Camera Obstacle")]
    [SerializeField] private float obstacleRadius = 0.25f;
    [SerializeField] private float obstacleBuffer = 0.1f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Dodge Impulse")]
    [SerializeField] private float forwardImpulse = 0.35f;
    [SerializeField] private float sideImpulse = 0.25f;
    [SerializeField] private float backImpulse = 0.15f;

    [Header("Return Feel")]
    [SerializeField] private float impulseReturnSpeed = 10f;
    [SerializeField] private float overshootStrength = 0.15f;

    [Header("Charge Zoom")]
    [SerializeField] private float chargeZoomFOV = 55f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomSmoothSpeed = 6f;

    [SerializeField] private float chargedAttackKickFOV = 50f;
    [SerializeField] private float kickReturnSpeed = 10f;

    [Header("Dynamic Camera - Distance")]
    [SerializeField] private float normalDistance = 3.5f;
    [SerializeField] private float sprintDistance = 4.2f;
    [SerializeField] private float combatDistance = 3.0f;

    [SerializeField] private float distanceSmooth = 8f;

    [Header("Dynamic Camera - Height")]
    [SerializeField] private float normalHeight = 1.5f;
    [SerializeField] private float combatHeight = 1.3f;

    [SerializeField] private float heightSmooth = 8f;

    private bool isCharging;
    private bool doChargeKick;
    private float currentFOVVelocity;

    [Header("Subtle Motion")]
    [SerializeField] private float swayAmount = 0.05f;
    [SerializeField] private float swaySpeed = 1.5f;

    [SerializeField] private float breatheAmount = 0.03f;
    [SerializeField] private float breatheSpeed = 1.2f;
    private float motionTime;

    private Vector3 dodgeImpulse;
    private Vector3 overshootVelocity;

    private float targetDistance;
    private float targetHeight;

    private PlayerCombat combat;
    private PlayerMovement movement;

    private float yaw;
    private float pitch = 10f;

    private Vector3 lastPosition;

    // other effects
    private Vector3 externalImpulse;
    private float externalImpulseReturnSpeed = 6f;

    private float externalRoll;
    private float externalYaw;
    private float externalPitch;

    private float rollReturnSpeed = 3f;

    private float screenShakeTime;
    private float screenShakeStrength;
    private float screenShakeReturnSpeed;
    private Vector2 screenShakeOffset;

    [Header("target blending")]
    [SerializeField] private float targetBlendSpeed = 6f;

    private Transform currentTarget;
    private Transform targetGoal;
    private float targetBlendT;

    private bool frozen;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        lastPosition = transform.position;

        combat = target.GetComponent<PlayerCombat>();
        movement = target.GetComponent<PlayerMovement>();

        currentTarget = target;
        targetGoal = target;
    }

    private void LateUpdate()
    {
        if (frozen)
        {
            transform.position = frozenPosition;
            transform.rotation = frozenRotation;
            return;
        }

        if (AbilityStateManager.Instance != null && AbilityStateManager.Instance.isFreezeAbilityActive)
        {
            transform.LookAt(target.position + Vector3.up * height);
            return;
        }

        if (currentTarget == null)
        {
            return;
        }

        Vector3 blendedTargetPos = GetBlendedTargetPosition();

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX + externalYaw;

        float newPitch = pitch - mouseY + externalPitch;
        newPitch = Mathf.Clamp(newPitch, -80f, 60f);

        bool isAttacking = combat != null && combat.IsAttacking();
        bool isCharging = combat != null && combat.IsCharging();

        Quaternion testRotation = Quaternion.Euler(newPitch, yaw, 0);
        Vector3 testOffset = testRotation * new Vector3(0, height, -distance);
        Vector3 testPosition = blendedTargetPos + testOffset;

        Vector3 direction = testPosition - blendedTargetPos;
        float distanceToTarget = direction.magnitude;

        if (!Physics.Raycast(blendedTargetPos, direction.normalized, distanceToTarget, groundLayer))
        {
            pitch = newPitch;
        }

        targetDistance = normalDistance;

        if (isAttacking || isCharging)
        {
            targetDistance = combatDistance;
        }
        else if (movement != null)
        {
            float speed = movement.GetComponent<CharacterController>().velocity.magnitude;

            if (speed > 4f)
            {
                targetDistance = sprintDistance;
            }
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * distanceSmooth);

        height = Mathf.Lerp(
            height,
            combat != null && (isAttacking || isCharging) ? combatHeight : normalHeight,
            Time.deltaTime * heightSmooth
        );

        Vector3 offset = rotation * new Vector3(0, height, -distance);

        motionTime += Time.deltaTime;

        float swayX = Mathf.Sin(motionTime * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(motionTime * breatheSpeed) * breatheAmount;

        Vector3 subtleOffset = new Vector3(swayX, swayY, 0f);

        Vector3 desiredPosition = blendedTargetPos + offset + subtleOffset + dodgeImpulse + externalImpulse;

        Vector3 directionToCamera = offset.normalized;
        float cameraDistance = offset.magnitude;

        RaycastHit hit;

        if (Physics.SphereCast(
            blendedTargetPos,
            obstacleRadius,
            directionToCamera,
            out hit,
            cameraDistance,
            obstacleLayers))
        {
            desiredPosition = blendedTargetPos + (directionToCamera * (hit.distance - obstacleBuffer)) + dodgeImpulse + externalImpulse;
        }

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

        externalImpulse = Vector3.Lerp(
            externalImpulse,
            Vector3.zero,
            Time.deltaTime * externalImpulseReturnSpeed
        );

        externalRoll = Mathf.Lerp(
            externalRoll,
            0f,
            Time.deltaTime * rollReturnSpeed
        );

        externalYaw = Mathf.Lerp(
            externalYaw,
            0f,
            Time.deltaTime * rollReturnSpeed
        );

        externalPitch = Mathf.Lerp(
            externalPitch,
            0f,
            Time.deltaTime * rollReturnSpeed
        );

        float targetFOV = normalFOV;

        if (isCharging)
        {
            targetFOV = chargeZoomFOV;
        }

        if (doChargeKick)
        {
            targetFOV = chargedAttackKickFOV;
        }

        Camera cam = GetComponent<Camera>();

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                targetFOV,
                Time.deltaTime * zoomSmoothSpeed
            );
        }

        float shakeX = 0f;
        float shakeY = 0f;

        if (screenShakeTime > 0f)
        {
            screenShakeTime -= Time.deltaTime;

            screenShakeOffset = Random.insideUnitCircle * screenShakeStrength;

            shakeX = screenShakeOffset.x;
            shakeY = screenShakeOffset.y;
        }
        else
        {
            screenShakeOffset = Vector2.Lerp(
                screenShakeOffset,
                Vector2.zero,
                Time.deltaTime * screenShakeReturnSpeed
            );
        }

        transform.LookAt(blendedTargetPos + Vector3.up * height);

        transform.rotation = transform.rotation * Quaternion.Euler(shakeY, shakeX, externalRoll);
    }

    private Vector3 GetBlendedTargetPosition()
    {
        if (currentTarget == targetGoal)
        {
            return currentTarget.position;
        }

        targetBlendT += Time.deltaTime * targetBlendSpeed;

        Vector3 pos = Vector3.Lerp(
            currentTarget.position,
            targetGoal.position,
            targetBlendT
        );

        if (targetBlendT >= 1f)
        {
            currentTarget = targetGoal;
            targetBlendT = 0f;
        }

        return pos;
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            return;
        }

        currentTarget = target;
        targetGoal = newTarget;
        target = newTarget;

        targetBlendT = 0f;
    }

    public void AddDodgeImpulse(Vector3 dodgeDirection)
    {
        Vector3 dir = -dodgeDirection;
        dir.y = 0f;
        dir.Normalize();

        float strength = sideImpulse;

        // forward back 
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

    public void AddExternalImpulse(Vector3 dir, float strength, float returnSpeed, Vector3 axisMask)
    {
        Vector3 push = -dir.normalized;

        push = Vector3.Scale(push, axisMask);

        externalImpulse += push * strength;

        externalImpulseReturnSpeed = returnSpeed;
    }

    public void AddExternalRotation(Vector3 dir, float strength, float returnSpeed, CameraEntity.CameraRotationAxis axis)
    {
        float side = Vector3.Dot(dir, transform.right);

        if (axis == CameraEntity.CameraRotationAxis.Roll)
        {
            externalRoll += side * strength;
        }

        if (axis == CameraEntity.CameraRotationAxis.Yaw)
        {
            externalYaw += side * strength;
        }

        if (axis == CameraEntity.CameraRotationAxis.Pitch)
        {
            externalPitch += side * strength;
        }

        rollReturnSpeed = returnSpeed;
    }

    public void SetChargeZoom(bool state)
    {
        isCharging = state;
    }

    public void TriggerChargeKick()
    {
        doChargeKick = true;
        StopCoroutine("ResetKick");
        StartCoroutine(ResetKick());
    }

    private IEnumerator ResetKick()
    {
        yield return new WaitForSeconds(0.15f);

        doChargeKick = false;
    }

    public void AddScreenShake(float strength, float returnSpeed, float duration)
    {
        screenShakeStrength = strength;
        screenShakeReturnSpeed = returnSpeed;
        screenShakeTime = duration;
    }

    public void ResetShake()
    {
        screenShakeTime = 0f;
        screenShakeStrength = 0f;
        screenShakeOffset = Vector2.zero;
    }

    public void SetFrozen(bool state)
    {
        frozen = state;

        if (state)
        {
            frozenPosition = transform.position;
            frozenRotation = transform.rotation;
        }
    }

    
}