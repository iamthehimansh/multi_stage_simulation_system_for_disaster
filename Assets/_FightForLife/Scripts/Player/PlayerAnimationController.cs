using UnityEngine;

namespace FightForLife.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Smoothing")]
        [SerializeField] private float speedDampTime = 0.1f;
        [SerializeField] private float waterDepthDampTime = 0.2f;

        // ───────────────────────── Animator Parameter Hashes ───────────────
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashIsSwimming = Animator.StringToHash("IsSwimming");
        private static readonly int HashIsDiving = Animator.StringToHash("IsDiving");
        private static readonly int HashIsWading = Animator.StringToHash("IsWading");
        private static readonly int HashIsSprinting = Animator.StringToHash("IsSprinting");
        private static readonly int HashIsCrouching = Animator.StringToHash("IsCrouching");
        private static readonly int HashWaterDepth = Animator.StringToHash("WaterDepth");
        private static readonly int HashIsCarrying = Animator.StringToHash("IsCarrying");
        private static readonly int HashInteractTrigger = Animator.StringToHash("InteractTrigger");
        private static readonly int HashDamageTrigger = Animator.StringToHash("DamageTrigger");
        private static readonly int HashRescueTrigger = Animator.StringToHash("RescueTrigger");

        // ───────────────────────── Private ─────────────────────────────────
        private PlayerController playerController;
        private PlayerInventory playerInventory;

        // ───────────────────────── Lifecycle ───────────────────────────────

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerInventory = GetComponent<PlayerInventory>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null || playerController == null) return;

            UpdateParameters();
        }

        // ───────────────────────── Parameter Sync ──────────────────────────

        private void UpdateParameters()
        {
            // Movement speed (normalized 0-1 based on walk speed for blend trees)
            animator.SetFloat(HashSpeed, playerController.CurrentSpeed, speedDampTime, Time.deltaTime);

            // State booleans
            animator.SetBool(HashIsGrounded, playerController.IsGrounded);
            animator.SetBool(HashIsSwimming, playerController.IsSwimming);
            animator.SetBool(HashIsDiving, playerController.IsDiving);
            animator.SetBool(HashIsWading, playerController.IsWading);
            animator.SetBool(HashIsSprinting, playerController.IsSprinting);
            animator.SetBool(HashIsCrouching, playerController.IsCrouching);

            // Water depth
            animator.SetFloat(HashWaterDepth, playerController.WaterDepth, waterDepthDampTime, Time.deltaTime);

            // Carrying
            bool isCarrying = playerInventory != null && playerInventory.GetActiveItem() != null;
            animator.SetBool(HashIsCarrying, isCarrying);
        }

        // ───────────────────────── Public Trigger Methods ──────────────────

        /// <summary>
        /// Triggers the interact animation (e.g. opening doors, pulling levers).
        /// </summary>
        public void PlayInteract()
        {
            if (animator == null) return;
            animator.SetTrigger(HashInteractTrigger);
        }

        /// <summary>
        /// Triggers the damage/hit reaction animation.
        /// </summary>
        public void PlayDamage()
        {
            if (animator == null) return;
            animator.SetTrigger(HashDamageTrigger);
        }

        /// <summary>
        /// Triggers the rescue animation (e.g. pulling NPC to safety).
        /// </summary>
        public void PlayRescue()
        {
            if (animator == null) return;
            animator.SetTrigger(HashRescueTrigger);
        }
    }
}
