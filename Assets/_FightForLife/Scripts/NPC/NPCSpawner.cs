using UnityEngine;
using System.Collections.Generic;
using FightForLife.Core;

namespace FightForLife.NPC
{
    [DefaultExecutionOrder(-10)]
    public class NPCSpawner : MonoBehaviour
    {
        public static NPCSpawner Instance { get; private set; }

        [Header("Spawning")]
        [SerializeField] private GameObject npcPrefab;
        [SerializeField] private GameObject[] npcPrefabVariants;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int totalNPCs = 20;

        [Header("Type Distribution (ratios)")]
        [SerializeField] private float adultRatio = 0.5f;
        [SerializeField] private float childRatio = 0.2f;
        [SerializeField] private float elderlyRatio = 0.2f;
        [SerializeField] private float disabledRatio = 0.1f;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadius = 2f;

        public event System.Action<CivilianAI> OnNPCRescued;
        public event System.Action<CivilianAI> OnNPCDied;

        private List<CivilianAI> allNPCs = new List<CivilianAI>();
        private int rescuedCount;
        private int deadCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Self-heal: in some scene-reload paths the static backing field
            // ends up null even though this spawner is alive and active.
            if (Instance == null) Instance = this;
        }

        public static void SetInstance(NPCSpawner sp) { Instance = sp; }

        private void Start()
        {
            SpawnAllNPCs();
        }

        private void SpawnAllNPCs()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[NPCSpawner] No spawn points assigned, using spawner position.");
                spawnPoints = new Transform[] { transform };
            }

            // Build type list based on ratios
            List<NPCType> typeList = BuildTypeList();

            for (int i = 0; i < totalNPCs; i++)
            {
                Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
                Vector3 offset = Random.insideUnitSphere * spawnRadius;
                offset.y = 0f;
                Vector3 spawnPos = spawnPoint.position + offset;

                GameObject npcObj;
                // Pick random prefab from variants, fallback to single prefab
                GameObject chosenPrefab = null;
                if (npcPrefabVariants != null && npcPrefabVariants.Length > 0)
                {
                    chosenPrefab = npcPrefabVariants[Random.Range(0, npcPrefabVariants.Length)];
                }
                if (chosenPrefab == null) chosenPrefab = npcPrefab;

                if (chosenPrefab != null)
                {
                    npcObj = Instantiate(chosenPrefab, spawnPos, Quaternion.identity, transform);
                }
                else
                {
                    npcObj = CreateNPCFromPrimitives(spawnPos, typeList[i]);
                    npcObj.transform.SetParent(transform);
                }
                npcObj.name = $"Civilian_{i:D2}_{typeList[i]}";

                CivilianAI civilian = npcObj.GetComponent<CivilianAI>();
                if (civilian == null)
                {
                    civilian = npcObj.AddComponent<CivilianAI>();
                }

                civilian.OnStateChanged += HandleNPCStateChanged;
                allNPCs.Add(civilian);
            }

            // Update ScoreManager total count
            if (ScoreManager.Instance != null)
            {
                // ScoreManager tracks totalCivilians via its serialized field
            }

            Debug.Log($"[NPCSpawner] Spawned {totalNPCs} NPCs across {spawnPoints.Length} spawn points.");
        }

        private List<NPCType> BuildTypeList()
        {
            List<NPCType> types = new List<NPCType>();
            float totalRatio = adultRatio + childRatio + elderlyRatio + disabledRatio;

            int adults = Mathf.RoundToInt((adultRatio / totalRatio) * totalNPCs);
            int children = Mathf.RoundToInt((childRatio / totalRatio) * totalNPCs);
            int elderly = Mathf.RoundToInt((elderlyRatio / totalRatio) * totalNPCs);
            int disabled = totalNPCs - adults - children - elderly;

            for (int i = 0; i < adults; i++) types.Add(NPCType.Adult);
            for (int i = 0; i < children; i++) types.Add(NPCType.Child);
            for (int i = 0; i < elderly; i++) types.Add(NPCType.Elderly);
            for (int i = 0; i < disabled; i++) types.Add(NPCType.Disabled);

            // Ensure we have exactly totalNPCs
            while (types.Count < totalNPCs) types.Add(NPCType.Adult);
            while (types.Count > totalNPCs) types.RemoveAt(types.Count - 1);

            // Shuffle
            for (int i = types.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (types[i], types[j]) = (types[j], types[i]);
            }

            return types;
        }

        private void HandleNPCStateChanged(CivilianAI npc, NPCState newState)
        {
            switch (newState)
            {
                case NPCState.Rescued:
                    // Skip if already counted via direct interact (Calm/Rescue/Free)
                    if (npc.IsRescueCounted) break;
                    npc.MarkRescueCounted();
                    NotifyRescue(npc);
                    break;

                case NPCState.Dead:
                    deadCount++;
                    OnNPCDied?.Invoke(npc);
                    break;
            }
        }

        private GameObject CreateNPCFromPrimitives(Vector3 pos, NPCType type)
        {
            var npc = new GameObject("NPC");
            npc.transform.position = pos;

            // Scale based on type
            float scale = type switch
            {
                NPCType.Child => 0.6f,
                NPCType.Elderly => 0.9f,
                _ => 1f
            };

            // Body colors by type
            Color bodyColor = type switch
            {
                NPCType.Child => new Color(0.9f, 0.6f, 0.2f),
                NPCType.Elderly => new Color(0.5f, 0.5f, 0.6f),
                NPCType.Disabled => new Color(0.6f, 0.3f, 0.5f),
                _ => new Color(0.3f + Random.Range(0, 0.4f), 0.3f + Random.Range(0, 0.3f), 0.3f + Random.Range(0, 0.3f))
            };

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(npc.transform);
            body.transform.localPosition = new Vector3(0, 1 * scale, 0);
            body.transform.localScale = new Vector3(0.5f * scale, 0.8f * scale, 0.5f * scale);
            var bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyRenderer.material.color = bodyColor;
            Object.Destroy(body.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(npc.transform);
            head.transform.localPosition = new Vector3(0, 1.9f * scale, 0);
            head.transform.localScale = Vector3.one * 0.35f * scale;
            var headRenderer = head.GetComponent<Renderer>();
            headRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            headRenderer.material.color = new Color(0.85f, 0.7f, 0.55f);
            Object.Destroy(head.GetComponent<Collider>());

            // NavMesh agent needs a collider
            var col = npc.AddComponent<CapsuleCollider>();
            col.height = 2f * scale;
            col.radius = 0.3f * scale;
            col.center = new Vector3(0, 1f * scale, 0);

            // Add NavMeshAgent
            var agent = npc.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.height = 2f * scale;
            agent.radius = 0.3f * scale;
            agent.speed = 3.5f;

            // Add AudioSource (required by CivilianAI)
            npc.AddComponent<AudioSource>();

            return npc;
        }

        // --- Public API ---

        public List<CivilianAI> GetAllNPCs()
        {
            return new List<CivilianAI>(allNPCs);
        }

        public int GetRescuedCount()
        {
            return rescuedCount;
        }

        public int GetTotalCount()
        {
            return allNPCs.Count;
        }

        public int GetAliveCount()
        {
            return allNPCs.Count - deadCount;
        }

        /// <summary>
        /// Called by CivilianAI the first time the player calms/rescues/frees an NPC.
        /// Counts the rescue immediately, ticks score and mission objectives,
        /// without forcing the NPC into the frozen Rescued state (so they keep following).
        /// </summary>
        public void NotifyRescue(CivilianAI npc)
        {
            rescuedCount++;
            OnNPCRescued?.Invoke(npc);

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RescueCivilian();

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.UpdateObjective("VM02", "rescue_families");
                MissionManager.Instance.UpdateObjective("VM03", "guide_across");
            }
        }

        public int GetDeadCount()
        {
            return deadCount;
        }
    }
}
