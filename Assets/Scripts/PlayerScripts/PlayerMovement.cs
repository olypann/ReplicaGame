using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("player")]
    public float movementSpeed = 2.0f;
    public float sprintSpeed = 5.335f;
    public float RotationSmoothTime = 0.12f;
    public float accelerationRate = 10.0f;

    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;


    [Header("ground")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;


    [Header("camera")]
    public GameObject cameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -20.0f;


    [Header("animation")]
    public Animator animator;

    private int animSpeed;
    private int animJump;
    private int animGrounded;

    private int animNearGround;
    private int animLand;
    private int animHardLand;


    [Header("landing detection")]
    [SerializeField] private float nearGroundDistance = 1.2f;

    private bool wasGrounded;
    private bool isNearGround;


    [Header("dodge")]
    [SerializeField] private float dodgeSpeed = 12f;
    [SerializeField] private float dodgeDuration = 0.2f;
    [SerializeField] private float doubleTapTime = 0.25f;
    [SerializeField] private float dodgeStaminaCost = 15f;


    [Header("dodge vfx")]
    [SerializeField] private GameObject dodgeVFX;
    [SerializeField] private Transform dodgeVFXSpawnPoint;



    private CharacterController controller;
    private GameObject cameraObject;
    private PlayerCombat combat;
    private ThirdPersonCamera cam;


    private float yaw;
    private float pitch;

    private float speed;
    private float targetRotation;
    private float rotationVelocity;
    private float verticalSpeed;

    private float jumpTimer;
    private float fallTimer;

    private const float terminalVelocity = 53.0f;


    private float lastATapTime;
    private float lastDTapTime;
    private float lastSTapTime;
    private float lastWTapTime;

    private bool isDodging;
    private float dodgeTimer;
    private Vector3 dodgeDirection;

    private int animDodgeLeft;
    private int animDodgeRight;
    private int animDodgeForward;
    private int animDodgeBack;


    public bool isPossessed;



    private void Awake()
    {
        // grab camera once
        cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
    }


    private void Start()
    {
        controller = GetComponent<CharacterController>();
        combat = GetComponent<PlayerCombat>();

        cam = cameraObject.GetComponent<ThirdPersonCamera>();

        yaw = cameraTarget.transform.eulerAngles.y;

        jumpTimer = JumpTimeout;
        fallTimer = FallTimeout;

        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // animator params
        animSpeed = Animator.StringToHash("Speed");
        animJump = Animator.StringToHash("Jump");
        animGrounded = Animator.StringToHash("Grounded");

        animNearGround = Animator.StringToHash("NearGround");
        animLand = Animator.StringToHash("Land");
        animHardLand = Animator.StringToHash("HardLand");

        animDodgeLeft = Animator.StringToHash("DodgeLeft");
        animDodgeRight = Animator.StringToHash("DodgeRight");
        animDodgeForward = Animator.StringToHash("DodgeForward");
        animDodgeBack = Animator.StringToHash("DodgeBack");
    }



    // used by abilities to lock player movement
    private bool IsMovementLocked()
    {
        return AbilityStateManager.Instance != null &&
        (
            AbilityStateManager.Instance.isCameraThrowActive ||
            AbilityStateManager.Instance.isFreezeAbilityActive ||
            isPossessed ||
            (GetComponent<PossessionAbility>() != null && GetComponent<PossessionAbility>().IsPossessing)
        );
    }



    private void Update()
    {
        PlayerPossessedAI ai = GetComponent<PlayerPossessedAI>();

        if (IsMovementLocked())
        {
            // only fully freeze if ai isn't controlling main charater
            if (ai == null || !ai.enabled)
            {
                speed = 0f;
                verticalSpeed = 0f;

                controller.Move(Vector3.zero);
            }

            return;
        }

        GroundCheck();
        NearGroundCheck();

        Jump();
        Movement();
        HandleDodge();
    }


    private void LateUpdate()
    {
        CameraRotation();
    }



    // input helpers
    private Vector2 GetMoveInput()
    {
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }

    private Vector2 GetLookInput()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    private bool IsJumpPressed()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    private bool IsSprintHeld()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }



    // main movement

    private void Movement()
    {
        if (isDodging)
        {
            return;
        }

        if (combat != null)
        {
            if (combat.IsAttacking() || combat.IsCharging())
            {
                // lock movement but still apply gravity
                speed = 0f;

                controller.Move(Vector3.up * verticalSpeed * Time.deltaTime);
                return;
            }
        }

        Vector2 moveInput = GetMoveInput();

        if (combat != null && combat.IsAttacking())
        {
            moveInput = Vector2.zero;
        }

        float targetSpeed = IsSprintHeld() ? sprintSpeed : movementSpeed;

        if (moveInput == Vector2.zero)
        {
            targetSpeed = 0f;
        }

        float currentSpeed = new Vector3(
            controller.velocity.x,
            0f,
            controller.velocity.z
        ).magnitude;

        float inputMagnitude = moveInput.magnitude > 0f ? 1f : 0f;

        float speedOffset = 0.1f;

        if (currentSpeed < targetSpeed - speedOffset || currentSpeed > targetSpeed + speedOffset)
        {
            speed = Mathf.Lerp(
                currentSpeed,
                targetSpeed * inputMagnitude,
                Time.deltaTime * accelerationRate
            );
        }
        else
        {
            speed = targetSpeed;
        }

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (moveInput != Vector2.zero)
        {
            targetRotation =
                Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                cameraObject.transform.eulerAngles.y;

            float rotation = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetRotation,
                ref rotationVelocity,
                RotationSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        Vector3 moveDirection =
            Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;

        controller.Move(
            moveDirection.normalized * (speed * Time.deltaTime) +
            Vector3.up * verticalSpeed * Time.deltaTime
        );

        // animation + footsteps
        if (animator != null)
        {
            float normalizedSpeed = speed / sprintSpeed;

            if (normalizedSpeed < 0.05f)
            {
                normalizedSpeed = 0f;
            }

            animator.SetFloat(animSpeed, normalizedSpeed);
        }

        if (SoundManager.Instance != null)
        {
            float normalizedSpeed = speed / sprintSpeed;
            bool isMoving = normalizedSpeed > 0.1f && Grounded;

            SoundManager.Instance.HandleFootsteps(normalizedSpeed, isMoving);
        }
    }



    // dodge system (double tap)

    private void HandleDodge()
    {
        if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;

            if (dodgeTimer <= 0f)
            {
                isDodging = false;
            }

            controller.Move(dodgeDirection * dodgeSpeed * Time.deltaTime);
            return;
        }

        if (combat != null && combat.IsAttacking())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastATapTime <= doubleTapTime)
            {
                TryDodge(-cameraObject.transform.right);
            }

            lastATapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastDTapTime <= doubleTapTime)
            {
                TryDodge(cameraObject.transform.right);
            }

            lastDTapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (Time.time - lastSTapTime <= doubleTapTime)
            {
                Vector3 forward = cameraObject.transform.forward;
                forward.y = 0f;
                forward.Normalize();

                TryDodge(-forward);
            }

            lastSTapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            if (Time.time - lastWTapTime <= doubleTapTime)
            {
                TryDodge(cameraObject.transform.forward);
            }

            lastWTapTime = Time.time;
        }
    }



    private void TryDodge(Vector3 direction)
    {
        if (combat != null)
        {
            if (combat.currentStamina < dodgeStaminaCost)
            {
                return;
            }

            combat.currentStamina -= dodgeStaminaCost;
        }

        StartDodge(direction);
    }


    private void StartDodge(Vector3 direction)
    {
        isDodging = true;
        dodgeTimer = dodgeDuration;

        direction.y = 0f;
        dodgeDirection = direction.normalized;

        PlayDodgeVFX(dodgeDirection);
        PlayDodgeAnimation(dodgeDirection);

        ThirdPersonCamera cam = cameraObject.GetComponent<ThirdPersonCamera>();

        if (cam != null)
        {
            cam.AddDodgeImpulse(dodgeDirection);
        }

        SoundManager.Instance?.PlayDodge();
    }



    private void PlayDodgeVFX(Vector3 dir)
    {
        if (dodgeVFX == null)
        {
            return;
        }

        if (dodgeVFXSpawnPoint == null)
        {
            return;
        }

        Vector3 spawnDir = -dir.normalized;

        GameObject vfx = Instantiate(
            dodgeVFX,
            dodgeVFXSpawnPoint.position,
            Quaternion.LookRotation(spawnDir)
        );

        Destroy(vfx, 1.0f);
    }


    private void PlayDodgeAnimation(Vector3 dir)
    {
        if (animator == null)
        {
            return;
        }

        Vector3 localDir = transform.InverseTransformDirection(dir);

        float x = localDir.x;
        float z = localDir.z;

        if (Mathf.Abs(x) > Mathf.Abs(z))
        {
            if (x > 0f)
            {
                animator.SetTrigger(animDodgeRight);
            }
            else
            {
                animator.SetTrigger(animDodgeLeft);
            }
        }
        else
        {
            if (z > 0f)
            {
                animator.SetTrigger(animDodgeForward);
            }
            else
            {
                animator.SetTrigger(animDodgeBack);
            }
        }
    }



    // jump and gravity
    private void Jump()
    {
        if (Grounded)
        {
            fallTimer = FallTimeout;

            if (verticalSpeed < 0f)
            {
                verticalSpeed = -2f;
            }

            if (IsJumpPressed() && jumpTimer <= 0f)
            {
                verticalSpeed = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                if (animator != null)
                {
                    animator.SetTrigger(animJump);
                }

                SoundManager.Instance?.PlayJump();
            }

            if (jumpTimer > 0f)
            {
                jumpTimer -= Time.deltaTime;
            }
        }
        else
        {
            jumpTimer = JumpTimeout;

            if (fallTimer > 0f)
            {
                fallTimer -= Time.deltaTime;
            }
        }

        if (verticalSpeed < terminalVelocity)
        {
            verticalSpeed += Gravity * Time.deltaTime;
        }
    }



    // ground checks
    private void GroundCheck()
    {
        Vector3 position = new Vector3(
            transform.position.x,
            transform.position.y - GroundedOffset,
            transform.position.z
        );

        Grounded = Physics.CheckSphere(
            position,
            GroundedRadius,
            GroundLayers,
            QueryTriggerInteraction.Ignore
        );

        bool justLanded = !wasGrounded && Grounded;

        if (animator != null)
        {
            animator.SetBool(animGrounded, Grounded);

            if (justLanded)
            {
                if (combat != null && combat.IsAttacking() && !Grounded)
                {
                    return;
                }

                if (combat != null && combat.IsInvulnerable())
                {
                    animator.SetTrigger(animHardLand);
                }
                else
                {
                    animator.SetTrigger(animLand);
                }

                SoundManager.Instance?.PlayLand();
            }
        }

        wasGrounded = Grounded;
    }


    private void NearGroundCheck()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        isNearGround = Physics.Raycast(
            origin,
            Vector3.down,
            nearGroundDistance,
            GroundLayers,
            QueryTriggerInteraction.Ignore
        );

        if (animator != null)
        {
            animator.SetBool(animNearGround, isNearGround && !Grounded);
        }
    }



    // camera look

    private void CameraRotation()
    {
        if (AbilityStateManager.Instance != null &&
            AbilityStateManager.Instance.isFreezeAbilityActive)
        {
            return;
        }

        Vector2 lookInput = GetLookInput();

        yaw += lookInput.x;
        pitch -= lookInput.y;

        pitch = Mathf.Clamp(pitch, BottomClamp, TopClamp);

        cameraTarget.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}