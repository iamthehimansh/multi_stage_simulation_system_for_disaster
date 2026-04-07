using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
#endif

namespace FightForLife.Core
{
#if UNITY_EDITOR
    public class VillageFloodSceneBuilder : MonoBehaviour
    {
        // Colors
        static readonly Color GROUND = new Color(0.35f, 0.25f, 0.15f);
        static readonly Color GRASS = new Color(0.2f, 0.45f, 0.15f);
        static readonly Color ROAD = new Color(0.4f, 0.38f, 0.35f);
        static readonly Color BRICK = new Color(0.55f, 0.3f, 0.2f);
        static readonly Color MUD = new Color(0.5f, 0.35f, 0.2f);
        static readonly Color ROOF_RED = new Color(0.6f, 0.2f, 0.15f);
        static readonly Color ROOF_BROWN = new Color(0.4f, 0.25f, 0.12f);
        static readonly Color WOOD = new Color(0.45f, 0.3f, 0.15f);
        static readonly Color STONE = new Color(0.5f, 0.5f, 0.48f);
        static readonly Color WATER = new Color(0.15f, 0.35f, 0.55f, 0.7f);
        static readonly Color TEMPLE_WHITE = new Color(0.85f, 0.82f, 0.78f);
        static readonly Color CONCRETE = new Color(0.6f, 0.58f, 0.55f);

        [MenuItem("Fight For Life/Build Village Flood Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ==================== LIGHTING ====================
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.6f;
            light.color = new Color(0.6f, 0.65f, 0.75f);
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(45, -30, 0);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.35f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.35f, 0.4f, 0.5f);
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 300f;

            // ==================== GROUND / TERRAIN ====================
            var ground = CreateBox("Ground", Vector3.zero, new Vector3(400, 1, 400), GRASS);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.isStatic = true;

            // Elevated north area (hills / safe zone)
            var hillNorth = CreateBox("Hill_North", new Vector3(0, 3, 150), new Vector3(200, 7, 100), GRASS);
            hillNorth.isStatic = true;
            // Slope ramp to hill
            var rampNorth = CreateBox("Ramp_North", new Vector3(0, 1.5f, 100), new Vector3(30, 0.5f, 50), GROUND);
            rampNorth.transform.rotation = Quaternion.Euler(-8, 0, 0);
            rampNorth.isStatic = true;

            // Lower south area (river bank - floods first)
            var lowSouth = CreateBox("LowGround_South", new Vector3(0, -1, -120), new Vector3(400, 1, 80), GROUND);
            lowSouth.isStatic = true;

            // Rice paddies (very low)
            var paddyL = CreateBox("RicePaddy_L", new Vector3(-80, -1.5f, -100), new Vector3(60, 0.5f, 50), new Color(0.3f, 0.4f, 0.15f));
            paddyL.isStatic = true;
            var paddyR = CreateBox("RicePaddy_R", new Vector3(60, -1.5f, -100), new Vector3(60, 0.5f, 50), new Color(0.3f, 0.4f, 0.15f));
            paddyR.isStatic = true;

            // ==================== ROADS ====================
            CreateBox("Road_Main_NS", new Vector3(0, 0.05f, 20), new Vector3(6, 0.1f, 250), ROAD).isStatic = true;
            CreateBox("Road_Main_EW", new Vector3(0, 0.05f, 0), new Vector3(200, 0.1f, 5), ROAD).isStatic = true;
            CreateBox("Road_Village_EW", new Vector3(0, 0.05f, 40), new Vector3(150, 0.1f, 4), ROAD).isStatic = true;

            // ==================== RIVER ====================
            var riverBed = CreateBox("RiverBed", new Vector3(0, -3, -160), new Vector3(400, 2, 30), new Color(0.2f, 0.15f, 0.1f));
            riverBed.isStatic = true;

            // ==================== BRIDGES ====================
            // Wooden bridge (will collapse)
            var bridgeWood = CreateBridge("Bridge_Wooden", new Vector3(-40, 0.3f, -160), 15, 4, WOOD);
            var bridgeStruct = bridgeWood.AddComponent<Disaster.StructuralDamage>();

            // Stone bridge (survives)
            CreateBridge("Bridge_Stone", new Vector3(40, 0.3f, -160), 15, 5, STONE);

            // ==================== VILLAGE CENTER ====================
            // Temple on hill (safe zone)
            var temple = CreateTemple(new Vector3(-20, 6.5f, 160));

            // Rescue zone at temple
            var templeRescue = CreateTriggerZone("RescueZone_Temple", new Vector3(-20, 7, 160), new Vector3(20, 5, 20));
            templeRescue.AddComponent<NPC.RescueZone>();

            // Village Square
            CreateBox("VillageSquare_Floor", new Vector3(0, 0.08f, 20), new Vector3(25, 0.15f, 25), STONE).isStatic = true;
            // Well
            var well = CreateCylinder("Well", new Vector3(0, 0.6f, 20), 1.5f, 1.2f, STONE);
            well.isStatic = true;

            // Market area
            CreateMarketStalls(new Vector3(-40, 0, 30));

            // Clinic
            CreateBuilding("Clinic", new Vector3(50, 0, 30), 10, 4, 8, TEMPLE_WHITE, ROOF_RED, 2);

            // School
            CreateBuilding("School", new Vector3(30, 3, 130), 15, 5, 10, TEMPLE_WHITE, ROOF_BROWN, 2);

            // ==================== HOUSES - CLUSTER A (Brick, 2-story) ====================
            CreateBuilding("House_Brick_01", new Vector3(-50, 0, 50), 8, 6, 7, BRICK, ROOF_RED, 2);
            CreateBuilding("House_Brick_02", new Vector3(-38, 0, 50), 7, 6, 8, BRICK, ROOF_RED, 2);
            CreateBuilding("House_Brick_03", new Vector3(-50, 0, 65), 8, 6, 7, BRICK, ROOF_BROWN, 2);
            CreateBuilding("House_Brick_04", new Vector3(-38, 0, 65), 7, 6, 8, BRICK, ROOF_RED, 2);
            CreateBuilding("House_Brick_05", new Vector3(35, 0, 50), 8, 6, 7, BRICK, ROOF_BROWN, 2);
            CreateBuilding("House_Brick_06", new Vector3(48, 0, 50), 7, 6, 8, BRICK, ROOF_RED, 2);
            CreateBuilding("House_Brick_07", new Vector3(35, 0, 65), 8, 6, 7, BRICK, ROOF_RED, 2);
            CreateBuilding("House_Brick_08", new Vector3(48, 0, 65), 7, 6, 8, BRICK, ROOF_BROWN, 2);

            // ==================== HOUSES - CLUSTER B (Mud, 1-story, weaker) ====================
            float mudY = -0.5f;
            for (int i = 0; i < 6; i++)
            {
                float x = -80 + i * 12;
                var house = CreateBuilding($"House_Mud_{i + 1:00}", new Vector3(x, mudY, -30), 6, 3.5f, 5, MUD, ROOF_BROWN, 1);
                var sd = house.AddComponent<Disaster.StructuralDamage>();
            }
            for (int i = 0; i < 6; i++)
            {
                float x = -80 + i * 12;
                var house = CreateBuilding($"House_Mud_{i + 7:00}", new Vector3(x, mudY, -50), 6, 3.5f, 5, MUD, ROOF_BROWN, 1);
                var sd = house.AddComponent<Disaster.StructuralDamage>();
            }

            // ==================== FARM ====================
            CreateBuilding("Barn", new Vector3(80, -0.5f, -70), 15, 6, 10, WOOD, ROOF_BROWN, 1);
            // Animal pens (fences)
            CreateBox("Fence_Pen_N", new Vector3(80, 0, -55), new Vector3(20, 1.2f, 0.2f), WOOD).isStatic = true;
            CreateBox("Fence_Pen_S", new Vector3(80, 0, -65), new Vector3(20, 1.2f, 0.2f), WOOD).isStatic = true;
            CreateBox("Fence_Pen_E", new Vector3(90, 0, -60), new Vector3(0.2f, 1.2f, 10), WOOD).isStatic = true;
            CreateBox("Fence_Pen_W", new Vector3(70, 0, -60), new Vector3(0.2f, 1.2f, 10), WOOD).isStatic = true;

            // ==================== TREES ====================
            var treeParent = new GameObject("Trees");
            System.Random rng = new System.Random(42);
            Vector3[] treeZones = {
                new Vector3(-90, 0, 80), new Vector3(90, 0, 80),
                new Vector3(-100, 0, 0), new Vector3(100, 0, 0),
                new Vector3(-60, 3, 140), new Vector3(60, 3, 140),
                new Vector3(-90, -0.5f, -80), new Vector3(90, -0.5f, -80),
            };
            foreach (var zone in treeZones)
            {
                for (int i = 0; i < 5; i++)
                {
                    float tx = zone.x + (float)(rng.NextDouble() * 30 - 15);
                    float tz = zone.z + (float)(rng.NextDouble() * 30 - 15);
                    var tree = CreateTree($"Tree_{treeParent.transform.childCount}", new Vector3(tx, zone.y, tz));
                    tree.transform.SetParent(treeParent.transform);
                }
            }

            // ==================== WATER PLANE ====================
            var waterPlane = CreateWaterPlane(new Vector3(0, -3, 0), 500);

            // ==================== PLAYER ====================
            var player = CreatePlayer(new Vector3(0, 1, 0));

            // ==================== CAMERA ====================
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.3f, 0.35f, 0.45f);
            cam.fieldOfView = 60;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            camObj.AddComponent<AudioListener>();
            var tpCam = camObj.AddComponent<FightForLife.Camera.ThirdPersonCamera>();
            camObj.transform.position = new Vector3(0, 3, -5);

            // Wire camera to player
            var tpCamSO = new SerializedObject(tpCam);
            var targetProp = tpCamSO.FindProperty("target");
            if (targetProp != null)
            {
                targetProp.objectReferenceValue = player.transform;
                tpCamSO.ApplyModifiedProperties();
            }

            // ==================== GAME MANAGERS ====================
            var managersObj = new GameObject("--- MANAGERS ---");

            // GameManager (if not DontDestroyOnLoad from menu)
            var gmObj = new GameObject("GameManager");
            gmObj.transform.SetParent(managersObj.transform);
            gmObj.AddComponent<GameManager>();

            var scoreObj = new GameObject("ScoreManager");
            scoreObj.transform.SetParent(managersObj.transform);
            scoreObj.AddComponent<ScoreManager>();

            var missionObj = new GameObject("MissionManager");
            missionObj.transform.SetParent(managersObj.transform);
            missionObj.AddComponent<MissionManager>();

            var checkpointObj = new GameObject("CheckpointSystem");
            checkpointObj.transform.SetParent(managersObj.transform);
            var cpSys = checkpointObj.AddComponent<CheckpointSystem>();

            // ==================== FLOOD MANAGER ====================
            var floodObj = new GameObject("FloodManager");
            floodObj.transform.SetParent(managersObj.transform);
            var floodMgr = floodObj.AddComponent<Disaster.FloodManager>();

            // Create FloodConfig asset
            var floodConfig = ScriptableObject.CreateInstance<Disaster.FloodConfig>();
            string configPath = "Assets/_FightForLife/ScriptableObjects/VillageFloodConfig.asset";
            AssetDatabase.CreateAsset(floodConfig, configPath);

            var floodSO = new SerializedObject(floodMgr);
            floodSO.FindProperty("config").objectReferenceValue = floodConfig;
            floodSO.FindProperty("waterPlane").objectReferenceValue = waterPlane.transform;
            floodSO.ApplyModifiedProperties();

            // ==================== WEATHER ====================
            var weatherObj = new GameObject("WeatherSystem");
            weatherObj.transform.SetParent(managersObj.transform);
            var weather = weatherObj.AddComponent<Disaster.WeatherSystem>();
            var weatherSO = new SerializedObject(weather);
            var dlProp = weatherSO.FindProperty("lightningLight");
            if (dlProp != null)
            {
                dlProp.objectReferenceValue = light;
                weatherSO.ApplyModifiedProperties();
            }

            // ==================== NPC SPAWNER ====================
            var npcParent = new GameObject("--- NPCs ---");
            var spawnerObj = new GameObject("NPCSpawner");
            spawnerObj.transform.SetParent(npcParent.transform);
            var spawner = spawnerObj.AddComponent<NPC.NPCSpawner>();

            // Create NPC spawn points
            Vector3[] spawnPositions = {
                // Cluster B houses (low ground, most vulnerable)
                new Vector3(-80, 0, -30), new Vector3(-68, 0, -30), new Vector3(-56, 0, -30),
                new Vector3(-44, 0, -30), new Vector3(-32, 0, -30), new Vector3(-20, 0, -30),
                new Vector3(-80, 0, -50), new Vector3(-68, 0, -50), new Vector3(-56, 0, -50),
                new Vector3(-44, 0, -50), new Vector3(-32, 0, -50), new Vector3(-20, 0, -50),
                // Village center
                new Vector3(5, 0, 25), new Vector3(-5, 0, 15), new Vector3(10, 0, 20),
                // Cluster A (safer)
                new Vector3(-50, 0, 55), new Vector3(-38, 0, 55), new Vector3(35, 0, 55),
                new Vector3(48, 0, 55), new Vector3(-50, 0, 70), new Vector3(48, 0, 70),
                // Market area
                new Vector3(-35, 0, 35), new Vector3(-45, 0, 25),
                // Farm area
                new Vector3(80, 0, -60), new Vector3(75, 0, -75),
                // Near bridges (south bank)
                new Vector3(-40, 0, -140), new Vector3(40, 0, -140),
                new Vector3(-45, 0, -150), new Vector3(45, 0, -150),
                new Vector3(0, 0, -145),
            };

            var spawnPointsParent = new GameObject("SpawnPoints");
            spawnPointsParent.transform.SetParent(npcParent.transform);
            Transform[] spawnTransforms = new Transform[spawnPositions.Length];
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                var sp = new GameObject($"Spawn_{i:00}");
                sp.transform.position = spawnPositions[i];
                sp.transform.SetParent(spawnPointsParent.transform);
                spawnTransforms[i] = sp.transform;
            }

            var spawnerSO = new SerializedObject(spawner);
            var spawnPointsProp = spawnerSO.FindProperty("spawnPoints");
            if (spawnPointsProp != null)
            {
                spawnPointsProp.arraySize = spawnTransforms.Length;
                for (int i = 0; i < spawnTransforms.Length; i++)
                {
                    spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];
                }
                spawnerSO.ApplyModifiedProperties();
            }

            // ==================== RESCUE ZONES ====================
            // Hill top rescue
            var rescueHill = CreateTriggerZone("RescueZone_Hill", new Vector3(0, 7, 180), new Vector3(30, 5, 20));
            rescueHill.AddComponent<NPC.RescueZone>();

            // ==================== EXTRACTION POINT ====================
            var extractionPoint = CreateTriggerZone("ExtractionPoint", new Vector3(0, 7.5f, 190), new Vector3(10, 5, 10));
            // Visual marker
            var extractMarker = CreateCylinder("ExtractionMarker", new Vector3(0, 10, 190), 0.5f, 5f, new Color(0, 1, 0, 0.5f));

            // ==================== CHECKPOINTS ====================
            var cp1 = CreateTriggerZone("Checkpoint_VillageCenter", new Vector3(0, 1, 20), new Vector3(10, 4, 10));
            cp1.AddComponent<Checkpoint>();

            var cp2 = CreateTriggerZone("Checkpoint_Temple", new Vector3(-20, 7, 150), new Vector3(10, 4, 10));
            cp2.AddComponent<Checkpoint>();

            var cp3 = CreateTriggerZone("Checkpoint_Hill", new Vector3(0, 7, 170), new Vector3(10, 4, 10));
            cp3.AddComponent<Checkpoint>();

            // Wire default spawn to checkpoint system
            var cpSysSO = new SerializedObject(cpSys);
            var defSpawnProp = cpSysSO.FindProperty("defaultSpawnPoint");
            if (defSpawnProp != null)
            {
                defSpawnProp.objectReferenceValue = player.transform;
                cpSysSO.ApplyModifiedProperties();
            }

            // ==================== ITEM PICKUPS ====================
            CreateItemPickup("FirstAid_01", new Vector3(52, 1, 32), new Color(1, 0.3f, 0.3f));
            CreateItemPickup("FirstAid_02", new Vector3(-20, 7.5f, 155), new Color(1, 0.3f, 0.3f));
            CreateItemPickup("Rope_01", new Vector3(-38, 1, 28), new Color(0.6f, 0.4f, 0.2f));
            CreateItemPickup("Flashlight_01", new Vector3(-48, 1, 52), new Color(1, 1, 0.5f));
            CreateItemPickup("LifeJacket_01", new Vector3(80, 1, -68), new Color(1, 0.5f, 0));
            CreateItemPickup("Food_01", new Vector3(-42, 1, 32), new Color(0.4f, 0.8f, 0.3f));
            CreateItemPickup("WaterBottle_01", new Vector3(5, 1, 22), new Color(0.3f, 0.6f, 1));

            // ==================== HAZARD ZONES ====================
            var hazardElectric = CreateTriggerZone("Hazard_Electric", new Vector3(50, 0, 20), new Vector3(8, 3, 8));
            var hazComp = hazardElectric.AddComponent<Disaster.Hazard>();

            // ==================== HUD CANVAS ====================
            CreateGameHUD();

            // ==================== NAVMESH ====================
            // Mark ground as Navigation Static
            StaticEditorFlags navFlags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(ground, navFlags);
            GameObjectUtility.SetStaticEditorFlags(hillNorth, navFlags);
            GameObjectUtility.SetStaticEditorFlags(lowSouth, navFlags);
            GameObjectUtility.SetStaticEditorFlags(rampNorth, navFlags);

            // ==================== SAVE ====================
            string scenePath = "Assets/_FightForLife/Scenes/Village_Flood.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Add to build settings
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool exists = false;
            foreach (var s in buildScenes)
            {
                if (s.path == scenePath) { exists = true; break; }
            }
            if (!exists)
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            Debug.Log("<color=green>[Fight For Life]</color> Village Flood scene built! Now bake NavMesh: Window > AI > Navigation > Bake");
            EditorUtility.DisplayDialog("Fight For Life",
                "Village Flood scene built successfully!\n\n" +
                "IMPORTANT: Bake NavMesh before playing:\n" +
                "Window > AI > Navigation > Bake\n\n" +
                "Then press Play to test!",
                "OK");
        }

        // ==================== BUILDER HELPERS ====================

        static Material CreateMat(Color color, bool transparent = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            if (transparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
            return mat;
        }

        static GameObject CreateBox(string name, Vector3 pos, Vector3 size, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().material = CreateMat(color);
            return obj;
        }

        static GameObject CreateCylinder(string name, Vector3 pos, float radius, float height, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(radius * 2, height / 2f, radius * 2);
            obj.GetComponent<Renderer>().material = CreateMat(color);
            return obj;
        }

        static GameObject CreateTree(string name, Vector3 pos)
        {
            var tree = new GameObject(name);
            tree.transform.position = pos;
            tree.isStatic = true;

            // Trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, 2, 0);
            trunk.transform.localScale = new Vector3(0.4f, 2, 0.4f);
            trunk.GetComponent<Renderer>().material = CreateMat(WOOD);
            trunk.isStatic = true;

            // Canopy
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform);
            canopy.transform.localPosition = new Vector3(0, 5, 0);
            canopy.transform.localScale = new Vector3(4, 3, 4);
            canopy.GetComponent<Renderer>().material = CreateMat(new Color(0.15f, 0.4f + Random.Range(0, 0.15f), 0.1f));
            canopy.isStatic = true;

            return tree;
        }

        static GameObject CreateBuilding(string name, Vector3 pos, float width, float height, float depth, Color wallColor, Color roofColor, int floors)
        {
            var building = new GameObject(name);
            building.transform.position = pos;
            building.isStatic = true;

            float totalHeight = height * floors;

            // Walls
            var walls = CreateBox($"{name}_Walls", new Vector3(0, totalHeight / 2f, 0), new Vector3(width, totalHeight, depth), wallColor);
            walls.transform.SetParent(building.transform, false);
            walls.isStatic = true;

            // Roof
            var roof = CreateBox($"{name}_Roof", new Vector3(0, totalHeight + 0.3f, 0), new Vector3(width + 0.5f, 0.5f, depth + 0.5f), roofColor);
            roof.transform.SetParent(building.transform, false);
            roof.isStatic = true;

            // Door (opening)
            var door = CreateBox($"{name}_Door", new Vector3(0, 1.2f, depth / 2f + 0.05f), new Vector3(1.5f, 2.4f, 0.3f), new Color(0.3f, 0.2f, 0.1f));
            door.transform.SetParent(building.transform, false);
            door.isStatic = true;

            // Windows per floor
            for (int f = 0; f < floors; f++)
            {
                float wy = (f * height) + height * 0.6f;
                // Front windows
                var winL = CreateBox($"{name}_Win_F{f}_L", new Vector3(-width * 0.3f, wy, depth / 2f + 0.05f), new Vector3(1, 1, 0.15f), new Color(0.6f, 0.8f, 0.9f, 0.7f));
                winL.transform.SetParent(building.transform, false);
                winL.isStatic = true;

                var winR = CreateBox($"{name}_Win_F{f}_R", new Vector3(width * 0.3f, wy, depth / 2f + 0.05f), new Vector3(1, 1, 0.15f), new Color(0.6f, 0.8f, 0.9f, 0.7f));
                winR.transform.SetParent(building.transform, false);
                winR.isStatic = true;
            }

            // Floor colliders for each story (walkable upper floors)
            for (int f = 1; f < floors; f++)
            {
                var floor = CreateBox($"{name}_Floor_{f}", new Vector3(0, f * height, 0), new Vector3(width - 0.2f, 0.15f, depth - 0.2f), wallColor);
                floor.transform.SetParent(building.transform, false);
                floor.isStatic = true;
            }

            return building;
        }

        static GameObject CreateTemple(Vector3 pos)
        {
            var temple = new GameObject("Temple");
            temple.transform.position = pos;
            temple.isStatic = true;

            // Main structure
            var main = CreateBox("Temple_Main", Vector3.zero, new Vector3(18, 8, 14), TEMPLE_WHITE);
            main.transform.SetParent(temple.transform, false);
            main.isStatic = true;

            // Stepped base
            var base1 = CreateBox("Temple_Base", new Vector3(0, -0.75f, 0), new Vector3(22, 1.5f, 18), STONE);
            base1.transform.SetParent(temple.transform, false);
            base1.isStatic = true;

            // Roof
            var roof = CreateBox("Temple_Roof", new Vector3(0, 4.5f, 0), new Vector3(20, 0.8f, 16), ROOF_RED);
            roof.transform.SetParent(temple.transform, false);
            roof.isStatic = true;

            // Bell tower
            var tower = CreateBox("Temple_Tower", new Vector3(0, 7, 0), new Vector3(4, 6, 4), TEMPLE_WHITE);
            tower.transform.SetParent(temple.transform, false);
            tower.isStatic = true;

            var towerRoof = CreateBox("Temple_TowerRoof", new Vector3(0, 10.5f, 0), new Vector3(5, 0.6f, 5), ROOF_RED);
            towerRoof.transform.SetParent(temple.transform, false);
            towerRoof.isStatic = true;

            // Bell (sphere)
            var bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bell.name = "Temple_Bell";
            bell.transform.SetParent(temple.transform, false);
            bell.transform.localPosition = new Vector3(0, 8.5f, 0);
            bell.transform.localScale = Vector3.one * 0.8f;
            bell.GetComponent<Renderer>().material = CreateMat(new Color(0.7f, 0.6f, 0.2f));
            bell.isStatic = true;

            // Entrance
            var entrance = CreateBox("Temple_Door", new Vector3(0, 1.5f, 7.1f), new Vector3(3, 3, 0.3f), WOOD);
            entrance.transform.SetParent(temple.transform, false);
            entrance.isStatic = true;

            return temple;
        }

        static GameObject CreateBridge(string name, Vector3 pos, float length, float width, Color color)
        {
            var bridge = new GameObject(name);
            bridge.transform.position = pos;
            bridge.isStatic = true;

            // Deck
            var deck = CreateBox($"{name}_Deck", Vector3.zero, new Vector3(width, 0.4f, length), color);
            deck.transform.SetParent(bridge.transform, false);
            deck.isStatic = true;

            // Railings
            var railL = CreateBox($"{name}_Rail_L", new Vector3(-width / 2f, 0.6f, 0), new Vector3(0.15f, 1, length), color);
            railL.transform.SetParent(bridge.transform, false);
            railL.isStatic = true;

            var railR = CreateBox($"{name}_Rail_R", new Vector3(width / 2f, 0.6f, 0), new Vector3(0.15f, 1, length), color);
            railR.transform.SetParent(bridge.transform, false);
            railR.isStatic = true;

            return bridge;
        }

        static void CreateMarketStalls(Vector3 center)
        {
            var market = new GameObject("Market");
            market.transform.position = center;

            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 6;
                var stall = new GameObject($"Stall_{i + 1}");
                stall.transform.SetParent(market.transform);
                stall.transform.localPosition = new Vector3(x, 0, 0);
                stall.isStatic = true;

                // Counter
                var counter = CreateBox($"Counter_{i}", new Vector3(0, 0.5f, 0), new Vector3(4, 1, 2), WOOD);
                counter.transform.SetParent(stall.transform, false);
                counter.isStatic = true;

                // Canopy
                var canopy = CreateBox($"Canopy_{i}", new Vector3(0, 2.5f, 0), new Vector3(4.5f, 0.1f, 3), new Color(0.7f, 0.15f + i * 0.15f, 0.1f));
                canopy.transform.SetParent(stall.transform, false);
                canopy.isStatic = true;

                // Support poles
                for (int p = 0; p < 4; p++)
                {
                    float px = (p < 2 ? -1.8f : 1.8f);
                    float pz = (p % 2 == 0 ? -1.2f : 1.2f);
                    var pole = CreateCylinder($"Pole_{i}_{p}", new Vector3(px, 1.25f, pz), 0.05f, 2.5f, WOOD);
                    pole.transform.SetParent(stall.transform, false);
                    pole.isStatic = true;
                }
            }
        }

        static GameObject CreateWaterPlane(Vector3 pos, float size)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "WaterPlane";
            water.transform.position = pos;
            water.transform.localScale = new Vector3(size, 0.1f, size);
            water.GetComponent<Renderer>().material = CreateMat(WATER, true);

            // Remove collider (water detection is done via FloodManager level)
            var col = water.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            // Add water zone trigger
            var boxCol = water.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(1, 50, 1); // Tall trigger

            water.AddComponent<Disaster.WaterZone>();

            return water;
        }

        static GameObject CreatePlayer(Vector3 pos)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");
            player.transform.position = pos;

            // Body (capsule visual)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform);
            body.transform.localPosition = new Vector3(0, 1, 0);
            body.transform.localScale = new Vector3(0.8f, 1, 0.8f);
            body.GetComponent<Renderer>().material = CreateMat(new Color(0.2f, 0.4f, 0.7f));
            // Remove capsule's own collider (CharacterController handles collision)
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(player.transform);
            head.transform.localPosition = new Vector3(0, 2.1f, 0);
            head.transform.localScale = Vector3.one * 0.45f;
            head.GetComponent<Renderer>().material = CreateMat(new Color(0.85f, 0.7f, 0.55f));
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // Camera target point
            var camTarget = new GameObject("CameraTarget");
            camTarget.transform.SetParent(player.transform);
            camTarget.transform.localPosition = new Vector3(0, 1.6f, 0);

            // CharacterController
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 1, 0);

            // Player scripts
            player.AddComponent<Player.PlayerHealth>();
            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerInventory>();
            player.AddComponent<Player.InteractionSystem>();
            player.AddComponent<Player.PlayerAnimationController>();

            return player;
        }

        static GameObject CreateTriggerZone(string name, Vector3 pos, Vector3 size)
        {
            var obj = new GameObject(name);
            obj.transform.position = pos;
            var col = obj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            return obj;
        }

        static void CreateItemPickup(string name, Vector3 pos, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = $"Item_{name}";
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * 0.4f;
            obj.GetComponent<Renderer>().material = CreateMat(color);
            obj.AddComponent<Player.ItemPickup>();
        }

        static void CreateGameHUD()
        {
            var canvasObj = new GameObject("GameHUD_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var hudMgr = canvasObj.AddComponent<UI.HUDManager>();

            // --- TOP LEFT: Health, Stamina, Oxygen ---
            var barsGroup = CreateUIPanel(canvasObj.transform, "BarsGroup",
                new Vector2(0, 0), new Vector2(300, 100),
                TextAnchor.UpperLeft, new Vector2(20, -20));

            var healthBg = CreateBarBG(barsGroup.transform, "HealthBar", new Vector2(0, 0), 250, 18);
            var healthFill = CreateBarFill(healthBg.transform, "HealthFill", new Color(0.8f, 0.15f, 0.1f));
            var healthSmooth = CreateBarFill(healthBg.transform, "HealthSmooth", new Color(0.9f, 0.3f, 0.2f, 0.5f));
            var healthLabel = CreateLabel(healthBg.transform, "HP", new Vector2(-130, 0), 14);

            var staminaBg = CreateBarBG(barsGroup.transform, "StaminaBar", new Vector2(0, -25), 220, 14);
            var staminaFill = CreateBarFill(staminaBg.transform, "StaminaFill", new Color(0.85f, 0.7f, 0.15f));
            var staminaLabel = CreateLabel(staminaBg.transform, "ST", new Vector2(-118, 0), 12);

            var oxygenGroup = new GameObject("OxygenGroup");
            oxygenGroup.transform.SetParent(barsGroup.transform, false);
            var oxygenRt = oxygenGroup.AddComponent<RectTransform>();
            oxygenRt.anchoredPosition = new Vector2(0, -48);
            oxygenRt.sizeDelta = new Vector2(200, 14);
            var oxygenBg = CreateBarBG(oxygenGroup.transform, "OxygenBar", Vector2.zero, 200, 12);
            var oxygenFill = CreateBarFill(oxygenBg.transform, "OxygenFill", new Color(0.2f, 0.5f, 0.9f));
            var oxygenLabel = CreateLabel(oxygenBg.transform, "O2", new Vector2(-108, 0), 11);

            // --- TOP RIGHT: Minimap, Score, Timer ---
            var topRight = CreateUIPanel(canvasObj.transform, "TopRight",
                new Vector2(0, 0), new Vector2(220, 250),
                TextAnchor.UpperRight, new Vector2(-20, -20));

            // Minimap placeholder
            var minimapBg = CreateUIRect(topRight.transform, "MinimapBG", Vector2.zero, new Vector2(180, 180));
            minimapBg.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.2f, 0.8f);

            var minimapImg = CreateUIRect(minimapBg.transform, "MinimapImage", Vector2.zero, new Vector2(170, 170));
            minimapImg.gameObject.AddComponent<RawImage>().color = new Color(0.2f, 0.3f, 0.2f);

            var scoreText = CreateLabel(topRight.transform, "Score: 0", new Vector2(0, -195), 16);
            var timerText = CreateLabel(topRight.transform, "00:00", new Vector2(0, -215), 20);
            timerText.fontStyle = FontStyles.Bold;

            // --- TOP CENTER: Phase Indicator ---
            var phaseGroup = new GameObject("PhaseGroup");
            phaseGroup.transform.SetParent(canvasObj.transform, false);
            var phaseRt = phaseGroup.AddComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0.5f, 1);
            phaseRt.anchorMax = new Vector2(0.5f, 1);
            phaseRt.pivot = new Vector2(0.5f, 1);
            phaseRt.anchoredPosition = new Vector2(0, -15);
            phaseRt.sizeDelta = new Vector2(300, 50);
            var phaseText = CreateLabel(phaseGroup.transform, "WARNING", Vector2.zero, 32);
            phaseText.fontStyle = FontStyles.Bold;
            phaseText.color = new Color(1f, 0.8f, 0f);
            phaseText.alignment = TextAlignmentOptions.Center;

            // --- BOTTOM LEFT: Mission Objective ---
            var missionGroup = new GameObject("MissionGroup");
            missionGroup.transform.SetParent(canvasObj.transform, false);
            var missionRt = missionGroup.AddComponent<RectTransform>();
            missionRt.anchorMin = new Vector2(0, 0);
            missionRt.anchorMax = new Vector2(0, 0);
            missionRt.pivot = new Vector2(0, 0);
            missionRt.anchoredPosition = new Vector2(20, 80);
            missionRt.sizeDelta = new Vector2(400, 80);

            var missionBg = missionGroup.AddComponent<Image>();
            missionBg.color = new Color(0, 0, 0, 0.4f);

            var missionName = CreateLabel(missionGroup.transform, "Mission Name", new Vector2(10, 20), 18);
            missionName.fontStyle = FontStyles.Bold;
            missionName.alignment = TextAlignmentOptions.Left;
            var missionDesc = CreateLabel(missionGroup.transform, "Mission description...", new Vector2(10, -8), 14);
            missionDesc.alignment = TextAlignmentOptions.Left;
            missionDesc.color = new Color(0.7f, 0.7f, 0.7f);

            // --- BOTTOM CENTER: Rescue Counter ---
            var rescueText = CreateLabel(canvasObj.transform, "Rescued: 0/30", new Vector2(0, 20), 22);
            var rescueRt = rescueText.GetComponent<RectTransform>();
            rescueRt.anchorMin = new Vector2(0.5f, 0);
            rescueRt.anchorMax = new Vector2(0.5f, 0);
            rescueRt.pivot = new Vector2(0.5f, 0);
            rescueRt.anchoredPosition = new Vector2(0, 20);
            rescueText.fontStyle = FontStyles.Bold;
            rescueText.alignment = TextAlignmentOptions.Center;

            // --- CENTER: Interaction Prompt ---
            var promptGroup = new GameObject("InteractionPrompt");
            promptGroup.transform.SetParent(canvasObj.transform, false);
            var promptRt = promptGroup.AddComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.5f, 0.3f);
            promptRt.anchorMax = new Vector2(0.5f, 0.3f);
            promptRt.pivot = new Vector2(0.5f, 0.5f);
            promptRt.anchoredPosition = Vector2.zero;
            promptRt.sizeDelta = new Vector2(300, 50);

            var promptBg = promptGroup.AddComponent<Image>();
            promptBg.color = new Color(0, 0, 0, 0.6f);

            var promptText = CreateLabel(promptGroup.transform, "Press E to Interact", Vector2.zero, 18);
            promptText.alignment = TextAlignmentOptions.Center;

            // Hold progress bar inside prompt
            var holdBg = CreateUIRect(promptGroup.transform, "HoldBG", new Vector2(0, -20), new Vector2(200, 6));
            holdBg.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            var holdFill = CreateUIRect(holdBg.transform, "HoldFill", Vector2.zero, new Vector2(200, 6));
            var holdFillImg = holdFill.gameObject.AddComponent<Image>();
            holdFillImg.color = new Color(0.2f, 0.8f, 0.3f);
            holdFillImg.type = Image.Type.Filled;
            holdFillImg.fillMethod = Image.FillMethod.Horizontal;
            holdFillImg.fillAmount = 0;

            // Low health vignette (fullscreen red overlay)
            var vignette = CreateUIRect(canvasObj.transform, "LowHealthVignette", Vector2.zero, Vector2.zero);
            var vigRt = vignette.GetComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero;
            vigRt.offsetMax = Vector2.zero;
            var vigImg = vignette.gameObject.AddComponent<Image>();
            vigImg.color = new Color(0.8f, 0, 0, 0);
            vigImg.raycastTarget = false;

            // --- Wire up HUDManager ---
            var hudSO = new SerializedObject(hudMgr);
            SetProp(hudSO, "healthBarFill", healthFill.GetComponent<Image>());
            SetProp(hudSO, "healthBarSmooth", healthSmooth.GetComponent<Image>());
            SetProp(hudSO, "staminaBarFill", staminaFill.GetComponent<Image>());
            SetProp(hudSO, "oxygenBarFill", oxygenFill.GetComponent<Image>());
            SetProp(hudSO, "oxygenBarGroup", oxygenGroup);
            SetProp(hudSO, "rescueCounterText", rescueText);
            SetProp(hudSO, "interactionPromptGroup", promptGroup);
            SetProp(hudSO, "interactionPromptText", promptText);
            SetProp(hudSO, "holdProgressFill", holdFillImg);
            SetProp(hudSO, "phaseIndicatorText", phaseText);
            SetProp(hudSO, "phaseIndicatorGroup", phaseGroup);
            SetProp(hudSO, "missionNameText", missionName);
            SetProp(hudSO, "missionDescriptionText", missionDesc);
            SetProp(hudSO, "missionObjectiveGroup", missionGroup);
            SetProp(hudSO, "scoreText", scoreText);
            SetProp(hudSO, "timerText", timerText);
            SetProp(hudSO, "lowHealthVignette", vigImg);
            hudSO.ApplyModifiedProperties();

            // Add pause menu
            canvasObj.AddComponent<UI.PauseMenu>();
        }

        static void SetProp(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        static GameObject CreateUIPanel(Transform parent, string name, Vector2 pos, Vector2 size, TextAnchor anchor, Vector2 offset)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();

            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    break;
                case TextAnchor.UpperRight:
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    break;
            }

            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            return obj;
        }

        static RectTransform CreateUIRect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static GameObject CreateBarBG(Transform parent, string name, Vector2 pos, float width, float height)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, height);
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            return obj;
        }

        static GameObject CreateBarFill(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            return obj;
        }

        static TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 pos, float fontSize)
        {
            var obj = new GameObject(text.Length > 20 ? text.Substring(0, 20) : text);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 40);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }
    }
#endif
}
