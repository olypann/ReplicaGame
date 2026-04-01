using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Player")]
    public float movementSpeed = 2.0f;
    public float sprintSpeed = 5.335f;
    public float RotationSmoothTime = 0.12f;
    public float accelerationRate = 10.0f;

    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;

    [Header("Ground")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("Camera")]
    public GameObject cameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -20.0f;

    private float yaw;
    private float pitch;

    private float speed;
    private float targetRotation;
    private float rotationVelocity;
    private float verticalSpeed;

    private float jumpTimer;
    private float fallTimer;

    private CharacterController controller;
    private GameObject cameraObject;
    private PlayerCombat combat;

    private const float terminalVelocity = 53.0f;

    [Header("Landing Detection")]
    [SerializeField] private float nearGroundDistance = 1.2f;

    private bool wasGrounded;
    private bool isNearGround;

    private int animNearGround;
    private int animLand;
    private int animHardLand;

    [Header("Animation")]
    public Animator animator;
    private int animSpeed;
    private int animJump;
    private int animGrounded;

    private void Awake()
    {
        // find main camera reference
        cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        // setup references
        controller = GetComponent<CharacterController>();
        combat = GetComponent<PlayerCombat>();

        yaw = cameraTarget.transform.eulerAngles.y;

        jumpTimer = JumpTimeout;
        fallTimer = FallTimeout;

        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // cache animator hashes
        animSpeed = Animator.StringToHash("Speed");
        animJump = Animator.StringToHash("Jump");
        animGrounded = Animator.StringToHash("Grounded");

        animNearGround = Animator.StringToHash("NearGround");
        animLand = Animator.StringToHash("Land");
        animHardLand = Animator.StringToHash("HardLand");
    }

    private void Update()
    {
        // main loop
        GroundCheck();
        NearGroundCheck();
        Jump();
        Movement();
    }

    private void LateUpdate()
    {
        // camera update
        CameraRotation();
    }

    // INPUT

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

    // MOVEMENT

    private void Movement()
    {
        Vector2 moveInput = GetMoveInput();

        // lock movement during attack
        if (combat != null && combat.IsAttacking())
        {
            moveInput = Vector2.zero;
        }

        float targetSpeed = IsSprintHeld() ? sprintSpeed : movementSpeed;

        if (moveInput == Vector2.zero)
        {
            targetSpeed = 0f;
        }

        float currentSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;

        float inputMagnitude = moveInput.magnitude > 0f ? 1f : 0f;

        float speedOffset = 0.1f;

        if (currentSpeed < targetSpeed - speedOffset || currentSpeed > targetSpeed + speedOffset)
        {
            speed = Mathf.Lerp(currentSpeed, targetSpeed * inputMagnitude, Time.deltaTime * accelerationRate);
        }
        else
        {
            speed = targetSpeed;
        }

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (moveInput != Vector2.zero)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraObject.transform.eulerAngles.y;

            float rotation = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetRotation,
                ref rotationVelocity,
                RotationSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        Vector3 moveDirection = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;

        controller.Move(
            moveDirection.normalized * (speed * Time.deltaTime) +
            Vector3.up * verticalSpeed * Time.deltaTime
        );

        // animation speed update
        if (animator != null)
        {
            float normalizedSpeed = speed / sprintSpeed;

            if (normalizedSpeed < 0.05f)
            {
                normalizedSpeed = 0f;
            }

            animator.SetFloat(animSpeed, normalizedSpeed);
        }
    }

    // JUMP

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

    // GROUND CHECK

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

    private void CameraRotation()
    {
        Vector2 lookInput = GetLookInput();

        yaw += lookInput.x;
        pitch -= lookInput.y;

        pitch = Mathf.Clamp(pitch, BottomClamp, TopClamp);

        cameraTarget.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}