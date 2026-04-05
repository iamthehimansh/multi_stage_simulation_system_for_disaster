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
        // ───────────────────────── Ground Movement ─────────────────────────
        [Header("Ground Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 12f;
        [SerializeField] private float rotationSmoothTime = 0.1f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Header("Crouch")]
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float standHeight = 2.0f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Sprint")]
        [SerializeField] private float sprintStaminaCost = 15f;

        // ───────────────────────── Water / Swimming ────────────────────────
        [Header("Water Thresholds (meters)")]
        [SerializeField] private float splashDepth = 0.3f;
        [SerializeField] private float shallowWadeDepth = 0.7f;
        [SerializeField] private float deepWadeDepth = 1.2f;

        [Header("Swimming")]
        [SerializeField] private float swimSpeed = 3f;
        [SerializeField] private float diveSpeed = 2.5f;
        [SerializeField] private float swimAcceleration = 5f;
        [SerializeField] private float surfaceBobAmplitude = 0.1f;
        [SerializeField] private float surfaceBobFrequency = 1.5f;
        [SerializeField] private float waterDragFactor = 3f;
        [SerializeField] private float wadingStaminaCost = 5f;
        [SerializeField] private float wadeSpeedShallow = 0.6f;
        [SerializeField] private float wadeSpeedDeep = 0.3f;

        [Header("Water Detection")]
        [SerializeField] private float feetOffsetY = 0f;

        // ───────────────────────── References ──────────────────────────────
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        // ───────────────────────── Public State ────────────────────────────
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

        // ───────────────────────── Private ─────────────────────────────────
        private CharacterController characterController;
        private PlayerHealth playerHealth;

        private Vector3 currentVelocity;
        private Vector3 moveDirection;
        private float verticalVelocity;
        private float rotationVelocity;
        private float targetHeight;
        private float waterDepth;
        private float waterSurfaceY;
        private bool isGrounded;
        private bool isSprinting;
        private bool isCrouching;
        private bool swimModeActive;

        // ───────────────────────── Lifecycle ───────────────────────────────

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerHealth = GetComponent<PlayerHealth>();
            targetHeight = standHeight;
        }

        private void Start()
        {
            if (cameraTransform == null)
            {
                var cam = UnityEngine.Camera.main;
                if (cam != null) cameraTransform = cam.transform;
            }
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

        // ───────────────────────── Water Detection ─────────────────────────

        private void DetectWater()
        {
            float feetY = transform.position.y + feetOffsetY;

            if (FloodManager.Instance != null)
            {
                waterSurfaceY = FloodManager.Instance.WaterLevel;
                waterDepth = Mathf.Max(0f, waterSurfaceY - feetY);
            }
            else
            {
                waterDepth = 0f;
                waterSurfaceY = float.MinValue;
            }
        }

        private void UpdateMovementState()
        {
            PlayerMovementState previousState = CurrentState;

            if (waterDepth >= deepWadeDepth)
            {
                if (Input.GetKey(KeyCode.LeftControl) && swimModeActive)
                {
                    CurrentState = PlayerMovementState.Diving;
                }
                else
                {
                    CurrentState = PlayerMovementState.Swimming;
                }
                swimModeActive = true;
            }
            else if (waterDepth >= splashDepth)
            {
                CurrentState = PlayerMovementState.Wading;
                swimModeActive = false;
            }
            else
            {
                CurrentState = PlayerMovementState.Grounded;
                swimModeActive = false;
            }

            // Update submersion flag on PlayerHealth
            playerHealth.IsSubmerged = CurrentState == PlayerMovementState.Diving;

            if (previousState != CurrentState)
            {
                OnStateTransition(previousState, CurrentState);
            }
        }

        private void OnStateTransition(PlayerMovementState from, PlayerMovementState to)
        {
            // Reset vertical velocity when entering/leaving water
            if (to == PlayerMovementState.Swimming || to == PlayerMovementState.Diving)
            {
                verticalVelocity = 0f;
            }

            // Un-crouch when entering water
            if (to != PlayerMovementState.Grounded && isCrouching)
            {
                isCrouching = false;
                targetHeight = standHeight;
            }
        }

        // ───────────────────────── Input Helpers ───────────────────────────

        private Vector3 GetCameraRelativeInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(h, 0f, v).normalized;

            if (cameraTransform == null) return input;

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            return (camForward * v + camRight * h).normalized;
        }

        private void RotateTowardMovement(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return;

            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        // ───────────────────────── Ground Movement ─────────────────────────

        private void HandleGroundMovement()
        {
            isGrounded = characterController.isGrounded;

            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            Vector3 inputDir = GetCameraRelativeInput();

            // Sprint
            isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && inputDir.sqrMagnitude > 0.01f;
            if (isSprinting && !playerHealth.UseStamina(sprintStaminaCost * Time.deltaTime))
                isSprinting = false;

            // Crouch toggle
            if (Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching;
                targetHeight = isCrouching ? crouchHeight : standHeight;
            }

            // Target speed
            float targetSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            float currentHorizontalSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;

            float speed;
            if (inputDir.sqrMagnitude > 0.01f)
                speed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, acceleration * Time.deltaTime);
            else
                speed = Mathf.MoveTowards(currentHorizontalSpeed, 0f, deceleration * Time.deltaTime);

            moveDirection = inputDir * speed;

            // Jump
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                verticalVelocity = jumpForce;
                isGrounded = false;
            }

            verticalVelocity += gravity * Time.deltaTime;
            moveDirection.y = verticalVelocity;

            characterController.Move(moveDirection * Time.deltaTime);
            currentVelocity = characterController.velocity;

            RotateTowardMovement(inputDir);
        }

        // ───────────────────────── Wading ──────────────────────────────────

        private void HandleWadingMovement()
        {
            isGrounded = characterController.isGrounded;

            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            Vector3 inputDir = GetCameraRelativeInput();

            // No sprint in water
            isSprinting = false;
            isCrouching = false;

            float speedMultiplier = waterDepth < shallowWadeDepth ? wadeSpeedShallow : wadeSpeedDeep;
            float targetSpeed = walkSpeed * speedMultiplier;

            // Deep wading drains stamina
            if (waterDepth >= shallowWadeDepth)
                playerHealth.UseStamina(wadingStaminaCost * Time.deltaTime);

            float currentHorizontalSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;

            float speed;
            if (inputDir.sqrMagnitude > 0.01f)
                speed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, acceleration * 0.6f * Time.deltaTime);
            else
                speed = Mathf.MoveTowards(currentHorizontalSpeed, 0f, deceleration * 0.5f * Time.deltaTime);

            moveDirection = inputDir * speed;

            // Apply water current
            ApplyWaterCurrent(ref moveDirection);

            verticalVelocity += gravity * Time.deltaTime;
            moveDirection.y = verticalVelocity;

            characterController.Move(moveDirection * Time.deltaTime);
            currentVelocity = characterController.velocity;

            RotateTowardMovement(inputDir);
        }

        // ───────────────────────── Swimming ────────────────────────────────

        private void HandleSwimmingMovement()
        {
            isGrounded = false;

            Vector3 inputDir = GetCameraRelativeInput();
            isSprinting = false;
            isCrouching = false;

            float targetSpeed = inputDir.sqrMagnitude > 0.01f ? swimSpeed : 0f;

            Vector3 horizontalVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            float currentHorizontalSpeed = horizontalVel.magnitude;
            float speed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, swimAcceleration * Time.deltaTime);

            moveDirection = inputDir * speed;

            // Vertical: Space to stay at/go to surface, Ctrl to start diving
            float targetY = 0f;
            if (Input.GetKey(KeyCode.Space))
            {
                targetY = 1.5f; // Upward force
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                targetY = -diveSpeed; // Transition to diving handled by state
            }
            else
            {
                // Bob at surface
                float surfaceTargetY = waterSurfaceY - (standHeight * 0.6f);
                float diff = surfaceTargetY - transform.position.y;
                targetY = diff * 3f;

                // Add bobbing
                targetY += Mathf.Sin(Time.time * surfaceBobFrequency * Mathf.PI * 2f) * surfaceBobAmplitude;
            }

            moveDirection.y = Mathf.Lerp(currentVelocity.y, targetY, waterDragFactor * Time.deltaTime);

            // Apply water current
            ApplyWaterCurrent(ref moveDirection);

            characterController.Move(moveDirection * Time.deltaTime);
            currentVelocity = characterController.velocity;

            RotateTowardMovement(inputDir);
        }

        // ───────────────────────── Diving ──────────────────────────────────

        private void HandleDivingMovement()
        {
            isGrounded = false;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // In dive mode, we use full 3D movement relative to camera
            Vector3 inputDir = Vector3.zero;
            if (cameraTransform != null)
            {
                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                inputDir = (camForward * v + camRight * h).normalized;
            }
            else
            {
                inputDir = new Vector3(h, 0f, v).normalized;
            }

            isSprinting = false;
            isCrouching = false;

            float targetSpeed = inputDir.sqrMagnitude > 0.01f ? diveSpeed : 0f;
            float speed = Mathf.MoveTowards(currentVelocity.magnitude, targetSpeed, swimAcceleration * Time.deltaTime);

            moveDirection = inputDir * speed;

            // Vertical: Space up, Ctrl down
            float vertTarget = 0f;
            if (Input.GetKey(KeyCode.Space))
                vertTarget = diveSpeed;
            else if (Input.GetKey(KeyCode.LeftControl))
                vertTarget = -diveSpeed;

            moveDirection.y = Mathf.Lerp(currentVelocity.y, vertTarget, waterDragFactor * Time.deltaTime);

            // Apply water current (reduced underwater)
            ApplyWaterCurrent(ref moveDirection, 0.5f);

            characterController.Move(moveDirection * Time.deltaTime);
            currentVelocity = characterController.velocity;

            // Rotate toward horizontal movement only
            Vector3 flatDir = new Vector3(moveDirection.x, 0f, moveDirection.z);
            RotateTowardMovement(flatDir);
        }

        // ───────────────────────── Water Current ───────────────────────────

        private void ApplyWaterCurrent(ref Vector3 move, float multiplier = 1f)
        {
            if (FloodManager.Instance == null) return;

            float strength = FloodManager.Instance.GetCurrentStrength();
            Vector2 flowDir2D = FloodManager.Instance.GetFlowDirection();
            Vector3 flowDir = new Vector3(flowDir2D.x, 0f, flowDir2D.y);

            // Scale current effect by how submerged the player is
            float submersionFactor = Mathf.Clamp01(waterDepth / deepWadeDepth);
            move += flowDir * (strength * submersionFactor * multiplier * Time.deltaTime);
        }

        // ───────────────────────── Crouch ──────────────────────────────────

        private void UpdateCrouch()
        {
            float currentHeight = characterController.height;
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

            if (Mathf.Abs(newHeight - currentHeight) > 0.001f)
            {
                float heightDiff = newHeight - currentHeight;
                characterController.height = newHeight;
                characterController.center = new Vector3(0f, newHeight * 0.5f, 0f);

                // Push player down when crouching to keep feet planted
                if (heightDiff < 0f && isGrounded)
                {
                    transform.position += new Vector3(0f, heightDiff * 0.5f, 0f);
                }
            }
        }
    }
}
