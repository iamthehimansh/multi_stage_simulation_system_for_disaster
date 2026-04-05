using UnityEngine;
using FightForLife.Player;
using System.Collections;

namespace FightForLife.Core
{
    public class CheckpointSystem : MonoBehaviour
    {
        public static CheckpointSystem Instance { get; private set; }

        [Header("Respawn Settings")]
        [SerializeField] private float respawnHealthPercent = 0.5f;
        [SerializeField] private float invincibilityDuration = 3f;
        [SerializeField] private int deathScorePenalty = 200;
        [SerializeField] private float respawnDelay = 2f;

        [Header("Default Spawn")]
        [SerializeField] private Transform defaultSpawnPoint;

        private Transform activeCheckpoint;
        private PlayerHealth playerHealth;
        private bool isRespawning;

        public Transform ActiveCheckpoint => activeCheckpoint;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();

            if (playerHealth != null)
                playerHealth.OnDeath += OnPlayerDeath;

            if (activeCheckpoint == null && defaultSpawnPoint != null)
                activeCheckpoint = defaultSpawnPoint;
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.OnDeath -= OnPlayerDeath;
        }

        public void SetCheckpoint(Transform checkpoint)
        {
            activeCheckpoint = checkpoint;
            Debug.Log($"Checkpoint activated at {checkpoint.position}");
        }

        private void OnPlayerDeath()
        {
            if (isRespawning) return;
            StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            isRespawning = true;

            // Apply score penalty
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(-deathScorePenalty);

            // Wait before respawning
            yield return new WaitForSeconds(respawnDelay);

            // Move player to checkpoint
            Transform spawnPoint = activeCheckpoint != null ? activeCheckpoint : defaultSpawnPoint;

            if (spawnPoint != null && playerHealth != null)
            {
                CharacterController cc = playerHealth.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                playerHealth.transform.position = spawnPoint.position;
                playerHealth.transform.rotation = spawnPoint.rotation;

                if (cc != null) cc.enabled = true;
            }

            // Restore partial health
            if (playerHealth != null)
            {
                float respawnHealth = playerHealth.MaxHealth * respawnHealthPercent;
                playerHealth.Heal(respawnHealth);
            }

            // Grant invincibility
            if (playerHealth != null)
            {
                playerHealth.SetInvincible(true);
                StartCoroutine(RemoveInvincibility());
            }

            isRespawning = false;
        }

        private IEnumerator RemoveInvincibility()
        {
            yield return new WaitForSeconds(invincibilityDuration);

            if (playerHealth != null)
                playerHealth.SetInvincible(false);
        }
    }

    /// <summary>
    /// Place this component on trigger colliders in the scene to mark checkpoint zones.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private bool activateOnEnter = true;

        private bool activated;

        private void OnTriggerEnter(Collider other)
        {
            if (!activateOnEnter) return;
            if (activated) return;
            if (!other.CompareTag("Player")) return;

            activated = true;

            if (CheckpointSystem.Instance != null)
                CheckpointSystem.Instance.SetCheckpoint(transform);
        }

        public void Activate()
        {
            activated = true;
            if (CheckpointSystem.Instance != null)
                CheckpointSystem.Instance.SetCheckpoint(transform);
        }

        public void Reset()
        {
            activated = false;
        }
    }
}
