using UnityEngine;

namespace FightForLife.Disaster
{
    public enum BuildingType
    {
        Brick,
        Mud,
        Concrete
    }

    [RequireComponent(typeof(AudioSource))]
    public class StructuralDamage : MonoBehaviour
    {
        [Header("Building")]
        [SerializeField] private BuildingType buildingType = BuildingType.Brick;
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float currentHP = 100f;

        [Header("Damage")]
        [SerializeField] private float baseDamageRate = 1f;
        [SerializeField] private float waterDepthDamageMultiplier = 2f;
        [SerializeField] private float timeDamageAcceleration = 0.1f;

        [Header("Collapse")]
        [SerializeField] private ParticleSystem collapseEffect;
        [SerializeField] private GameObject rubbleCollider;
        [SerializeField] private float collapseDamageRadius = 5f;
        [SerializeField] private float collapseDamage = 50f;

        [Header("Visual Warning")]
        [SerializeField] private Renderer buildingRenderer;
        [SerializeField] private Color healthyColor = Color.white;
        [SerializeField] private Color damagedColor = new Color(0.4f, 0.2f, 0.1f);
        [SerializeField] private string colorPropertyName = "_BaseColor";

        [Header("Audio")]
        [SerializeField] private AudioClip creakingClip;
        [SerializeField] private AudioClip collapseClip;
        [SerializeField] private float creakInterval = 8f;

        public float DamagePercent => 1f - (currentHP / maxHP);
        public bool IsCollapsed { get; private set; }
        public BuildingType Type => buildingType;

        private AudioSource audioSource;
        private float timeInWater;
        private float lastCreakTime;
        private MaterialPropertyBlock propertyBlock;
        private bool hasWarnedCritical;

        private float DurabilityMultiplier => buildingType switch
        {
            BuildingType.Concrete => 0.3f,  // Takes less damage
            BuildingType.Brick => 1f,
            BuildingType.Mud => 3f,          // Takes much more damage
            _ => 1f
        };

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 40f;

            propertyBlock = new MaterialPropertyBlock();
            currentHP = maxHP;

            if (rubbleCollider != null)
                rubbleCollider.SetActive(false);
        }

        private void Update()
        {
            if (IsCollapsed) return;

            float waterDepth = GetWaterDepthAtBase();

            if (waterDepth > 0.1f)
            {
                timeInWater += Time.deltaTime;
                ApplyWaterDamage(waterDepth);
            }

            UpdateVisuals();
            UpdateCreaking();

            if (currentHP <= 0f)
            {
                Collapse();
            }
        }

        private float GetWaterDepthAtBase()
        {
            if (FloodManager.Instance == null) return 0f;

            float waterLevel = FloodManager.Instance.WaterLevel;
            float baseY = transform.position.y;
            float depth = waterLevel - baseY;
            return Mathf.Max(0f, depth);
        }

        private void ApplyWaterDamage(float waterDepth)
        {
            float depthFactor = waterDepth * waterDepthDamageMultiplier;
            float timeFactor = 1f + (timeInWater * timeDamageAcceleration);
            float damage = baseDamageRate * depthFactor * timeFactor * DurabilityMultiplier * Time.deltaTime;

            currentHP = Mathf.Max(0f, currentHP - damage);
        }

        private void UpdateVisuals()
        {
            if (buildingRenderer == null) return;

            float damageRatio = DamagePercent;
            Color currentColor = Color.Lerp(healthyColor, damagedColor, damageRatio);

            buildingRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyName, currentColor);
            buildingRenderer.SetPropertyBlock(propertyBlock);
        }

        private void UpdateCreaking()
        {
            if (currentHP > maxHP * 0.5f) return;

            if (Time.time - lastCreakTime >= creakInterval)
            {
                lastCreakTime = Time.time;

                if (creakingClip != null)
                {
                    float volume = Mathf.Lerp(0.3f, 1f, DamagePercent);
                    audioSource.PlayOneShot(creakingClip, volume);
                }

                if (!hasWarnedCritical && currentHP < maxHP * 0.25f)
                {
                    hasWarnedCritical = true;
                    Debug.Log($"[Structure] {gameObject.name} is critically damaged! ({currentHP:F0}/{maxHP:F0} HP)");
                }
            }
        }

        private void Collapse()
        {
            if (IsCollapsed) return;
            IsCollapsed = true;

            Debug.Log($"[Structure] {gameObject.name} has collapsed!");

            // Play collapse effect
            if (collapseEffect != null)
            {
                collapseEffect.transform.position = transform.position;
                collapseEffect.Play();
            }

            // Play collapse audio
            if (collapseClip != null)
            {
                audioSource.PlayOneShot(collapseClip);
            }

            // Disable building renderer
            if (buildingRenderer != null)
            {
                buildingRenderer.enabled = false;
            }

            // Enable rubble collider
            if (rubbleCollider != null)
            {
                rubbleCollider.SetActive(true);
            }

            // Damage nearby entities
            DealCollapseDamage();
        }

        private void DealCollapseDamage()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, collapseDamageRadius);

            foreach (Collider hit in hits)
            {
                // Damage player
                var playerHealth = hit.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(collapseDamage);
                    continue;
                }

                // Damage/trap NPCs
                var civilian = hit.GetComponent<NPC.CivilianAI>();
                if (civilian != null)
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < collapseDamageRadius * 0.5f)
                    {
                        civilian.SetTrapped();
                    }
                    else
                    {
                        civilian.SetInjured();
                    }
                }
            }
        }
    }
}
