using UnityEngine;
using System.Collections.Generic;
using FightForLife.Core;
using FightForLife.Disaster;
using FightForLife.NPC;

namespace FightForLife.Missions
{
    /// <summary>
    /// Orchestrates mission flow across flood phases.
    /// Creates all MissionData, wires phase transitions, and handles mission chaining.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MissionFlowController : MonoBehaviour
    {
        public static MissionFlowController Instance { get; private set; }

        [Header("Mission Waypoints")]
        [SerializeField] private Transform templeBellPosition;
        [SerializeField] private Transform extractionPosition;

        private MissionManager mm;
        private FloodManager fm;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(InitializeWhenReady());
        }

        private System.Collections.IEnumerator InitializeWhenReady()
        {
            while (MissionManager.Instance == null)
                yield return null;
            yield return null; // Extra frame for GameManager.InitializeGame
            yield return null;

            mm = MissionManager.Instance;
            fm = FloodManager.Instance;

            if (mm == null) { Debug.LogError("[MissionFlow] No MissionManager!"); yield break; }

            CreateAllMissions();

            if (fm != null)
                fm.OnPhaseChanged += OnPhaseChanged;

            mm.OnMissionCompleted += OnMissionCompleted;
            mm.OnMissionFailed += OnMissionFailed;

            var vm01 = mm.FindMissionById("VM01");
            if (vm01 != null)
            {
                vm01.status = MissionStatus.Available;
                mm.StartMission(vm01);
            }
        }

        private void OnDestroy()
        {
            if (fm != null) fm.OnPhaseChanged -= OnPhaseChanged;
            if (mm != null)
            {
                mm.OnMissionCompleted -= OnMissionCompleted;
                mm.OnMissionFailed -= OnMissionFailed;
            }
        }

        private void CreateAllMissions()
        {
            var missions = new List<MissionData>();

            // V-M01: The Warning
            var vm01 = new MissionData
            {
                missionId = "VM01",
                missionName = "The Warning",
                description = "The river is rising. Find the temple bell and ring it to warn the village!",
                type = MissionType.Primary,
                status = MissionStatus.Locked,
                rewardPoints = 500,
                timeLimit = 300f, // 5 minutes
                disasterPhase = 0,
                waypointLabel = "Temple Bell"
            };
            vm01.objectives.Add(new ObjectiveData { objectiveId = "reach_temple", description = "Reach the temple", requiredCount = 1 });
            vm01.objectives.Add(new ObjectiveData { objectiveId = "ring_bell", description = "Ring the temple bell", requiredCount = 1 });
            vm01.parallelObjectives = false;
            if (templeBellPosition != null)
                vm01.waypointPosition = templeBellPosition.position;
            missions.Add(vm01);

            // V-M02: Rising Tide
            var vm02 = new MissionData
            {
                missionId = "VM02",
                missionName = "Rising Tide",
                description = "Houses are flooding! Rescue families before it's too late.",
                type = MissionType.Primary,
                status = MissionStatus.Locked,
                rewardPoints = 1300,
                timeLimit = 480f, // 8 minutes
                disasterPhase = 1,
                waypointLabel = "Village"
            };
            vm02.objectives.Add(new ObjectiveData { objectiveId = "rescue_families", description = "Rescue civilians", requiredCount = 5 });
            vm02.parallelObjectives = true;
            missions.Add(vm02);

            // V-M03: Bridge Run
            var vm03 = new MissionData
            {
                missionId = "VM03",
                missionName = "Bridge Run",
                description = "Guide civilians across the bridge before it collapses!",
                type = MissionType.Primary,
                status = MissionStatus.Locked,
                rewardPoints = 1700,
                timeLimit = 360f, // 6 minutes
                disasterPhase = 1,
                waypointLabel = "Bridge"
            };
            vm03.objectives.Add(new ObjectiveData { objectiveId = "guide_across", description = "Guide NPCs across bridge", requiredCount = 4 });
            vm03.parallelObjectives = true;
            missions.Add(vm03);

            // V-M04: The Cellar
            var vm04 = new MissionData
            {
                missionId = "VM04",
                missionName = "The Cellar",
                description = "A child is trapped in a flooded cellar. Dive in and rescue them!",
                type = MissionType.Primary,
                status = MissionStatus.Locked,
                rewardPoints = 500,
                timeLimit = 300f,
                disasterPhase = 1,
                waypointLabel = "Cellar"
            };
            vm04.objectives.Add(new ObjectiveData { objectiveId = "enter_cellar", description = "Find the cellar entrance", requiredCount = 1 });
            vm04.objectives.Add(new ObjectiveData { objectiveId = "rescue_child", description = "Rescue the trapped child", requiredCount = 1 });
            vm04.parallelObjectives = false;
            missions.Add(vm04);

            // V-M05: Escape the Valley
            var vm05 = new MissionData
            {
                missionId = "VM05",
                missionName = "Escape the Valley",
                description = "Reach the hilltop extraction point before the final surge!",
                type = MissionType.Primary,
                status = MissionStatus.Locked,
                rewardPoints = 1000,
                timeLimit = 480f, // 8 minutes
                disasterPhase = 2,
                waypointLabel = "Extraction"
            };
            vm05.objectives.Add(new ObjectiveData { objectiveId = "reach_extraction", description = "Reach the extraction point", requiredCount = 1 });
            vm05.parallelObjectives = true;
            if (extractionPosition != null)
                vm05.waypointPosition = extractionPosition.position;
            missions.Add(vm05);

            // V-S01: Medic Run (Secondary)
            var vs01 = new MissionData
            {
                missionId = "VS01",
                missionName = "Medic Run",
                description = "Retrieve medical supplies from the clinic and bring them to the temple.",
                type = MissionType.Secondary,
                status = MissionStatus.Locked,
                rewardPoints = 300,
                timeLimit = 0f,
                disasterPhase = 1,
                waypointLabel = "Clinic"
            };
            vs01.objectives.Add(new ObjectiveData { objectiveId = "get_supplies", description = "Get medical supplies from clinic", requiredCount = 1 });
            vs01.objectives.Add(new ObjectiveData { objectiveId = "deliver_supplies", description = "Deliver to temple", requiredCount = 1 });
            vs01.parallelObjectives = false;
            missions.Add(vs01);

            // V-S02: Animal Rescue (Secondary)
            var vs02 = new MissionData
            {
                missionId = "VS02",
                missionName = "Animal Rescue",
                description = "Free the livestock from the barn before the flood reaches them.",
                type = MissionType.Secondary,
                status = MissionStatus.Locked,
                rewardPoints = 450,
                timeLimit = 0f,
                disasterPhase = 1,
                waypointLabel = "Barn"
            };
            vs02.objectives.Add(new ObjectiveData { objectiveId = "open_gates", description = "Open barn gates", requiredCount = 3 });
            vs02.parallelObjectives = true;
            missions.Add(vs02);

            // V-S05: Signal Fire (Secondary)
            var vs05 = new MissionData
            {
                missionId = "VS05",
                missionName = "Signal Fire",
                description = "Reach the radio tower on the hill and send an SOS signal.",
                type = MissionType.Secondary,
                status = MissionStatus.Locked,
                rewardPoints = 350,
                timeLimit = 0f,
                disasterPhase = 2,
                waypointLabel = "Radio Tower"
            };
            vs05.objectives.Add(new ObjectiveData { objectiveId = "send_sos", description = "Send SOS from radio tower", requiredCount = 1 });
            vs05.parallelObjectives = true;
            missions.Add(vs05);

            // Register all missions with MissionManager via reflection (allMissions is serialized)
            var field = typeof(MissionManager).GetField("allMissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(mm, missions);

            // Map mission waypoints to actual buildings in the scene (no cylinder poles)
            MapMissionsToSceneBuildings(vm02, vm03, vm04, vm05, vs01, vs02, vs05);

            Debug.Log($"[MissionFlow] Created {missions.Count} missions.");
        }

        // ── Map missions to actual scene buildings ─────────────────────────
        // Finds real buildings/structures by name and uses their positions as waypoints.
        // No cylinder poles created — the minimap and HUD show waypoints natively.

        private void MapMissionsToSceneBuildings(MissionData vm02, MissionData vm03, MissionData vm04,
                                                  MissionData vm05, MissionData vs01, MissionData vs02, MissionData vs05)
        {
            // VM02 "Rising Tide" -> village center (any house cluster)
            vm02.waypointPosition = FindBuildingPosition("BigHouse", "House", "OldHouse");

            // VM03 "Bridge Run" -> actual bridge
            vm03.waypointPosition = FindBuildingPosition("BLD_Bridge", "Bridge");

            // VM04 "The Cellar" -> a house with cellar (OldHouse or similar)
            vm04.waypointPosition = FindBuildingPosition("OldHouse", "Cabin", "House");

            // VM05 "Escape" -> extraction point (already set via extractionPosition)
            if (vm05.waypointPosition == Vector3.zero && extractionPosition != null)
                vm05.waypointPosition = extractionPosition.position;

            // VS01 "Medic Run" -> church as clinic
            vs01.waypointPosition = FindBuildingPosition("Church", "Clinic", "BrickHouse");

            // VS02 "Animal Rescue" -> barn
            vs02.waypointPosition = FindBuildingPosition("Barn1", "Barn2", "Barn");

            // VS05 "Signal Fire" -> radio tower
            vs05.waypointPosition = FindBuildingPosition("Struct_RadioTower", "RadioTower", "Tower");

            Debug.Log("[MissionFlow] Mapped mission waypoints to scene buildings.");
        }

        private Vector3 FindBuildingPosition(params string[] names)
        {
            foreach (var name in names)
            {
                var go = GameObject.Find(name);
                if (go != null) return go.transform.position;
            }
            // Fallback: search partial match
            foreach (var name in names)
            {
                var allObjects = FindObjectsByType<Transform>(FindObjectsSortMode.None);
                foreach (var t in allObjects)
                {
                    if (t.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return t.position;
                }
            }
            return templeBellPosition != null ? templeBellPosition.position : Vector3.zero;
        }

        // ── Legacy mission marker spawning (kept for Village_Flood scene) ──────
        // Creates cylinder poles for scenes that don't have pre-placed buildings.

        private void SpawnMissionMarkers(MissionData vm02, MissionData vm03, MissionData vm04,
                                         MissionData vm05, MissionData vs01, MissionData vs02, MissionData vs05)
        {
            Vector3 anchor = templeBellPosition != null
                ? templeBellPosition.position
                : Vector3.zero;

            // Offsets are in world XZ around the temple bell, then snapped to ground.
            // (-X = west, +Z = north)

            // VM02 "Rising Tide": Village center marker (just for waypoint guidance, civilians ARE the objective)
            vm02.waypointPosition = SpawnGroundedMarker(anchor + new Vector3(-25f, 0f, -20f),
                "VM02_VillageMarker", "VM02", null, "Village Center", new Color(1f, 0.5f, 0.1f), 0);

            // VM03 "Bridge Run": single bridge marker (also waypoint), counted by NPC rescues like VM02
            vm03.waypointPosition = SpawnGroundedMarker(anchor + new Vector3(-50f, 0f, 5f),
                "VM03_BridgeMarker", "VM03", null, "Bridge", new Color(0.6f, 0.4f, 0.2f), 0);

            // VM04 "The Cellar": cellar entrance + child rescue (sequential)
            Vector3 cellarPos = SpawnGroundedMarker(anchor + new Vector3(15f, 0f, -25f),
                "VM04_CellarEntrance", "VM04", "enter_cellar", "Cellar Entrance", new Color(0.3f, 0.6f, 1f), 1);
            vm04.waypointPosition = cellarPos;
            // Spawn the trapped child a few meters away (inside the "cellar")
            SpawnGroundedMarker(cellarPos + new Vector3(3f, 0f, 2f),
                "VM04_TrappedChild", "VM04", "rescue_child", "Trapped Child", new Color(1f, 0.3f, 0.5f), 1);

            // VM05 already has extractionPosition. If it's null, fall back to a hilltop marker.
            if (vm05.waypointPosition == Vector3.zero)
            {
                vm05.waypointPosition = SpawnGroundedMarker(anchor + new Vector3(60f, 0f, 60f),
                    "VM05_Extraction", "VM05", "reach_extraction", "Extraction Point", new Color(0.2f, 1f, 0.2f), 1);
            }

            // VS01 "Medic Run": clinic + temple delivery
            Vector3 clinicPos = SpawnGroundedMarker(anchor + new Vector3(20f, 0f, 15f),
                "VS01_Clinic", "VS01", "get_supplies", "Clinic Supplies", new Color(1f, 1f, 1f), 1);
            vs01.waypointPosition = clinicPos;
            SpawnGroundedMarker(anchor + new Vector3(2f, 0f, 0f),
                "VS01_TempleDelivery", "VS01", "deliver_supplies", "Deliver to Temple", new Color(0.9f, 0.9f, 0.5f), 1);

            // VS02 "Animal Rescue": 3 barn gates
            Vector3 barnCenter = anchor + new Vector3(-15f, 0f, 25f);
            vs02.waypointPosition = SpawnGroundedMarker(barnCenter,
                "VS02_BarnGate1", "VS02", "open_gates", "Barn Gate", new Color(0.8f, 0.6f, 0.3f), 1);
            SpawnGroundedMarker(barnCenter + new Vector3(4f, 0f, 0f),
                "VS02_BarnGate2", "VS02", "open_gates", "Barn Gate", new Color(0.8f, 0.6f, 0.3f), 1);
            SpawnGroundedMarker(barnCenter + new Vector3(-4f, 0f, 2f),
                "VS02_BarnGate3", "VS02", "open_gates", "Barn Gate", new Color(0.8f, 0.6f, 0.3f), 1);

            // VS05 "Signal Fire": radio tower on a hill
            vs05.waypointPosition = SpawnGroundedMarker(anchor + new Vector3(40f, 0f, -45f),
                "VS05_RadioTower", "VS05", "send_sos", "Radio Tower", new Color(0.4f, 0.85f, 1f), 1);
        }

        /// <summary>
        /// Creates a tall glowing cylinder at the given XZ, snapped to the ground via raycast.
        /// If interactionCount > 0, also attaches a MissionInteractable that ticks the objective.
        /// Returns the world position of the marker.
        /// </summary>
        private Vector3 SpawnGroundedMarker(Vector3 pos, string name, string missionId, string objectiveId,
                                            string label, Color glow, int interactionCount)
        {
            // Snap to ground via raycast from high above. Find the actual
            // Terrain hit beneath any building structures so the marker lands
            // on the ground next to a building, not on a roof/platform/column.
            {
                Vector3 origin = new Vector3(pos.x, 300f, pos.z);
                var hits = Physics.RaycastAll(origin, Vector3.down, 600f, ~0, QueryTriggerInteraction.Ignore);
                if (hits != null && hits.Length > 0)
                {
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    float terrainY = float.NaN;
                    // First pass: prefer an actual Unity Terrain or any object literally named "Terrain".
                    foreach (var h in hits)
                    {
                        if (h.collider is TerrainCollider ||
                            h.collider.gameObject.name.Equals("Terrain", System.StringComparison.OrdinalIgnoreCase))
                        {
                            terrainY = h.point.y;
                            break;
                        }
                    }
                    // Fallback: pick the LOWEST hit that isn't water — that's
                    // the deepest non-water surface, which is the ground.
                    if (float.IsNaN(terrainY))
                    {
                        for (int i = hits.Length - 1; i >= 0; i--)
                        {
                            string n = hits[i].collider.gameObject.name.ToLowerInvariant();
                            if (n.Contains("water")) continue;
                            terrainY = hits[i].point.y;
                            break;
                        }
                    }
                    if (!float.IsNaN(terrainY)) pos.y = terrainY;
                }
            }

            var go = new GameObject(name);
            go.transform.position = pos;

            // Visible glowing pillar
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.SetParent(go.transform, false);
            pillar.transform.localPosition = new Vector3(0f, 3f, 0f);
            pillar.transform.localScale = new Vector3(0.6f, 3f, 0.6f);
            // Remove the pillar's own collider so it doesn't block the player
            var pillarCol = pillar.GetComponent<Collider>();
            if (pillarCol != null) Destroy(pillarCol);

            // Emissive material
            var rend = pillar.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = glow;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glow * 3f);
            rend.material = mat;

            // Floating beacon — a thin tall cylinder above
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Beacon";
            beacon.transform.SetParent(go.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 12f, 0f);
            beacon.transform.localScale = new Vector3(0.15f, 6f, 0.15f);
            var beaconCol = beacon.GetComponent<Collider>();
            if (beaconCol != null) Destroy(beaconCol);
            var beaconRend = beacon.GetComponent<Renderer>();
            var beaconMat = new Material(mat);
            beaconRend.material = beaconMat;

            // Floating world-space label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 7f, 0f);
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = label;
            tm.fontSize = 48;
            tm.characterSize = 0.1f;
            tm.color = Color.white;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            labelGo.AddComponent<MissionMarkerBillboard>();

            // Interactable trigger sphere
            if (interactionCount > 0 && !string.IsNullOrEmpty(objectiveId))
            {
                var trigger = go.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 2.5f;
                trigger.center = new Vector3(0f, 1.5f, 0f);

                var mi = go.AddComponent<MissionInteractable>();
                // Configure via reflection since fields are private/SerializeField
                var t = typeof(MissionInteractable);
                var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                t.GetField("missionId", bf)?.SetValue(mi, missionId);
                t.GetField("objectiveId", bf)?.SetValue(mi, objectiveId);
                t.GetField("promptText", bf)?.SetValue(mi, $"[E] {label}");
                t.GetField("holdTime", bf)?.SetValue(mi, 1.0f);
                t.GetField("singleUse", bf)?.SetValue(mi, true);
                t.GetField("disableOnUse", bf)?.SetValue(mi, false);
            }

            return pos;
        }

        private void OnPhaseChanged(FloodPhase phase)
        {
            switch (phase)
            {
                case FloodPhase.Warning:
                    // VM01 should already be active
                    break;

                case FloodPhase.DisasterStrikes:
                    // Fail VM01 if not completed
                    var vm01 = mm.FindMissionById("VM01");
                    if (vm01 != null && vm01.status == MissionStatus.Active)
                        mm.FailMission(vm01);

                    // Auto-start Phase 2 missions
                    AutoStartMission("VM02");
                    AutoStartMission("VM03");
                    AutoStartMission("VM04");
                    AutoStartMission("VS01");
                    AutoStartMission("VS02");
                    break;

                case FloodPhase.Escape:
                    // Fail incomplete Phase 2 missions
                    FailIncomplete("VM02");
                    FailIncomplete("VM03");
                    FailIncomplete("VM04");

                    // Start escape mission
                    AutoStartMission("VM05");
                    AutoStartMission("VS05");
                    break;
            }
        }

        private void AutoStartMission(string id)
        {
            var mission = mm.FindMissionById(id);
            if (mission != null && (mission.status == MissionStatus.Locked || mission.status == MissionStatus.Available))
            {
                mission.status = MissionStatus.Available;
                mm.StartMission(mission);
            }
        }

        private void FailIncomplete(string id)
        {
            var mission = mm.FindMissionById(id);
            if (mission != null && mission.status == MissionStatus.Active)
                mm.FailMission(mission);
        }

        private void OnMissionCompleted(MissionData mission)
        {
            switch (mission.missionId)
            {
                case "VM01":
                    // Bell rung - alert all NPCs
                    if (NPCSpawner.Instance != null)
                    {
                        foreach (var npc in NPCSpawner.Instance.GetAllNPCs())
                        {
                            if (npc.CurrentState == NPCState.Normal)
                            {
                                // Use reflection or public method to set alert
                                npc.Interact(FindAnyObjectByType<FightForLife.Player.PlayerController>().gameObject);
                            }
                        }
                    }
                    Debug.Log("[MissionFlow] Village warned! NPCs going to Alert state.");

                    // Ringing the bell triggers the disaster phase, which auto-starts VM02-VM04 + secondaries.
                    if (fm != null)
                        fm.ForceAdvancePhase(FloodPhase.DisasterStrikes);
                    else
                    {
                        // Fallback: directly start phase-2 missions if FloodManager is missing
                        AutoStartMission("VM02");
                        AutoStartMission("VM03");
                        AutoStartMission("VM04");
                        AutoStartMission("VS01");
                        AutoStartMission("VS02");
                    }
                    break;

                case "VM05":
                    // Player escaped!
                    if (GameManager.Instance != null)
                        GameManager.Instance.GameOver(true);
                    break;
            }
        }

        private void OnMissionFailed(MissionData mission)
        {
            if (mission.missionId == "VM05")
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.GameOver(false);
            }
        }
    }

    /// <summary>
    /// Simple billboard so floating mission labels always face the camera.
    /// </summary>
    public class MissionMarkerBillboard : MonoBehaviour
    {
        private UnityEngine.Camera cam;
        private void LateUpdate()
        {
            if (cam == null) cam = UnityEngine.Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
