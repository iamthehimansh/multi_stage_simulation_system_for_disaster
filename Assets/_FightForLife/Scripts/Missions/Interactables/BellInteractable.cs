using UnityEngine;
using FightForLife.Core;
using FightForLife.Player;

namespace FightForLife.Missions
{
    public class BellInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private AudioClip bellSound;
        [SerializeField] private float holdTime = 2f;
        [SerializeField] private string missionId = "VM01";
        [SerializeField] private string objectiveId = "ring_bell";

        private AudioSource audioSource;
        private bool hasBeenRung;

        public float HoldDuration => holdTime;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 200f;
        }

        public string GetPrompt()
        {
            return hasBeenRung ? "" : "Hold E - Ring Bell";
        }

        public bool CanInteract()
        {
            return !hasBeenRung;
        }

        public void Interact(GameObject interactor)
        {
            if (hasBeenRung) return;
            hasBeenRung = true;

            if (bellSound != null)
                audioSource.PlayOneShot(bellSound);

            // Update mission objective
            if (MissionManager.Instance != null)
                MissionManager.Instance.UpdateObjective(missionId, objectiveId);

            Debug.Log("[Bell] Temple bell has been rung!");
        }
    }
}
