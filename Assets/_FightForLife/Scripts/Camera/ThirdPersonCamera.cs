using UnityEngine;

namespace FightForLife.Camera
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        // ───────────────────────── Follow Target ───────────────────────────
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Distance")]
        [SerializeField] private float defaultDistance = 5f;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float zoomSmoothTime = 0.15f;

        [Header("Orbit / Mouse Look")]
        [SerializeField] private float mouseSensitivityX = 3f;
        [SerializeField] private float mouseSensitivityY = 2f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 75f;
        [SerializeField] private bool alwaysOrbit = true;

        [Header("Follow Smoothing")]
        [SerializeField] private float followSmoothTime = 0.08f;

        [Header("Collision Avoidance")]
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionSmoothTime = 0.05f;

        [Header("Dynamic FOV")]
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private float sprintFOV = 72f;
        [SerializeField] private float fovSmoothTime = 0.3f;

        [Header("Underwater")]
        [SerializeField] private Color underwaterTintColor = new Color(0.15f, 0.35f, 0.55f, 0.4f);

        // ───────────────────────── Public State ────────────────────────────
        /// <summary>True when the camera position is below the water surface level.</summary>
        public bool IsUnderwater { get; private set; }

        /// <summary>Current yaw angle (degrees).</summary>
        public float Yaw { get; private set; }

        /// <summary>Current pitch angle (degrees).</summary>
        public float Pitch { get; private set; }

        // ───────────────────────── Private ─────────────────────────────────
        private UnityEngine.Camera cam;
        private float currentDistance;
        private float targetDistance;
        private float distanceVelocity;
        private float collisionDistance;
        private float collisionVelocity;
        private Vector3 followVelocity;
        private Vector3 currentPivot;
        private float fovVelocity;

        // Shake
        private float shakeIntensity;
        private float shakeDuration;
        private float shakeTimer;
        private float shakeFrequency = 25f;

        // ───────────────────────── Lifecycle ───────────────────────────────

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            if (cam == null)
                cam = GetComponentInChildren<UnityEngine.Camera>();

            targetDistance = defaultDistance;
            currentDistance = defaultDistance;
            collisionDistance = defaultDistance;
        }

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                currentPivot = target.position + targetOffset;
            }

            // Initialize angles from current rotation
            Vector3 angles = transform.eulerAngles;
            Yaw = angles.y;
            Pitch = angles.x;
            if (Pitch > 180f) Pitch -= 360f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            UpdatePivot();
            UpdateDistance();
            UpdatePosition();
            UpdateFOV();
            UpdateUnderwaterState();
            ApplyShake();
        }

        // ───────────────────────── Input ───────────────────────────────────

        private void HandleInput()
        {
            // Mouse input - rotate player body horizontally, only camera rotates vertically
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

            // Rotate player body on Y axis (yaw)
            if (target != null && Mathf.Abs(mouseX) > 0.001f)
            {
                target.Rotate(0f, mouseX, 0f, Space.World);
            }

            // Camera yaw follows player rotation
            if (target != null)
                Yaw = target.eulerAngles.y;

            // Camera pitch (vertical look) is independent
            Pitch -= mouseY;
            Pitch = Mathf.Clamp(Pitch, minPitch, maxPitch);

            // Scroll zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetDistance -= scroll * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }

        // ───────────────────────── Pivot (Follow) ──────────────────────────

        private void UpdatePivot()
        {
            Vector3 targetPivot = target.position + targetOffset;
            currentPivot = Vector3.SmoothDamp(currentPivot, targetPivot, ref followVelocity, followSmoothTime);
        }

        // ───────────────────────── Distance & Collision ────────────────────

        private void UpdateDistance()
        {
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

            // Collision check
            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 desiredPosition = currentPivot + rotation * (Vector3.back * currentDistance);
            Vector3 direction = desiredPosition - currentPivot;
            float desiredDist = direction.magnitude;

            float adjustedDist = desiredDist;

            if (Physics.SphereCast(currentPivot, collisionRadius, direction.normalized, out RaycastHit hit,
                    desiredDist, collisionMask))
            {
                adjustedDist = Mathf.Max(minDistance, hit.distance - collisionRadius);
            }

            collisionDistance = Mathf.SmoothDamp(collisionDistance, adjustedDist, ref collisionVelocity, collisionSmoothTime);
        }

        // ───────────────────────── Position & Rotation ─────────────────────

        private void UpdatePosition()
        {
            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 position = currentPivot + rotation * (Vector3.back * collisionDistance);

            transform.position = position;
            transform.LookAt(currentPivot);
        }

        // ───────────────────────── FOV ─────────────────────────────────────

        private void UpdateFOV()
        {
            if (cam == null) return;

            bool sprinting = false;
            if (target != null)
            {
                var controller = target.GetComponent<Player.PlayerController>();
                if (controller != null)
                    sprinting = controller.IsSprinting;
            }

            float targetFov = sprinting ? sprintFOV : defaultFOV;
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVelocity, fovSmoothTime);
        }

        // ───────────────────────── Underwater ──────────────────────────────

        private void UpdateUnderwaterState()
        {
            float waterLevel = float.MinValue;

            // Access FloodManager for water level
            var flood = Disaster.FloodManager.Instance;
            if (flood != null)
                waterLevel = flood.WaterLevel;

            IsUnderwater = transform.position.y < waterLevel;

            // Underwater tint can be read by a post-processing script or applied via OnRenderImage.
            // We expose IsUnderwater and UnderwaterTintColor for external systems.
        }

        /// <summary>
        /// Returns the configured underwater tint color for external rendering systems.
        /// </summary>
        public Color GetUnderwaterTintColor()
        {
            return IsUnderwater ? underwaterTintColor : Color.clear;
        }

        // ───────────────────────── Camera Shake ────────────────────────────

        /// <summary>
        /// Triggers a camera shake effect.
        /// </summary>
        /// <param name="intensity">How much the camera shakes (units).</param>
        /// <param name="duration">How long the shake lasts (seconds).</param>
        /// <param name="frequency">Oscillation frequency (higher = faster shaking).</param>
        public void Shake(float intensity, float duration, float frequency = 25f)
        {
            // Take the stronger shake if one is already playing
            if (shakeTimer > 0f && intensity < shakeIntensity) return;

            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
            shakeFrequency = frequency;
        }

        private void ApplyShake()
        {
            if (shakeTimer <= 0f) return;

            shakeTimer -= Time.deltaTime;
            float decay = Mathf.Clamp01(shakeTimer / shakeDuration);
            float currentIntensity = shakeIntensity * decay;

            float offsetX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f * currentIntensity;
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f * currentIntensity;

            transform.position += transform.right * offsetX + transform.up * offsetY;

            if (shakeTimer <= 0f)
            {
                shakeTimer = 0f;
                shakeIntensity = 0f;
            }
        }
    }
}
