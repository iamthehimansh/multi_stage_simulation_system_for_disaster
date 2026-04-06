using UnityEngine;

namespace FightForLife.Disaster
{
    [RequireComponent(typeof(Rigidbody))]
    public class FloatingObject : MonoBehaviour
    {
        [Header("Buoyancy")]
        [SerializeField] private float buoyancyForce = 10f;
        [SerializeField] private float waterDrag = 3f;
        [SerializeField] private float waterAngularDrag = 1.5f;
        [SerializeField] private float airDrag = 0.1f;
        [SerializeField] private float airAngularDrag = 0.05f;
        [SerializeField] private float submergedDepthForFullForce = 1f;

        [Header("Bobbing")]
        [SerializeField] private float bobNoiseScale = 0.5f;
        [SerializeField] private float bobNoiseSpeed = 1f;

        [Header("Danger")]
        [SerializeField] private bool isDangerous;
        [SerializeField] private float collisionDamage = 15f;
        [SerializeField] private float minCollisionSpeed = 2f;

        private Rigidbody rb;
        private float noiseOffsetX;
        private float noiseOffsetZ;
        private bool isInWater;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Random noise offsets so objects don't all bob identically
            noiseOffsetX = Random.Range(0f, 100f);
            noiseOffsetZ = Random.Range(0f, 100f);
        }

        private void FixedUpdate()
        {
            if (FloodManager.Instance == null) return;

            float waterLevel = FloodManager.Instance.WaterLevel;
            float objectBottom = transform.position.y;
            float submersion = waterLevel - objectBottom;

            if (submersion > 0f)
            {
                isInWater = true;

                // Buoyancy
                float forceFraction = Mathf.Clamp01(submersion / submergedDepthForFullForce);
                Vector3 buoyancy = Vector3.up * buoyancyForce * forceFraction;

                // Add subtle noise for realistic bobbing
                float time = Time.time * bobNoiseSpeed;
                float noiseX = (Mathf.PerlinNoise(time + noiseOffsetX, 0f) - 0.5f) * 2f * bobNoiseScale;
                float noiseZ = (Mathf.PerlinNoise(0f, time + noiseOffsetZ) - 0.5f) * 2f * bobNoiseScale;
                buoyancy += new Vector3(noiseX, 0f, noiseZ);

                rb.AddForce(buoyancy, ForceMode.Acceleration);

                // Apply water drag
                rb.drag = waterDrag;
                rb.angularDrag = waterAngularDrag;

                // Apply current from FloodManager
                Vector2 flowDir = FloodManager.Instance.GetFlowDirection();
                float strength = FloodManager.Instance.GetCurrentStrength();
                Vector3 currentForce = new Vector3(flowDir.x, 0f, flowDir.y) * strength;
                rb.AddForce(currentForce, ForceMode.Acceleration);

                // Dampen vertical velocity when near surface (reduces oscillation)
                if (Mathf.Abs(submersion) < 0.3f)
                {
                    Vector3 vel = rb.velocity;
                    vel.y *= 0.95f;
                    rb.velocity = vel;
                }
            }
            else
            {
                isInWater = false;
                rb.drag = airDrag;
                rb.angularDrag = airAngularDrag;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isDangerous) return;
            if (!isInWater) return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minCollisionSpeed) return;

            // Damage player
            var playerHealth = collision.gameObject.GetComponent<Player.PlayerHealth>();
            if (playerHealth != null)
            {
                float scaledDamage = collisionDamage * (impactSpeed / minCollisionSpeed);
                playerHealth.TakeDamage(scaledDamage);
                Debug.Log($"[FloatingObject] {gameObject.name} hit player for {scaledDamage:F1} damage (speed: {impactSpeed:F1})");
            }

            // Injure NPCs
            var civilian = collision.gameObject.GetComponent<NPC.CivilianAI>();
            if (civilian != null)
            {
                civilian.SetInjured();
            }
        }
    }
}
