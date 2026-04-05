using UnityEngine;

namespace FightForLife.Disaster
{
    [RequireComponent(typeof(Collider))]
    public class WaterZone : MonoBehaviour
    {
        [Header("Water Surface")]
        [SerializeField] private float waterSurfaceOffset = 0f;

        [Header("Current")]
        [SerializeField] private Vector3 currentDirection = Vector3.right;
        [SerializeField] private float currentStrength = 1f;
        [SerializeField] private bool useFloodManagerCurrent = true;

        [Header("Force Application")]
        [SerializeField] private float forceMultiplier = 1f;
        [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

        [Header("Buoyancy")]
        [SerializeField] private float waterDrag = 3f;
        [SerializeField] private float waterAngularDrag = 2f;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null) return;

            Vector3 current = GetCurrentAtPosition(other.transform.position);
            if (current.sqrMagnitude > 0.01f)
            {
                rb.AddForce(current * forceMultiplier, forceMode);
            }

            // Apply water drag
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null) return;

            // Restore default drag values
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
        }

        /// <summary>
        /// Returns the water depth at a given world position.
        /// Positive values mean the position is below the water surface.
        /// </summary>
        public float GetWaterDepthAtPosition(Vector3 worldPosition)
        {
            float waterLevel = GetWaterLevel();
            float depth = waterLevel - worldPosition.y;
            return Mathf.Max(0f, depth);
        }

        /// <summary>
        /// Returns the current force vector at a given world position.
        /// </summary>
        public Vector3 GetCurrentAtPosition(Vector3 worldPosition)
        {
            if (useFloodManagerCurrent && FloodManager.Instance != null)
            {
                Vector2 flowDir = FloodManager.Instance.GetFlowDirection();
                float strength = FloodManager.Instance.GetCurrentStrength();
                return new Vector3(flowDir.x, 0f, flowDir.y) * strength;
            }

            return currentDirection.normalized * currentStrength;
        }

        /// <summary>
        /// Returns true if the given world position is below the water surface.
        /// </summary>
        public bool IsUnderwater(Vector3 worldPosition)
        {
            return worldPosition.y < GetWaterLevel();
        }

        private float GetWaterLevel()
        {
            if (FloodManager.Instance != null)
            {
                return FloodManager.Instance.WaterLevel + waterSurfaceOffset;
            }

            return transform.position.y + waterSurfaceOffset;
        }
    }
}
