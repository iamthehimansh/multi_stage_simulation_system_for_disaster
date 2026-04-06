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
            mm = MissionManager.Instance;
            fm = FloodManager.Instance;

            if (mm == null) { Debug.LogError("[MissionFlow] No MissionManager!"); return; }

            CreateAllMissions();

            if (fm != null)
                fm.OnPhaseChanged += OnPhaseChanged;

            mm.OnMissionCompleted += OnMissionCompleted;
            mm.OnMissionFailed += OnMissionFailed;

            // Start first mission immediately
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

            Debug.Log($"[MissionFlow] Created {missions.Count} missions.");
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
}
