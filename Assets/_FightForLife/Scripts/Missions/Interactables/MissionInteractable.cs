using UnityEngine;
using FightForLife.Core;
using FightForLife.Player;

namespace FightForLife.Missions
{
    /// <summary>
    /// Generic interactable that updates a mission objective on interaction.
    /// Used for: fence gates, radio tower, cellar entrance, supply pickup, etc.
    /// </summary>
    public class MissionInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private string promptText = "Hold E - Interact";
        [SerializeField] private float holdTime = 1.5f;
        [SerializeField] private bool singleUse = true;

        [Header("Mission")]
        [SerializeField] private string missionId;
        [SerializeField] private string objectiveId;

        [Header("Effects")]
        [SerializeField] private AudioClip interactSound;
        [SerializeField] private bool disableOnUse = true;
        [SerializeField] private GameObject[] objectsToDisable;
        [SerializeField] private GameObject[] objectsToEnable;

        private bool used;
        private AudioSource audioSource;

        public float HoldDuration => holdTime;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        public string GetPrompt() => used ? "" : promptText;

        public bool CanInteract() => !used || !singleUse;

        public void Interact(GameObject interactor)
        {
            if (used && singleUse) return;
            used = true;

            if (interactSound != null)
                audioSource.PlayOneShot(interactSound);

            if (MissionManager.Instance != null)
                MissionManager.Instance.UpdateObjective(missionId, objectiveId);

            foreach (var go in objectsToDisable)
                if (go != null) go.SetActive(false);

            foreach (var go in objectsToEnable)
                if (go != null) go.SetActive(true);

            if (disableOnUse)
            {
                var col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                var rend = GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
            }

            Debug.Log($"[MissionInteractable] {missionId}/{objectiveId} used");
        }
    }
}
