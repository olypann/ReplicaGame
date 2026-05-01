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

    [Header("camera obstacle")]
    [SerializeField] private float obstacleRadius = 0.25f;
    [SerializeField] private float obstacleBuffer = 0.1f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("dodge impulse")]
    [SerializeField] private float forwardImpulse = 0.35f;
    [SerializeField] private float sideImpulse = 0.25f;
    [SerializeField] private float backImpulse = 0.15f;

    [Header("return feel")]
    [SerializeField] private float impulseReturnSpeed = 10f;
    [SerializeField] private float overshootStrength = 0.15f;

    [Header("charge zoom")]
    [SerializeField] private float chargeZoomFOV = 55f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomSmoothSpeed = 6f;

    [SerializeField] private float chargedAttackKickFOV = 50f;
    [SerializeField] private float kickReturnSpeed = 10f;

    [Header("dynamic distance")]
    [SerializeField] private float normalDistance = 3.5f;
    [SerializeField] private float sprintDistance = 4.2f;
    [SerializeField] private float combatDistance = 3.0f;

    [SerializeField] private float distanceSmooth = 8f;

    [Header("dynamic height")]
    [SerializeField] private float normalHeight = 1.5f;
    [SerializeField] private float combatHeight = 1.3f;

    [SerializeField] private float heightSmooth = 8f;

    [Header("subtle motion")]
    [SerializeField] private float swayAmount = 0.05f;
    [SerializeField] private float swaySpeed = 1.5f;

    [SerializeField] private float breatheAmount = 0.03f;
    [SerializeField] private float breatheSpeed = 1.2f;

    [Header("aim breathing")]
    [SerializeField] private float aimBreathAmount = 0.03f;
    [SerializeField] private float aimBreathSpeed = 2.5f;

    [Header("target blending")]
    [SerializeField] private float targetBlendSpeed = 6f;

    [Header("camera throw fx")]
    [SerializeField] private float throwSwayAmount = 0.05f;
    [SerializeField] private float throwSwaySpeed = 2f;
    [SerializeField] private float throwSwayReturnSpeed = 6f;

    [SerializeField] private float throwImpulseReturnSpeed = 14f;

    [Header("camera hit vfx")]
    [SerializeField] private GameObject cameraHitVFX;
    [SerializeField] private Transform cameraHitVFXAnchor;
    [SerializeField] private float cameraHitVFXDistance = 1.5f;


    private float yaw;
    private float pitch = 10f;

    private float motionTime;

    private float targetDistance;
    private float targetHeight;

    private Vector3 lastPosition;

    private Vector3 dodgeImpulse;
    private Vector3 externalImpulse;

    private Vector3 throwCameraImpulse;

    private Vector3 throwSwayOffset;
    private Vector3 throwSwayVelocity;

    private Vector3 shakeOffset;

    private float externalYaw;
    private float externalPitch;
    private float externalRoll;

    private float externalImpulseReturnSpeed = 6f;
    private float rollReturnSpeed = 3f;

    private float screenShakeTime;
    private float screenShakeStrength;
    private float screenShakeReturnSpeed;
    private Vector2 screenShakeOffset;

    private float currentFOVVelocity;

    private bool isCharging;
    private bool doChargeKick;

    private bool frozen;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    private Transform currentTarget;
    private Transform targetGoal;
    private float targetBlendT;

    private Transform runtimeHitAnchor;

    private PlayerCombat combat;
    private PlayerMovement movement;


    private void Start()
    {
        // lock cursor on start
        Cursor.lockState = CursorLockMode.Locked;

        lastPosition = transform.position;

        // grab refs from target
        combat = target.GetComponent<PlayerCombat>();
        movement = target.GetComponent<PlayerMovement>();

        // setup target blending
        currentTarget = target;
        targetGoal = target;

        // create anchor if none assigned
        if (cameraHitVFXAnchor == null)
        {
            GameObject go = new GameObject("CameraHitVFXAnchor");
            go.transform.SetParent(transform);
            cameraHitVFXAnchor = go.transform;
        }
    }


    private void LateUpdate()
    {
        // camera throw override takes full control
        if (AbilityStateManager.Instance != null &&
            (AbilityStateManager.Instance.isCameraThrowActive ||
             AbilityStateManager.Instance.isCameraReturning))
        {
            ApplyAbilityCameraOverride();
            return;
        }

        // freeze mode just unlocks cursor and stops movement
        if (AbilityStateManager.Instance != null &&
            AbilityStateManager.Instance.isFreezeAbilityActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // fully frozen camera
        if (frozen)
        {
            transform.position = frozenPosition;
            transform.rotation = frozenRotation;
            return;
        }

        if (currentTarget == null)
        {
            return;
        }

        Vector3 blendedTargetPos = GetBlendedTargetPosition();

        // mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX + externalYaw;

        float newPitch = pitch - mouseY + externalPitch;
        newPitch = Mathf.Clamp(newPitch, -80f, 60f);

        bool attacking = combat != null && combat.IsAttacking();
        bool charging = combat != null && combat.IsCharging();

        // prevent clipping through ground
        Quaternion testRot = Quaternion.Euler(newPitch, yaw, 0);
        Vector3 testOffset = testRot * new Vector3(0, height, -distance);
        Vector3 testPos = blendedTargetPos + testOffset;

        Vector3 dir = testPos - blendedTargetPos;
        float dist = dir.magnitude;

        if (!Physics.Raycast(blendedTargetPos, dir.normalized, dist, groundLayer))
        {
            pitch = newPitch;
        }

        // dynamic distance
        targetDistance = normalDistance;

        if (attacking || charging)
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

        // smooth distance + height
        distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * distanceSmooth);

        height = Mathf.Lerp(
            height,
            attacking || charging ? combatHeight : normalHeight,
            Time.deltaTime * heightSmooth
        );

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 offset = rotation * new Vector3(0, height, -distance);

        // subtle movement (sway + breathing)
        motionTime += Time.deltaTime;

        float swayX = Mathf.Sin(motionTime * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(motionTime * breatheSpeed) * breatheAmount;

        Vector3 subtleOffset = new Vector3(swayX, swayY, 0f);

        Vector3 desiredPosition =
            blendedTargetPos +
            offset +
            subtleOffset +
            dodgeImpulse +
            externalImpulse;

        // obstacle handling
        Vector3 dirToCam = offset.normalized;
        float camDist = offset.magnitude;

        if (Physics.SphereCast(
            blendedTargetPos,
            obstacleRadius,
            dirToCam,
            out RaycastHit hit,
            camDist,
            obstacleLayers))
        {
            desiredPosition =
                blendedTargetPos +
                (dirToCam * (hit.distance - obstacleBuffer)) +
                dodgeImpulse +
                externalImpulse;
        }

        // slight overshoot based on movement
        Vector3 frameVel = (transform.position - lastPosition) / Time.deltaTime;
        Vector3 overshoot = frameVel * overshootStrength;

        // throw sway
        Vector3 targetSway =
            transform.right * Mathf.Sin(Time.time * throwSwaySpeed) * throwSwayAmount +
            transform.up * Mathf.Cos(Time.time * throwSwaySpeed * 0.8f) * (throwSwayAmount * 0.5f);

        throwSwayOffset = Vector3.Lerp(
            throwSwayOffset,
            targetSway,
            Time.deltaTime * throwSwayReturnSpeed
        );

        Vector3 basePos = desiredPosition + overshoot + throwSwayOffset + throwCameraImpulse;

        // smooth position
        Vector3 finalPos = Vector3.Lerp(
            transform.position,
            basePos,
            Time.deltaTime * smoothSpeed
        );

        // apply shake at the end so it doesnt get smoothed out
        finalPos += new Vector3(shakeOffset.x, shakeOffset.y, 0f);

        transform.position = finalPos;

        //decay stuff over time
        shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * 20f);

        dodgeImpulse = Vector3.Lerp(dodgeImpulse, Vector3.zero, Time.deltaTime * impulseReturnSpeed);
        externalImpulse = Vector3.Lerp(externalImpulse, Vector3.zero, Time.deltaTime * externalImpulseReturnSpeed);

        externalRoll = Mathf.Lerp(externalRoll, 0f, Time.deltaTime * rollReturnSpeed);
        externalYaw = Mathf.Lerp(externalYaw, 0f, Time.deltaTime * rollReturnSpeed);
        externalPitch = Mathf.Lerp(externalPitch, 0f, Time.deltaTime * rollReturnSpeed);

        lastPosition = transform.position;

        // fov stuff
        float targetFOV = normalFOV;

        if (charging)
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

        // screen shake
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

        UpdateHitVFXAnchor();

        // final rotation
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

        // blend instead of snapping
        currentTarget = targetGoal != null ? targetGoal : target;
        targetGoal = newTarget;
        targetBlendT = 0f;
    }


    public void AddDodgeImpulse(Vector3 dodgeDirection)
    {
        Vector3 dir = -dodgeDirection;
        dir.y = 0f;
        dir.Normalize();

        float strength = sideImpulse;

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


    public void AddShake(Vector3 amount)
    {
        shakeOffset += amount;
    }


    public float GetYaw()
    {
        return yaw;
    }


    private void ApplyAbilityCameraOverride()
    {
        // slight breathing + shake when camera is "held"
        shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * 20f);

        float breathe = Mathf.Sin(Time.time * aimBreathSpeed) * aimBreathAmount;

        Vector3 breatheOffset = transform.forward * breathe;

        transform.position += shakeOffset + breatheOffset;
    }


    public void AddThrowCameraImpulse(Vector3 impulse)
    {
        throwCameraImpulse += impulse;
    }


    public void ClearThrowCameraImpulse()
    {
        throwCameraImpulse = Vector3.zero;
    }


    public void ResetAfterCameraThrow()
    {
        throwCameraImpulse = Vector3.zero;
        throwSwayOffset = Vector3.zero;
        throwSwayVelocity = Vector3.zero;

        shakeOffset = Vector3.zero;

        lastPosition = transform.position;
    }


    public void RestoreNormalControl()
    {
        frozen = false;

        // prevents snapping when switching back
        currentTarget = transform;
        targetGoal = target;
        targetBlendT = 0f;

        shakeOffset = Vector3.zero;
        dodgeImpulse = Vector3.zero;
        externalImpulse = Vector3.zero;
        throwCameraImpulse = Vector3.zero;

        lastPosition = transform.position;
    }


    public void SnapToTargetInstant(Transform newTarget)
    {
        if (newTarget == null)
        {
            return;
        }

        currentTarget = newTarget;
        targetGoal = newTarget;
        target = newTarget;

        targetBlendT = 1f;

        Vector3 blendedTargetPos = newTarget.position;
        Vector3 offset = transform.position - blendedTargetPos;

        transform.position = blendedTargetPos + offset;
    }


    private void UpdateHitVFXAnchor()
    {
        if (cameraHitVFXAnchor == null)
        {
            return;
        }

        cameraHitVFXAnchor.position =
            transform.position +
            transform.forward * cameraHitVFXDistance;

        cameraHitVFXAnchor.rotation = transform.rotation;
    }


    public void PlayCameraHitVFX()
    {
        if (cameraHitVFX == null || cameraHitVFXAnchor == null)
        {
            return;
        }

        Instantiate(
            cameraHitVFX,
            cameraHitVFXAnchor.position,
            cameraHitVFXAnchor.rotation
        );
    }
}