using UnityEngine;

namespace FightForLife.Player
{
    /// <summary>
    /// Interface for any object the player can interact with.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Prompt text shown to the player (e.g. "Press E to open door").</summary>
        string GetPrompt();

        /// <summary>Execute the interaction.</summary>
        void Interact(GameObject player);

        /// <summary>Whether the interaction is currently available.</summary>
        bool CanInteract();

        /// <summary>
        /// How long the player must hold the interact key.
        /// Return 0 for instant interactions.
        /// </summary>
        float HoldDuration { get; }
    }

    public class InteractionSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private float pickupRange = 2.5f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private float sphereCastRadius = 0.15f;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        // ───────────────────────── Public State ────────────────────────────
        /// <summary>Current prompt text for UI to display. Empty when nothing is targeted.</summary>
        public string CurrentPrompt { get; private set; } = string.Empty;

        /// <summary>Hold progress from 0 to 1. Only relevant when HoldDuration > 0.</summary>
        public float HoldProgress { get; private set; }

        /// <summary>Whether the player is currently holding the interact key.</summary>
        public bool IsHolding => isHolding;

        /// <summary>The currently targeted interactable, or null.</summary>
        public IInteractable CurrentTarget { get; private set; }

        /// <summary>The GameObject of the current target, or null.</summary>
        public GameObject CurrentTargetObject { get; private set; }

        // ───────────────────────── Events ──────────────────────────────────
        public event System.Action<IInteractable> OnInteractionComplete;
        public event System.Action<GameObject> OnItemPickedUp;
        public event System.Action<GameObject> OnItemDropped;

        // ───────────────────────── Private ─────────────────────────────────
        private float holdTimer;
        private bool isHolding;
        private PlayerInventory inventory;

        // ───────────────────────── Lifecycle ───────────────────────────────

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
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
            DetectInteractable();
            HandleInteractInput();
            HandlePickupInput();
        }

        // ───────────────────────── Detection ───────────────────────────────

        private void DetectInteractable()
        {
            CurrentTarget = null;
            CurrentTargetObject = null;
            CurrentPrompt = string.Empty;

            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

            if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, interactionRange, interactableMask))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract())
                {
                    CurrentTarget = interactable;
                    CurrentTargetObject = hit.collider.gameObject;
                    CurrentPrompt = interactable.GetPrompt();
                }
            }

            // Reset hold if target changed or lost
            if (CurrentTarget == null && isHolding)
            {
                CancelHold();
            }
        }

        // ───────────────────────── Interact (E) ────────────────────────────

        private void HandleInteractInput()
        {
            if (CurrentTarget == null)
            {
                if (isHolding) CancelHold();
                return;
            }

            float holdDuration = CurrentTarget.HoldDuration;

            if (holdDuration <= 0f)
            {
                // Instant interaction
                if (Input.GetKeyDown(KeyCode.E))
                {
                    PerformInteraction();
                }
            }
            else
            {
                // Hold interaction
                if (Input.GetKey(KeyCode.E))
                {
                    if (!isHolding)
                    {
                        isHolding = true;
                        holdTimer = 0f;
                    }

                    holdTimer += Time.deltaTime;
                    HoldProgress = Mathf.Clamp01(holdTimer / holdDuration);

                    if (holdTimer >= holdDuration)
                    {
                        PerformInteraction();
                        CancelHold();
                    }
                }
                else if (isHolding)
                {
                    CancelHold();
                }
            }
        }

        private void PerformInteraction()
        {
            if (CurrentTarget == null) return;

            CurrentTarget.Interact(gameObject);
            OnInteractionComplete?.Invoke(CurrentTarget);
        }

        private void CancelHold()
        {
            isHolding = false;
            holdTimer = 0f;
            HoldProgress = 0f;
        }

        // ───────────────────────── Pickup / Drop (F) ───────────────────────

        private void HandlePickupInput()
        {
            if (!Input.GetKeyDown(KeyCode.F)) return;

            // If we have an inventory and an active item, drop it
            if (inventory != null && inventory.GetActiveItem() != null)
            {
                // Check if nothing is in range to pick up; then drop
                if (CurrentTargetObject == null || CurrentTargetObject.GetComponent<ItemPickup>() == null)
                {
                    inventory.DropActiveItem();
                    return;
                }
            }

            // Try to pick up an item in range
            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, pickupRange, interactableMask))
            {
                ItemPickup pickup = hit.collider.GetComponentInParent<ItemPickup>();
                if (pickup != null && inventory != null)
                {
                    if (inventory.TryAddItem(pickup.ItemData))
                    {
                        OnItemPickedUp?.Invoke(pickup.gameObject);
                        Destroy(pickup.gameObject);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attach to world items that can be picked up. Holds a reference to the item's data.
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemData itemData;
        public ItemData ItemData => itemData;
    }
}
