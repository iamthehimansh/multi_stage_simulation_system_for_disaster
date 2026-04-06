using UnityEngine;
using FightForLife.Core;
using FightForLife.Player;
using FightForLife.NPC;

namespace FightForLife.Missions
{
    /// <summary>
    /// Generic trigger zone that updates a mission objective when player/NPC enters.
    /// Used for: reach_temple, reach_extraction, guide_across bridge, etc.
    /// </summary>
    public class MissionZone : MonoBehaviour
    {
        [Header("Mission")]
        [SerializeField] private string missionId;
        [SerializeField] private string objectiveId;

        [Header("Detection")]
        [SerializeField] private bool detectPlayer = true;
        [SerializeField] private bool detectFollowingNPCs;
        [SerializeField] private bool oneShot = true;

        private bool triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered && oneShot) return;

            if (detectPlayer && other.GetComponent<PlayerController>() != null)
            {
                TriggerObjective();
            }

            if (detectFollowingNPCs)
            {
                var npc = other.GetComponent<CivilianAI>();
                if (npc != null && npc.CurrentState == NPCState.Following)
                {
                    npc.MarkRescued();
                    TriggerObjective();
                }
            }
        }

        private void TriggerObjective()
        {
            if (MissionManager.Instance != null)
                MissionManager.Instance.UpdateObjective(missionId, objectiveId);

            if (oneShot && detectPlayer)
                triggered = true;

            Debug.Log($"[MissionZone] {missionId}/{objectiveId} triggered");
        }
    }
}
