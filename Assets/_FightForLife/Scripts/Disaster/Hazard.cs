using UnityEngine;

namespace FightForLife.Disaster
{
    public enum HazardType
    {
        ElectrifiedWater,
        Debris,
        StrongCurrent,
        Fire,
        GasLeak,
        Whirlpool
    }

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(AudioSource))]
    public class Hazard : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private HazardType hazardType = HazardType.Debris;
        [SerializeField] private float damagePerSecond = 10f;
        [SerializeField] private bool isActive = true;

        [Header("Visual Warning")]
        [SerializeField] private ParticleSystem warningEffect;
        [SerializeField] private Color hazardColor = Color.red;

        [Header("Audio Warning")]
        [SerializeField] private AudioClip hazardLoopClip;
        [SerializeField] private float audioVolume = 0.7f;

        [Header("Whirlpool (if applicable)")]
        [SerializeField] private float pullForce = 5f;
        [SerializeField] private float pullRadius = 8f;

        [Header("Strong Current (if applicable)")]
        [SerializeField] private Vector3 currentDirection = Vector3.right;
        [SerializeField] private float currentForce = 10f;

        public bool IsActive => isActive;
        public HazardType Type => hazardType;

        private AudioSource audioSource;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 25f;
            audioSource.loop = true;
            audioSource.volume = audioVolume;

            SetupVisuals();
        }

        private void Start()
        {
            if (isActive && hazardLoopClip != null)
            {
                audioSource.clip = hazardLoopClip;
                audioSource.Play();
            }
        }

        private void SetupVisuals()
        {
            if (warningEffect != null)
            {
                var main = warningEffect.main;
                main.startColor = hazardColor;

                if (isActive)
                    warningEffect.Play();
                else
                    warningEffect.Stop();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!isActive) return;

            // Damage player
            var playerHealth = other.GetComponent<Player.PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damagePerSecond * Time.deltaTime);

                // Apply hazard-specific effects
                ApplyHazardEffect(other);
            }

            // Damage NPCs (set injured if struggling/panicking)
            var civilian = other.GetComponent<NPC.CivilianAI>();
            if (civilian != null && civilian.CurrentState != NPC.NPCState.Dead &&
                civilian.CurrentState != NPC.NPCState.Rescued)
            {
                // NPCs get injured by hazards
                if (civilian.CurrentState != NPC.NPCState.Injured &&
                    civilian.CurrentState != NPC.NPCState.Trapped)
                {
                    civilian.SetInjured();
                }
            }
        }

        private void ApplyHazardEffect(Collider target)
        {
            Rigidbody rb = target.attachedRigidbody;
            if (rb == null) return;

            switch (hazardType)
            {
                case HazardType.Whirlpool:
                    // Pull toward center
                    Vector3 toCenter = transform.position - target.transform.position;
                    toCenter.y = 0f;
                    float dist = toCenter.magnitude;
                    if (dist > 0.1f && dist < pullRadius)
                    {
                        float forceMagnitude = pullForce * (1f - dist / pullRadius);
                        rb.AddForce(toCenter.normalized * forceMagnitude, ForceMode.Acceleration);
                        // Downward spiral
                        rb.AddForce(Vector3.down * forceMagnitude * 0.5f, ForceMode.Acceleration);
                    }
                    break;

                case HazardType.StrongCurrent:
                    rb.AddForce(currentDirection.normalized * currentForce, ForceMode.Acceleration);
                    break;

                case HazardType.Fire:
                    // Push away from center slightly
                    Vector3 awayFromFire = target.transform.position - transform.position;
                    awayFromFire.y = 0f;
                    if (awayFromFire.sqrMagnitude > 0.01f)
                    {
                        rb.AddForce(awayFromFire.normalized * 2f, ForceMode.Acceleration);
                    }
                    break;
            }
        }

        /// <summary>
        /// Disables the hazard (e.g., shutting off power disables electrified water).
        /// </summary>
        public void Disable()
        {
            isActive = false;

            if (warningEffect != null)
                warningEffect.Stop();

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

            Debug.Log($"[Hazard] {gameObject.name} ({hazardType}) has been disabled.");
        }

        /// <summary>
        /// Re-enables the hazard.
        /// </summary>
        public void Enable()
        {
            isActive = true;

            if (warningEffect != null)
                warningEffect.Play();

            if (audioSource != null && hazardLoopClip != null)
            {
                audioSource.clip = hazardLoopClip;
                audioSource.Play();
            }
        }
    }
}
