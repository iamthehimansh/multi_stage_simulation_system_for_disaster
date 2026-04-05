using UnityEngine;

namespace FightForLife.NPC
{
    [RequireComponent(typeof(Collider))]
    public class RescueZone : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Color glowColor = new Color(0f, 1f, 0.3f, 0.3f);
        [SerializeField] private float glowIntensity = 1.5f;

        [Header("Stats")]
        [SerializeField] private int npcsRescuedHere;

        public int NPCsRescuedHere => npcsRescuedHere;

        private void Awake()
        {
            // Ensure collider is a trigger
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            SetupVisual();
        }

        private void SetupVisual()
        {
            // Apply green glow to the zone's renderer if present
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = rend.material;
                mat.color = glowColor;
                mat.SetColor("_EmissionColor", glowColor * glowIntensity);
                mat.EnableKeyword("_EMISSION");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CivilianAI civilian = other.GetComponent<CivilianAI>();
            if (civilian == null)
                civilian = other.GetComponentInParent<CivilianAI>();

            if (civilian == null) return;

            if (civilian.CurrentState == NPCState.Following)
            {
                civilian.MarkRescued();
                npcsRescuedHere++;

                Debug.Log($"[RescueZone] {civilian.gameObject.name} rescued at {gameObject.name}. " +
                          $"Total at this zone: {npcsRescuedHere}");
            }
        }
    }
}
