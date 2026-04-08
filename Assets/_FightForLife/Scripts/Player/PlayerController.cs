using UnityEngine;
using FightForLife.Disaster;

namespace FightForLife.Player
{
    public enum PlayerMovementState
    {
        Grounded,
        Wading,
        Swimming,
        Diving
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Ground Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 12f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float gravity = -20f;

        [Header("Crouch")]
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float standHeight = 2.0f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Sprint")]
        [SerializeField] private float sprintStaminaCost = 15f;

        [Header("Water Thresholds")]
        [SerializeField] private float splashDepth = 0.3f;
        [SerializeField] private float shallowWadeDepth = 0.7f;
        [SerializeField] private float deepWadeDepth = 1.2f;

        [Header("Swimming")]
        [SerializeField] private float swimSpeed = 3f;
        [SerializeField] private float diveSpeed = 2.5f;
        [SerializeField] private float swimAcceleration = 5f;
        [SerializeField] private float waterDragFactor = 3f;
        [SerializeField] private float wadingStaminaCost = 5f;

        // Public state
        public PlayerMovementState CurrentState { get; private set; } = PlayerMovementState.Grounded;
        public Vector3 Velocity => currentVelocity;
        public float CurrentSpeed => new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
        public bool IsGrounded => isGrounded;
        public bool IsSwimming => CurrentState == PlayerMovementState.Swimming;
        public bool IsDiving => CurrentState == PlayerMovementState.Diving;
        public bool IsWading => CurrentState == PlayerMovementState.Wading;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public float WaterDepth => waterDepth;

        // Raw input for animation (no rotation applied)
        public float InputX { get; private set; }
        public float InputZ { get; private set; }

        private CharacterController cc;
        private PlayerHealth playerHealth;
        private Transform camTransform;

        private Vector3 currentVelocity;
        private float verticalVelocity;
        private float targetHeight;
        private float waterDepth;
        private float waterSurfaceY;
        private bool isGrounded;
        private bool isSprinting;
        private bool isCrouching;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            playerHealth = GetComponent<PlayerHealth>();
            targetHeight = standHeight;
        }

        private void Start()
        {
            if (UnityEngine.Camera.main != null)
                camTransform = UnityEngine.Camera.main.transform;
        }

        private void Update()
        {
            if (!playerHealth.IsAlive) return;

            DetectWater();
            UpdateMovementState();

            switch (CurrentState)
            {
                case PlayerMovementState.Grounded:
                    HandleGroundMovement();
                    break;
                case PlayerMovementState.Wading:
                    HandleWadingMovement();
                    break;
                case PlayerMovementState.Swimming:
                    HandleSwimmingMovement();
                    break;
                case PlayerMovementState.Diving:
                    HandleDivingMovement();
                    break;
            }

            UpdateCrouch();
        }

        private void DetectWater()
        {
            float feetY = transform.position.y;
            if (FloodManager.Instance != null)
            {
                waterSurfaceY = FloodManager.Instance.WaterLevel;
                waterDepth = Mathf.Max(0f, waterSurfaceY - feetY);
            }
            else
            {
                waterDepth = 0f;
            }
        }

        private void UpdateMovementState()
        {
            PlayerMovementState prev = CurrentState;

            if (waterDepth >= deepWadeDepth)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                    CurrentState = PlayerMovementState.Diving;
                else
                    CurrentState = PlayerMovementState.Swimming;
            }
            else if (waterDepth >= splashDepth)
            {
                CurrentState = PlayerMovementState.Wading;
            }
            else
            {
                CurrentState = PlayerMovementState.Grounded;
            }

            playerHealth.IsSubmerged = CurrentState == PlayerMovementState.Diving;

            if (prev != CurrentState && (CurrentState == PlayerMovementState.Swimming || CurrentState == PlayerMovementState.Diving))
            {
                verticalVelocity = 0f;
                isCrouching = false;
                targetHeight = standHeight;
            }
        }

        /// <summary>
        /// Convert WASD input to world-space direction relative to camera.
        /// Player does NOT rotate — only moves in camera-relative directions.
        /// </summary>
        private Vector3 GetCameraRelativeInput(out float rawH, out float rawV)
        {
            rawH = Input.GetAxisRaw("Horizontal");
            rawV = Input.GetAxisRaw("Vertical");

            // Store for animation
            InputX = rawH;
            InputZ = rawV;

            if (camTransform == null)
                return new Vector3(rawH, 0f, rawV).normalized;

            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            return (camForward * rawV + camRight * rawH).normalized;
        }

        private void HandleGroundMovement()
        {
            isGrounded = cc.isGrounded;
            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            float rawH, rawV;
            Vector3 inputDir = GetCameraRelativeInput(out rawH, out rawV);
            bool hasInput = inputDir.sqrMagnitude > 0.01f;

            // Sprint
            isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && hasInput;
            if (isSprinting && !playerHealth.UseStamina(sprintStaminaCost * Time.deltaTime))
                isSprinting = false;

            // Crouch toggle
            if (Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching;
                targetHeight = isCrouching ? crouchHeight : standHeight;
            }

            float targetSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            float currentHSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;

            float speed;
            if (hasInput)
                speed = Mathf.MoveTowards(currentHSpeed, targetSpeed, acceleration * Time.deltaTime);
            else
                speed = Mathf.MoveTowards(currentHSpeed, 0f, deceleration * Time.deltaTime);

            Vector3 moveDir = inputDir * speed;

            // Jump
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                verticalVelocity = jumpForce;
                isGrounded = false;
            }

            verticalVelocity += gravity * Time.deltaTime;
            moveDir.y = verticalVelocity;

            cc.Move(moveDir * Time.deltaTime);
            currentVelocity = cc.velocity;

            // Player rotation is controlled by ThirdPersonCamera (mouse input)
        }

        private void HandleWadingMovement()
        {
            isGrounded = cc.isGrounded;
            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            float rawH, rawV;
            Vector3 inputDir = GetCameraRelativeInput(out rawH, out rawV);

            isSprinting = false;
            isCrouching = false;

            float speedMul = waterDepth < shallowWadeDepth ? 0.6f : 0.3f;
            float targetSpeed = walkSpeed * speedMul;

            if (waterDepth >= shallowWadeDepth)
                playerHealth.UseStamina(wadingStaminaCost * Time.deltaTime);

            float currentHSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
            float speed = inputDir.sqrMagnitude > 0.01f
                ? Mathf.MoveTowards(currentHSpeed, targetSpeed, acceleration * 0.6f * Time.deltaTime)
                : Mathf.MoveTowards(currentHSpeed, 0f, deceleration * 0.5f * Time.deltaTime);

            Vector3 moveDir = inputDir * speed;
            ApplyWaterCurrent(ref moveDir);

            verticalVelocity += gravity * Time.deltaTime;
            moveDir.y = verticalVelocity;

            cc.Move(moveDir * Time.deltaTime);
            currentVelocity = cc.velocity;
        }

        private void HandleSwimmingMovement()
        {
            isGrounded = false;

            float rawH, rawV;
            Vector3 inputDir = GetCameraRelativeInput(out rawH, out rawV);

            float targetSpeed = inputDir.sqrMagnitude > 0.01f ? swimSpeed : 0f;
            float currentHSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
            float speed = Mathf.MoveTowards(currentHSpeed, targetSpeed, swimAcceleration * Time.deltaTime);

            Vector3 moveDir = inputDir * speed;

            float targetY = 0f;
            if (Input.GetKey(KeyCode.Space))
                targetY = 1.5f;
            else if (Input.GetKey(KeyCode.LeftControl))
                targetY = -diveSpeed;
            else
            {
                float surfaceTargetY = waterSurfaceY - (standHeight * 0.6f);
                targetY = (surfaceTargetY - transform.position.y) * 3f;
            }

            moveDir.y = Mathf.Lerp(currentVelocity.y, targetY, waterDragFactor * Time.deltaTime);
            ApplyWaterCurrent(ref moveDir);

            cc.Move(moveDir * Time.deltaTime);
            currentVelocity = cc.velocity;
        }

        private void HandleDivingMovement()
        {
            isGrounded = false;

            float rawH, rawV;
            Vector3 inputDir = GetCameraRelativeInput(out rawH, out rawV);

            float targetSpeed = inputDir.sqrMagnitude > 0.01f ? diveSpeed : 0f;
            float speed = Mathf.MoveTowards(currentVelocity.magnitude, targetSpeed, swimAcceleration * Time.deltaTime);

            Vector3 moveDir = inputDir * speed;

            float vertTarget = 0f;
            if (Input.GetKey(KeyCode.Space)) vertTarget = diveSpeed;
            else if (Input.GetKey(KeyCode.LeftControl)) vertTarget = -diveSpeed;

            moveDir.y = Mathf.Lerp(currentVelocity.y, vertTarget, waterDragFactor * Time.deltaTime);
            ApplyWaterCurrent(ref moveDir, 0.5f);

            cc.Move(moveDir * Time.deltaTime);
            currentVelocity = cc.velocity;
        }

        private void ApplyWaterCurrent(ref Vector3 move, float multiplier = 1f)
        {
            if (FloodManager.Instance == null) return;
            float strength = FloodManager.Instance.GetCurrentStrength();
            Vector2 flowDir2D = FloodManager.Instance.GetFlowDirection();
            Vector3 flowDir = new Vector3(flowDir2D.x, 0f, flowDir2D.y);
            float submersionFactor = Mathf.Clamp01(waterDepth / deepWadeDepth);
            move += flowDir * (strength * submersionFactor * multiplier * Time.deltaTime);
        }

        private void UpdateCrouch()
        {
            float currentHeight = cc.height;
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            if (Mathf.Abs(newHeight - currentHeight) > 0.001f)
            {
                float heightDiff = newHeight - currentHeight;
                cc.height = newHeight;
                cc.center = new Vector3(0f, newHeight * 0.5f, 0f);
                if (heightDiff < 0f && isGrounded)
                    transform.position += new Vector3(0f, heightDiff * 0.5f, 0f);
            }
        }
    }
}
