using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FightForLife.Core
{
    /// <summary>
    /// Runtime fixup pass that snaps all trees to the terrain so they don't
    /// float, and snaps any civilian NPCs that ended up on top of building
    /// roofs back down to ground level.
    /// Runs once after the scene loads.
    /// </summary>
    public static class WorldGroundSnapper
    {
        // Names of objects we never want to land on (rooftops, walls, civilians).
        private static readonly string[] BLOCKED_KEYWORDS = new[]
        {
            "house", "temple", "barn", "hut", "cabin", "shed", "tower",
            "roof", "wall", "mayor", "civilian", "npc", "fence", "building"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            // Defer one frame so the scene builders / spawners finish first.
            var runner = new GameObject("[WorldGroundSnapper]");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<Runner>();
        }

        private class Runner : MonoBehaviour
        {
            private IEnumerator Start()
            {
                // Wait two frames so NPCSpawner.Start has run.
                yield return null;
                yield return null;
                SnapAllTrees();
                SnapAllCivilians();
                // do not destroy — the runner stays alive to police rooftops
                // Periodically rescue any civilians that have wandered onto
                // building roofs (NavMesh sometimes lets them path up).
                while (true)
                {
                    yield return new WaitForSeconds(3f);
                    SnapAllCivilians();
                }
            }
        }

        public static void SnapAllTrees()
        {
            var all = Object.FindObjectsOfType<GameObject>();
            int snapped = 0;
            foreach (var o in all)
            {
                if (o == null) continue;
                if (!o.name.StartsWith("Tree")) continue;
                // only operate on top-level tree objects (parent is the Trees container)
                if (o.transform.parent == null) continue;
                if (TrySnapToGround(o.transform, 0f, /*allowBuildings*/false))
                    snapped++;
            }
            Debug.Log($"[WorldGroundSnapper] Snapped {snapped} trees to terrain.");
        }

        public static void SnapAllCivilians()
        {
            var civs = Object.FindObjectsOfType<FightForLife.NPC.CivilianAI>();
            int moved = 0;
            foreach (var c in civs)
            {
                if (c == null) continue;
                Vector3 newPos;
                if (TryFindClearGroundPosition(c, out newPos))
                {
                    var agent = c.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && agent.enabled)
                    {
                        // Warp re-syncs the NavMeshAgent to the new spot so it
                        // doesn't get pulled back to its old NavMesh point.
                        agent.Warp(newPos);
                    }
                    else
                    {
                        c.transform.position = newPos;
                    }
                    moved++;
                }
            }
            Debug.Log($"[WorldGroundSnapper] Repositioned {moved} civilians off/inside buildings.");
        }

        /// <summary>
        /// Returns a ground position outside any building bounds. If the
        /// civilian is currently inside a building, walks outward in a spiral
        /// until clear ground is found.
        /// </summary>
        private static bool TryFindClearGroundPosition(FightForLife.NPC.CivilianAI c, out Vector3 result)
        {
            result = c.transform.position;
            Vector3 origin = c.transform.position;

            // Step 1: gather building bounds we overlap right now.
            var overlaps = Physics.OverlapSphere(origin, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            Bounds? insideBuilding = null;
            foreach (var col in overlaps)
            {
                if (col == null || col.gameObject == c.gameObject) continue;
                if (!IsBlocked(col.gameObject)) continue;
                if (col.bounds.Contains(origin))
                {
                    insideBuilding = col.bounds;
                    break;
                }
            }

            // Step 2: pick a candidate XZ. If inside a building, push out past
            // its bounds; otherwise just snap straight down.
            Vector3 candidateXZ = new Vector3(origin.x, 0, origin.z);
            if (insideBuilding.HasValue)
            {
                Bounds b = insideBuilding.Value;
                Vector3 center = new Vector3(b.center.x, 0, b.center.z);
                Vector3 dir = new Vector3(origin.x - center.x, 0, origin.z - center.z);
                if (dir.sqrMagnitude < 0.01f) dir = Vector3.right;
                dir.Normalize();
                float push = Mathf.Max(b.extents.x, b.extents.z) + 2f;
                candidateXZ = center + dir * push;
            }

            // Step 3: try the candidate, then a small spiral if it's still blocked.
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3 tryPos;
                if (attempt == 0)
                {
                    tryPos = candidateXZ;
                }
                else
                {
                    float ang = attempt * 60f * Mathf.Deg2Rad;
                    float r = 2f + attempt * 0.8f;
                    tryPos = candidateXZ + new Vector3(Mathf.Cos(ang) * r, 0, Mathf.Sin(ang) * r);
                }

                Vector3 rayOrigin = new Vector3(tryPos.x, 300f, tryPos.z);
                var hits = Physics.RaycastAll(rayOrigin, Vector3.down, 600f, ~0, QueryTriggerInteraction.Ignore);
                if (hits == null || hits.Length == 0) continue;
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    if (h.collider.gameObject == c.gameObject) continue;
                    if (h.collider.transform.IsChildOf(c.transform)) continue;
                    if (IsBlocked(h.collider.gameObject)) continue;
                    Vector3 ground = new Vector3(tryPos.x, h.point.y + 0.05f, tryPos.z);
                    // Reject if the ground point is itself inside a building.
                    var ovs = Physics.OverlapSphere(ground + Vector3.up * 0.5f, 0.4f, ~0, QueryTriggerInteraction.Ignore);
                    bool blockedHere = false;
                    foreach (var ov in ovs)
                    {
                        if (ov.gameObject == c.gameObject) continue;
                        if (IsBlocked(ov.gameObject)) { blockedHere = true; break; }
                    }
                    if (blockedHere) break; // try next attempt
                    // Snap to NavMesh if possible.
                    UnityEngine.AI.NavMeshHit nmh;
                    if (UnityEngine.AI.NavMesh.SamplePosition(ground, out nmh, 4f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        result = nmh.position;
                    }
                    else
                    {
                        result = ground;
                    }
                    return (result - origin).sqrMagnitude > 0.04f;
                }
            }
            return false;
        }

        /// <summary>
        /// Raycasts down from high above and places the transform on the first
        /// non-blocked surface (Terrain / ground). Returns true if a hit was found.
        /// </summary>
        private static bool TrySnapToGround(Transform t, float yOffset, bool allowBuildings)
        {
            Vector3 origin = new Vector3(t.position.x, 300f, t.position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 600f, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                if (h.collider.transform == t || h.collider.transform.IsChildOf(t)) continue;
                if (!allowBuildings && IsBlocked(h.collider.gameObject)) continue;
                t.position = new Vector3(t.position.x, h.point.y + yOffset, t.position.z);
                return true;
            }
            return false;
        }

        private static bool IsBlocked(GameObject go)
        {
            string n = go.name.ToLowerInvariant();
            foreach (var k in BLOCKED_KEYWORDS)
                if (n.Contains(k)) return true;
            // Walk up parents — building parts are nested under named building roots.
            var p = go.transform.parent;
            int depth = 0;
            while (p != null && depth < 4)
            {
                string pn = p.name.ToLowerInvariant();
                foreach (var k in BLOCKED_KEYWORDS)
                    if (pn.Contains(k)) return true;
                p = p.parent;
                depth++;
            }
            return false;
        }
    }
}
