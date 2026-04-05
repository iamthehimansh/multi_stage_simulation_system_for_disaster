using UnityEngine;
using FightForLife.Player;
using FightForLife.Disaster;

namespace FightForLife.Items
{
    /// <summary>
    /// Enhanced world item pickup with floating animation, glow effect, and water buoyancy.
    /// Works alongside the simple ItemPickup in InteractionSystem for backward compatibility.
    /// The InteractionSystem's pickup detection will find and use the Player.ItemPickup component.
    /// This script adds visual polish and buoyancy on top.
    /// </summary>
    [RequireComponent(typeof(Player.ItemPickup))]
    public class ItemPickupEnhanced : MonoBehaviour, IInteractable
    {
        [Header("Item")]
        [SerializeField] private ItemData itemData;

        [Header("Floating Animation")]
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 1.5f;
        [SerializeField] private float rotationSpeed = 45f;

        [Header("Glow Effect")]
        [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.5f, 1f);
        [SerializeField] private float glowIntensity = 2f;
        [SerializeField] private float glowPulseSpeed = 2f;

        [Header("Buoyancy")]
        [SerializeField] private bool canFloatOnWater = true;
        [SerializeField] private float buoyancyForce = 8f;
        [SerializeField] private float waterDrag = 4f;

        [Header("Interaction")]
        [SerializeField] private string pickupPrompt = "Pick Up";
        [SerializeField] private float holdDuration = 0f;

        public ItemData ItemData => itemData;
        public float HoldDuration => holdDuration;

        private Vector3 startLocalPosition;
        private Renderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Rigidbody rb;
        private float bobPhase;
        private bool isInWater;

        private void Awake()
        {
            startLocalPosition = transform.localPosition;
            meshRenderer = GetComponentInChildren<Renderer>();
            rb = GetComponent<Rigidbody>();
            propertyBlock = new MaterialPropertyBlock();

            // Random starting phase so items don't all bob in sync
            bobPhase = Random.Range(0f, Mathf.PI * 2f);

            SetupGlow();
        }

        private void Update()
        {
            UpdateBobAnimation();
            UpdateGlowPulse();
            UpdateBuoyancy();
        }

        private void SetupGlow()
        {
            if (meshRenderer == null) return;

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", glowColor * glowIntensity);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void UpdateBobAnimation()
        {
            // Only bob if not affected by physics (no rigidbody or kinematic)
            if (rb != null && !rb.isKinematic && isInWater) return;

            float bobOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f + bobPhase) * bobAmplitude;
            transform.localPosition = startLocalPosition + Vector3.up * bobOffset;

            // Slow rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void UpdateGlowPulse()
        {
            if (meshRenderer == null) return;

            float pulse = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", glowColor * glowIntensity * pulse);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void UpdateBuoyancy()
        {
            if (!canFloatOnWater || rb == null || rb.isKinematic) return;
            if (FloodManager.Instance == null) return;

            float waterLevel = FloodManager.Instance.WaterLevel;
            float submersion = waterLevel - transform.position.y;

            if (submersion > 0f)
            {
                isInWater = true;
                float forceFraction = Mathf.Clamp01(submersion / 0.5f);
                rb.AddForce(Vector3.up * buoyancyForce * forceFraction, ForceMode.Acceleration);
                rb.linearDamping = waterDrag;

                // Apply current
                Vector2 flowDir = FloodManager.Instance.GetFlowDirection();
                float strength = FloodManager.Instance.GetCurrentStrength();
                Vector3 current = new Vector3(flowDir.x, 0f, flowDir.y) * strength * 0.3f;
                rb.AddForce(current, ForceMode.Acceleration);
            }
            else
            {
                isInWater = false;
                rb.linearDamping = 0.1f;
            }
        }

        // --- IInteractable ---

        public string GetPrompt()
        {
            string name = itemData != null ? itemData.DisplayName : "Item";
            return $"{pickupPrompt} {name}";
        }

        public bool CanInteract()
        {
            return itemData != null;
        }

        public void Interact(GameObject player)
        {
            if (itemData == null) return;

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return;

            if (inventory.TryAddItem(itemData))
            {
                Debug.Log($"[ItemPickup] {player.name} picked up {itemData.DisplayName}");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[ItemPickup] Inventory full, cannot pick up {itemData.DisplayName}");
            }
        }
    }
}
