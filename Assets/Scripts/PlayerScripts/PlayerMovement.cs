using UnityEngine;

namespace StarterAssets
{
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
        public float BottomClamp = -30.0f;

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

        private const float terminalVelocity = 53.0f;

        [Header("Animation")]
        public Animator animator;
        private int animMove;
        private int animSpeed;
        private int animJump;
        private int animGrounded;

        private void Awake()
        {
            cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            controller = GetComponent<CharacterController>();

            yaw = cameraTarget.transform.eulerAngles.y;

            jumpTimer = JumpTimeout;
            fallTimer = FallTimeout;

            animator = GetComponentInChildren<Animator>();
            animMove = Animator.StringToHash("Move");
            animSpeed = Animator.StringToHash("Speed");
            animJump = Animator.StringToHash("Jump");
            animGrounded = Animator.StringToHash("Grounded");
        }

        private void Update()
        {
            GroundCheck();
            Jump();
            Movement();

        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        //player input

        private Vector2 GetMoveInput()
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");
            return new Vector2(x, y);
        }

        private Vector2 GetLookInput()
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            return new Vector2(x, y);
        }

        private bool IsJumpPressed()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }

        private bool IsSprintHeld()
        {
            return Input.GetKey(KeyCode.LeftShift);
        }

        //character movement

        private void Movement()
        {
            Vector2 moveInput = GetMoveInput();

            float targetSpeed;

            if (IsSprintHeld())
            {
                targetSpeed = sprintSpeed;
            }
            else
            {
                targetSpeed = movementSpeed;
            }

            if (moveInput == Vector2.zero)
            {
                targetSpeed = 0f;
            }

            float currentSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
            float inputMagnitude = moveInput.magnitude;

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


            //animation
            if (animator != null)
            {
                animator.SetFloat(animSpeed, speed);

                if (IsSprintHeld())
                {
                    animator.SetFloat(animMove, 2f);
                }
                else
                {
                    animator.SetFloat(animMove, 1f);
                }
            }
        }

        //jump / verticql movement

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


            //animation
            if (IsJumpPressed() && jumpTimer <= 0f)
            {
                verticalSpeed = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                if (animator != null)
                {
                    animator.SetTrigger(animJump);
                }
            }
        }

        //grounded check

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

            //animation
            if (animator != null)
            {
                animator.SetBool(animGrounded, Grounded);
            }
        }

        //cam rotation (remove later)

        private void CameraRotation()
        {
            Vector2 lookInput = GetLookInput();

            yaw += lookInput.x;
            pitch += lookInput.y;

            pitch = Mathf.Clamp(pitch, BottomClamp, TopClamp);

            cameraTarget.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}