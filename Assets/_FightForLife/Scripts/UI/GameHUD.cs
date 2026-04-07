using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using FightForLife.Core;
using FightForLife.Player;
using FightForLife.NPC;
using FightForLife.Disaster;

namespace FightForLife.UI
{
    /// <summary>
    /// Complete self-contained HUD that programmatically creates all UI elements.
    /// Attach to an empty GameObject in the scene — no prefabs or manual setup required.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        // ══════════════════════════════ COLORS ══════════════════════════════
        private static readonly Color COL_HEALTH      = new Color(1f, 0.267f, 0.267f);       // #FF4444
        private static readonly Color COL_HEALTH_BG   = new Color(0.4f, 0.1f, 0.1f);
        private static readonly Color COL_STAMINA     = new Color(1f, 0.667f, 0f);            // #FFAA00
        private static readonly Color COL_STAMINA_BG  = new Color(0.4f, 0.25f, 0f);
        private static readonly Color COL_OXYGEN      = new Color(0f, 0.8f, 1f);              // #00CCFF
        private static readonly Color COL_OXYGEN_BG   = new Color(0f, 0.25f, 0.4f);
        private static readonly Color COL_SCORE       = new Color(1f, 0.843f, 0f);            // #FFD700
        private static readonly Color COL_PANEL       = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color COL_WHITE       = Color.white;
        private static readonly Color COL_PHASE_WARN  = new Color(0.2f, 0.85f, 0.2f);        // Green
        private static readonly Color COL_PHASE_DIS   = new Color(1f, 0.55f, 0f);            // Orange
        private static readonly Color COL_PHASE_ESC   = new Color(1f, 0.2f, 0.2f);           // Red
        private static readonly Color COL_VIGNETTE    = new Color(1f, 0f, 0f, 0f);

        // ══════════════════════════════ SETTINGS ════════════════════════════
        private const float BAR_WIDTH  = 240f;
        private const float BAR_HEIGHT = 22f;
        private const float BAR_SPACING = 6f;
        private const float HEALTH_SMOOTH_SPEED = 5f;
        private const float LOW_HEALTH_THRESHOLD = 0.25f;
        private const float VIGNETTE_MIN_ALPHA = 0.08f;
        private const float VIGNETTE_MAX_ALPHA = 0.45f;
        private const float VIGNETTE_PULSE_SPEED = 2f;
        private const float WARNING_BANNER_DURATION = 3f;
        private const float WARNING_FADE_DURATION = 0.6f;
        private const float MINIMAP_SIZE = 200f;
        private const float MINIMAP_CAM_HEIGHT = 80f;
        private const float MINIMAP_CAM_ORTHO = 40f;
        private const int   MINIMAP_TEX_RES = 256;
        private const float COMPASS_WIDTH = 400f;
        private const float COMPASS_HEIGHT = 30f;

        // ══════════════════════════════ REFERENCES ══════════════════════════
        private PlayerHealth playerHealth;
        private PlayerController playerController;
        private InteractionSystem interactionSystem;
        private FloodManager floodManager;
        private MissionManager missionManager;
        private ScoreManager scoreManager;
        private NPCSpawner npcSpawner;
        private Transform playerTransform;

        // ══════════════════════════════ UI ELEMENTS ═════════════════════════
        private Canvas canvas;
        private CanvasScaler scaler;

        // Status bars
        private Image healthBarFill;
        private Image healthBarSmooth;
        private TextMeshProUGUI healthText;
        private Image staminaBarFill;
        private TextMeshProUGUI staminaText;
        private Image oxygenBarFill;
        private TextMeshProUGUI oxygenText;
        private GameObject oxygenBarGroup;

        // Mission panel
        private GameObject missionPanel;
        private TextMeshProUGUI missionNameText;
        private TextMeshProUGUI missionObjectiveText;
        private TextMeshProUGUI missionTimerText;

        // Minimap
        private RawImage minimapImage;
        private UnityEngine.Camera minimapCamera;
        private RenderTexture minimapRT;

        // Compass
        private RectTransform compassContainer;
        private TextMeshProUGUI[] compassLetters;
        private RectTransform compassStrip;

        // Phase indicator
        private GameObject phasePanel;
        private TextMeshProUGUI phaseText;

        // Rescue counter
        private TextMeshProUGUI rescueText;

        // Score
        private TextMeshProUGUI scoreText;

        // Interaction prompt
        private GameObject interactionGroup;
        private TextMeshProUGUI interactionText;
        private Image interactionProgressBg;
        private Image interactionProgressFill;

        // Warning banner
        private CanvasGroup warningBannerGroup;
        private TextMeshProUGUI warningBannerText;
        private RectTransform warningBannerRect;
        private Coroutine warningCoroutine;

        // Low health vignette
        private Image vignetteImage;

        // Crosshair
        private Image crosshairDot;

        // State
        private float smoothHealthTarget;
        private float currentSmoothHealth;
        private bool isInitialized;

        // ════════════════════════════ LIFECYCLE ═════════════════════════════

        private void Awake()
        {
            CreateCanvas();
            CreateAllUI();
        }

        private void Start()
        {
            FindReferences();
            SubscribeEvents();
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized) return;

            UpdateSmoothHealthBar();
            UpdateOxygenVisibility();
            UpdateInteractionPrompt();
            UpdateRescueCounter();
            UpdatePhaseIndicator();
            UpdateMissionPanel();
            UpdateScoreDisplay();
            UpdateLowHealthWarning();
            UpdateCompass();
            UpdateMinimap();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (minimapRT != null)
            {
                minimapRT.Release();
                Destroy(minimapRT);
            }
            if (minimapCamera != null)
                Destroy(minimapCamera.gameObject);
        }

        // ════════════════════════════ SETUP ═════════════════════════════════

        private void FindReferences()
        {
            playerHealth      = FindAnyObjectByType<PlayerHealth>();
            playerController  = FindAnyObjectByType<PlayerController>();
            interactionSystem = FindAnyObjectByType<InteractionSystem>();
            floodManager      = FloodManager.Instance ?? FindAnyObjectByType<FloodManager>();
            missionManager    = MissionManager.Instance ?? FindAnyObjectByType<MissionManager>();
            scoreManager      = ScoreManager.Instance ?? FindAnyObjectByType<ScoreManager>();
            npcSpawner        = NPCSpawner.Instance ?? FindAnyObjectByType<NPCSpawner>();

            if (playerHealth != null)
            {
                playerTransform = playerHealth.transform;
                smoothHealthTarget = playerHealth.HealthPercent;
                currentSmoothHealth = smoothHealthTarget;
                UpdateHealthBar(playerHealth.HealthPercent);
                UpdateStaminaBar(playerHealth.StaminaPercent);
                UpdateOxygenBar(playerHealth.OxygenPercent);
            }

            SetupMinimapCamera();
        }

        private void SubscribeEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged  += OnHealthChanged;
                playerHealth.OnStaminaChanged += OnStaminaChanged;
                playerHealth.OnOxygenChanged  += OnOxygenChanged;
                playerHealth.OnDeath          += OnPlayerDeath;
            }

            if (floodManager != null)
                floodManager.OnPhaseChanged += OnPhaseChanged;

            if (missionManager != null)
            {
                missionManager.OnMissionStarted   += OnMissionStarted;
                missionManager.OnMissionCompleted  += OnMissionCompleted;
                missionManager.OnMissionFailed     += OnMissionFailed;
            }
        }

        private void UnsubscribeEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged  -= OnHealthChanged;
                playerHealth.OnStaminaChanged -= OnStaminaChanged;
                playerHealth.OnOxygenChanged  -= OnOxygenChanged;
                playerHealth.OnDeath          -= OnPlayerDeath;
            }

            if (floodManager != null)
                floodManager.OnPhaseChanged -= OnPhaseChanged;

            if (missionManager != null)
            {
                missionManager.OnMissionStarted   -= OnMissionStarted;
                missionManager.OnMissionCompleted  -= OnMissionCompleted;
                missionManager.OnMissionFailed     -= OnMissionFailed;
            }
        }

        // ═══════════════════════ CANVAS CREATION ═══════════════════════════

        private void CreateCanvas()
        {
            // Canvas
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Canvas Scaler
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Graphic Raycaster
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // ═══════════════════════ UI CREATION ═══════════════════════════════

        private void CreateAllUI()
        {
            CreateStatusBars();
            CreateMissionPanel();
            CreateMinimap();
            CreateCompass();
            CreatePhaseIndicator();
            CreateRescueCounter();
            CreateScoreDisplay();
            CreateInteractionPrompt();
            CreateWarningBanner();
            CreateLowHealthVignette();
            CreateCrosshair();
        }

        // ─────────────────── STATUS BARS (top-left) ────────────────────────

        private void CreateStatusBars()
        {
            float yOffset = -20f;

            // Health bar
            var healthGroup = CreateBarGroup("HealthBar", new Vector2(20f, yOffset),
                "\u2665 HP", COL_HEALTH, COL_HEALTH_BG, out healthBarFill, out healthText);

            // Smooth trailing bar behind the fill
            healthBarSmooth = CreateSmoothTrailBar(healthGroup, COL_HEALTH);

            yOffset -= (BAR_HEIGHT + BAR_SPACING + 20f);

            // Stamina bar
            CreateBarGroup("StaminaBar", new Vector2(20f, yOffset),
                "\u26A1 STA", COL_STAMINA, COL_STAMINA_BG, out staminaBarFill, out staminaText);

            yOffset -= (BAR_HEIGHT + BAR_SPACING + 20f);

            // Oxygen bar (hidden by default)
            oxygenBarGroup = CreateBarGroup("OxygenBar", new Vector2(20f, yOffset),
                "\u25CF O2", COL_OXYGEN, COL_OXYGEN_BG, out oxygenBarFill, out oxygenText).gameObject;
            oxygenBarGroup.SetActive(false);
        }

        private RectTransform CreateBarGroup(string name, Vector2 position, string label,
            Color fillColor, Color bgColor, out Image fillImage, out TextMeshProUGUI valueText)
        {
            // Container panel
            float panelWidth = BAR_WIDTH + 80f;
            float panelHeight = BAR_HEIGHT + 16f;
            var panel = CreatePanel(name + "Panel", canvas.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(position.x, position.y),
                new Vector2(panelWidth, panelHeight), COL_PANEL, 6f);

            // Label text
            var labelTMP = CreateTMP(name + "Label", panel,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(8f, 0f), new Vector2(50f, panelHeight),
                label, 12f, COL_WHITE, TextAlignmentOptions.Left);

            // Bar background
            float barX = 60f;
            var barBg = CreateImage(name + "Bg", panel,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(barX, 0f), new Vector2(BAR_WIDTH, BAR_HEIGHT), bgColor);
            AddRoundedCorners(barBg, 4f);

            // Bar fill
            var fill = CreateImage(name + "Fill", barBg.rectTransform,
                Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, fillColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            fillImage = fill;

            // Value text (right side)
            valueText = CreateTMP(name + "Value", panel,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-8f, 0f), new Vector2(60f, panelHeight),
                "100%", 11f, COL_WHITE, TextAlignmentOptions.Right);

            return panel;
        }

        private Image CreateSmoothTrailBar(RectTransform parent, Color barColor)
        {
            // Find the bar background (child index 1 = Bg)
            var barBg = parent.GetChild(1) as RectTransform;
            if (barBg == null) return null;

            Color trailColor = new Color(barColor.r, barColor.g, barColor.b, 0.4f);
            var trail = CreateImage("SmoothTrail", barBg,
                Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, trailColor);
            trail.rectTransform.anchorMin = Vector2.zero;
            trail.rectTransform.anchorMax = new Vector2(1f, 1f);
            trail.rectTransform.offsetMin = Vector2.zero;
            trail.rectTransform.offsetMax = Vector2.zero;
            trail.type = Image.Type.Filled;
            trail.fillMethod = Image.FillMethod.Horizontal;
            trail.fillOrigin = 0;
            trail.fillAmount = 1f;

            // Make sure trail renders behind fill
            trail.rectTransform.SetAsFirstSibling();

            return trail;
        }

        // ─────────────────── MISSION PANEL (top-right) ─────────────────────

        private void CreateMissionPanel()
        {
            float panelW = 320f;
            float panelH = 100f;

            var panel = CreatePanel("MissionPanel", canvas.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -70f),
                new Vector2(panelW, panelH), COL_PANEL, 8f);
            missionPanel = panel.gameObject;

            // Mission name
            missionNameText = CreateTMP("MissionName", panel,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -8f), new Vector2(-12f, -8f),
                "MISSION", 14f, COL_WHITE, TextAlignmentOptions.TopLeft);
            missionNameText.rectTransform.anchorMin = new Vector2(0f, 1f);
            missionNameText.rectTransform.anchorMax = new Vector2(1f, 1f);
            missionNameText.rectTransform.offsetMin = new Vector2(12f, -32f);
            missionNameText.rectTransform.offsetMax = new Vector2(-12f, -8f);
            missionNameText.fontStyle = FontStyles.Bold;

            // Objective text
            missionObjectiveText = CreateTMP("MissionObj", panel,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                "Objective", 12f, new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.TopLeft);
            missionObjectiveText.rectTransform.anchorMin = new Vector2(0f, 0f);
            missionObjectiveText.rectTransform.anchorMax = new Vector2(1f, 1f);
            missionObjectiveText.rectTransform.offsetMin = new Vector2(12f, 8f);
            missionObjectiveText.rectTransform.offsetMax = new Vector2(-12f, -36f);

            // Timer text
            missionTimerText = CreateTMP("MissionTimer", panel,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -8f), new Vector2(80f, 24f),
                "", 12f, COL_SCORE, TextAlignmentOptions.Right);

            missionPanel.SetActive(false);
        }

        // ─────────────────── MINIMAP (bottom-right) ────────────────────────

        private void CreateMinimap()
        {
            float margin = 20f;

            // Border frame
            var border = CreatePanel("MinimapBorder", canvas.transform,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-margin, margin),
                new Vector2(MINIMAP_SIZE + 6f, MINIMAP_SIZE + 6f),
                new Color(0.15f, 0.15f, 0.15f, 0.9f), 4f);

            // Inner frame
            var inner = CreatePanel("MinimapInner", border,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(MINIMAP_SIZE, MINIMAP_SIZE),
                Color.black, 2f);

            // RawImage for render texture
            var mmGO = new GameObject("MinimapImage");
            mmGO.transform.SetParent(inner, false);
            minimapImage = mmGO.AddComponent<RawImage>();
            var mmRect = minimapImage.rectTransform;
            mmRect.anchorMin = Vector2.zero;
            mmRect.anchorMax = Vector2.one;
            mmRect.offsetMin = Vector2.zero;
            mmRect.offsetMax = Vector2.zero;

            // Create render texture
            minimapRT = new RenderTexture(MINIMAP_TEX_RES, MINIMAP_TEX_RES, 16, RenderTextureFormat.ARGB32);
            minimapRT.Create();
            minimapImage.texture = minimapRT;

            // Player indicator dot in center of minimap
            var playerDot = CreateImage("PlayerDot", inner,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(8f, 8f), COL_HEALTH);
        }

        private void SetupMinimapCamera()
        {
            if (playerTransform == null) return;

            var camGO = new GameObject("MinimapCamera");
            minimapCamera = camGO.AddComponent<UnityEngine.Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = MINIMAP_CAM_ORTHO;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 1f);
            minimapCamera.cullingMask = ~0; // Render everything; adjust layers as needed
            minimapCamera.targetTexture = minimapRT;
            minimapCamera.depth = -10;

            // Position above player looking down
            camGO.transform.position = playerTransform.position + Vector3.up * MINIMAP_CAM_HEIGHT;
            camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Ensure the minimap camera does not render UI
            minimapCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
        }

        // ─────────────────── COMPASS (top-center) ──────────────────────────

        private void CreateCompass()
        {
            // Compass background
            var bg = CreatePanel("CompassBg", canvas.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -15f),
                new Vector2(COMPASS_WIDTH, COMPASS_HEIGHT + 8f),
                COL_PANEL, 4f);

            compassContainer = bg;

            // Clip area
            var clipGO = new GameObject("CompassClip");
            clipGO.transform.SetParent(bg, false);
            var clipRect = clipGO.AddComponent<RectTransform>();
            clipRect.anchorMin = Vector2.zero;
            clipRect.anchorMax = Vector2.one;
            clipRect.offsetMin = new Vector2(4f, 2f);
            clipRect.offsetMax = new Vector2(-4f, -2f);
            var clipMask = clipGO.AddComponent<RectMask2D>();

            // Compass strip (wide, slides left/right)
            var stripGO = new GameObject("CompassStrip");
            stripGO.transform.SetParent(clipGO.transform, false);
            compassStrip = stripGO.AddComponent<RectTransform>();
            compassStrip.anchorMin = new Vector2(0.5f, 0f);
            compassStrip.anchorMax = new Vector2(0.5f, 1f);
            compassStrip.sizeDelta = new Vector2(COMPASS_WIDTH * 4f, 0f);
            compassStrip.anchoredPosition = Vector2.zero;

            // Place N, E, S, W cardinal labels on the strip
            // Strip spans 360 degrees across its width
            float stripW = COMPASS_WIDTH * 4f;
            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            compassLetters = new TextMeshProUGUI[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
            {
                float frac = i / (float)dirs.Length;
                float xPos = (frac - 0.5f) * stripW;
                bool isCardinal = (i % 2 == 0);
                float fontSize = isCardinal ? 16f : 11f;
                Color col = isCardinal ? COL_WHITE : new Color(0.6f, 0.6f, 0.6f);

                compassLetters[i] = CreateTMP("Dir_" + dirs[i], compassStrip,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(xPos, 0f), new Vector2(40f, COMPASS_HEIGHT),
                    dirs[i], fontSize, col, TextAlignmentOptions.Center);
                if (isCardinal)
                    compassLetters[i].fontStyle = FontStyles.Bold;
            }

            // Center tick mark
            var tick = CreateImage("CompassTick", bg,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, -2f), new Vector2(2f, 10f), COL_SCORE);
        }

        // ─────────────────── PHASE INDICATOR (below compass) ───────────────

        private void CreatePhaseIndicator()
        {
            var panel = CreatePanel("PhasePanel", canvas.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -55f),
                new Vector2(200f, 30f), COL_PANEL, 6f);
            phasePanel = panel.gameObject;

            phaseText = CreateTMP("PhaseText", panel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(190f, 26f),
                "WARNING", 14f, COL_PHASE_WARN, TextAlignmentOptions.Center);
            phaseText.fontStyle = FontStyles.Bold;

            phasePanel.SetActive(false);
        }

        // ─────────────────── RESCUE COUNTER (left side) ────────────────────

        private void CreateRescueCounter()
        {
            var panel = CreatePanel("RescuePanel", canvas.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(20f, 0f),
                new Vector2(200f, 36f), COL_PANEL, 6f);

            rescueText = CreateTMP("RescueText", panel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(190f, 30f),
                "\u263A Civilians Saved: 0/0", 13f, COL_WHITE, TextAlignmentOptions.Center);
        }

        // ─────────────────── SCORE DISPLAY (top-right) ─────────────────────

        private void CreateScoreDisplay()
        {
            var panel = CreatePanel("ScorePanel", canvas.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -20f),
                new Vector2(180f, 36f), COL_PANEL, 6f);

            scoreText = CreateTMP("ScoreText", panel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(170f, 30f),
                "\u2605 Score: 0", 15f, COL_SCORE, TextAlignmentOptions.Center);
            scoreText.fontStyle = FontStyles.Bold;
        }

        // ─────────────────── INTERACTION PROMPT (bottom-center) ────────────

        private void CreateInteractionPrompt()
        {
            var panel = CreatePanel("InteractionPanel", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 80f),
                new Vector2(300f, 60f), COL_PANEL, 8f);
            interactionGroup = panel.gameObject;

            interactionText = CreateTMP("InteractionText", panel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 6f), new Vector2(280f, 30f),
                "Press E to interact", 14f, COL_WHITE, TextAlignmentOptions.Center);

            // Progress bar background
            interactionProgressBg = CreateImage("PromptProgressBg", panel,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 10f), new Vector2(260f, 8f),
                new Color(0.2f, 0.2f, 0.2f, 0.8f));
            AddRoundedCorners(interactionProgressBg, 4f);

            // Progress bar fill
            interactionProgressFill = CreateImage("PromptProgressFill", interactionProgressBg.rectTransform,
                Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, COL_SCORE);
            interactionProgressFill.rectTransform.anchorMin = Vector2.zero;
            interactionProgressFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            interactionProgressFill.rectTransform.offsetMin = Vector2.zero;
            interactionProgressFill.rectTransform.offsetMax = Vector2.zero;
            interactionProgressFill.type = Image.Type.Filled;
            interactionProgressFill.fillMethod = Image.FillMethod.Horizontal;
            interactionProgressFill.fillOrigin = 0;
            interactionProgressFill.fillAmount = 0f;

            interactionProgressBg.gameObject.SetActive(false);
            interactionGroup.SetActive(false);
        }

        // ─────────────────── WARNING BANNER (center) ───────────────────────

        private void CreateWarningBanner()
        {
            var panelGO = new GameObject("WarningBanner");
            panelGO.transform.SetParent(canvas.transform, false);

            warningBannerGroup = panelGO.AddComponent<CanvasGroup>();
            warningBannerGroup.alpha = 0f;
            warningBannerGroup.blocksRaycasts = false;
            warningBannerGroup.interactable = false;

            warningBannerRect = panelGO.GetComponent<RectTransform>();
            warningBannerRect.anchorMin = new Vector2(0f, 0.5f);
            warningBannerRect.anchorMax = new Vector2(1f, 0.5f);
            warningBannerRect.anchoredPosition = new Vector2(0f, 100f);
            warningBannerRect.sizeDelta = new Vector2(0f, 80f);

            // Background
            var bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);

            // Text
            warningBannerText = CreateTMP("WarningText", warningBannerRect,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1200f, 60f),
                "", 28f, COL_WHITE, TextAlignmentOptions.Center);
            warningBannerText.fontStyle = FontStyles.Bold;

            panelGO.SetActive(false);
        }

        // ─────────────────── LOW HEALTH VIGNETTE ───────────────────────────

        private void CreateLowHealthVignette()
        {
            var vigGO = new GameObject("LowHealthVignette");
            vigGO.transform.SetParent(canvas.transform, false);

            vignetteImage = vigGO.AddComponent<Image>();
            vignetteImage.color = COL_VIGNETTE;
            vignetteImage.raycastTarget = false;

            var vigRect = vignetteImage.rectTransform;
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;

            // Create a simple gradient texture for edge vignette
            var tex = CreateVignetteTexture(256);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(tex.width * 0.3f, tex.height * 0.3f, tex.width * 0.3f, tex.height * 0.3f));
            vignetteImage.sprite = sprite;
            vignetteImage.type = Image.Type.Sliced;
        }

        private Texture2D CreateVignetteTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((dist - 0.4f) / 0.6f);
                    alpha = alpha * alpha; // Quadratic falloff
                    tex.SetPixel(x, y, new Color(1f, 0f, 0f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        // ─────────────────── CROSSHAIR ─────────────────────────────────────

        private void CreateCrosshair()
        {
            crosshairDot = CreateImage("Crosshair", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(4f, 4f),
                new Color(1f, 1f, 1f, 0.7f));
        }

        // ═══════════════════════ UPDATE METHODS ════════════════════════════

        // --- Health / Stamina / Oxygen ---

        private void OnHealthChanged(float percent)
        {
            smoothHealthTarget = percent;
            UpdateHealthBar(percent);
        }

        private void OnStaminaChanged(float percent)
        {
            UpdateStaminaBar(percent);
        }

        private void OnOxygenChanged(float percent)
        {
            UpdateOxygenBar(percent);
        }

        private void UpdateHealthBar(float percent)
        {
            if (healthBarFill != null)
                healthBarFill.fillAmount = percent;
            if (healthText != null && playerHealth != null)
                healthText.text = $"{Mathf.CeilToInt(playerHealth.Health)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}";
        }

        private void UpdateSmoothHealthBar()
        {
            if (healthBarSmooth == null) return;
            currentSmoothHealth = Mathf.Lerp(currentSmoothHealth, smoothHealthTarget,
                Time.deltaTime * HEALTH_SMOOTH_SPEED);
            healthBarSmooth.fillAmount = currentSmoothHealth;
        }

        private void UpdateStaminaBar(float percent)
        {
            if (staminaBarFill != null)
                staminaBarFill.fillAmount = percent;
            if (staminaText != null && playerHealth != null)
                staminaText.text = $"{Mathf.CeilToInt(playerHealth.Stamina)}/{Mathf.CeilToInt(playerHealth.MaxStamina)}";
        }

        private void UpdateOxygenBar(float percent)
        {
            if (oxygenBarFill != null)
                oxygenBarFill.fillAmount = percent;
            if (oxygenText != null && playerHealth != null)
                oxygenText.text = $"{Mathf.CeilToInt(playerHealth.Oxygen)}/{Mathf.CeilToInt(playerHealth.MaxOxygen)}";
        }

        private void UpdateOxygenVisibility()
        {
            if (oxygenBarGroup == null || playerHealth == null) return;
            bool show = playerHealth.OxygenPercent < 1f;
            if (oxygenBarGroup.activeSelf != show)
                oxygenBarGroup.SetActive(show);
        }

        // --- Interaction Prompt ---

        private void UpdateInteractionPrompt()
        {
            if (interactionSystem == null || interactionGroup == null) return;

            string prompt = interactionSystem.CurrentPrompt;
            bool hasPrompt = !string.IsNullOrEmpty(prompt);

            if (interactionGroup.activeSelf != hasPrompt)
                interactionGroup.SetActive(hasPrompt);

            if (hasPrompt)
            {
                if (interactionText != null)
                    interactionText.text = prompt;

                if (interactionProgressBg != null)
                {
                    bool isHolding = interactionSystem.IsHolding;
                    if (interactionProgressBg.gameObject.activeSelf != isHolding)
                        interactionProgressBg.gameObject.SetActive(isHolding);

                    if (isHolding && interactionProgressFill != null)
                        interactionProgressFill.fillAmount = interactionSystem.HoldProgress;
                }
            }
        }

        // --- Rescue Counter ---

        private void UpdateRescueCounter()
        {
            if (rescueText == null || npcSpawner == null) return;
            rescueText.text = $"\u263A Civilians Saved: {npcSpawner.GetRescuedCount()}/{npcSpawner.GetTotalCount()}";
        }

        // --- Phase Indicator ---

        private void UpdatePhaseIndicator()
        {
            if (phaseText == null || floodManager == null) return;

            bool showPhase = floodManager.CurrentPhase != FloodPhase.PreDisaster;
            if (phasePanel != null && phasePanel.activeSelf != showPhase)
                phasePanel.SetActive(showPhase);

            if (!showPhase) return;

            switch (floodManager.CurrentPhase)
            {
                case FloodPhase.Warning:
                    phaseText.text = "\u26A0 WARNING";
                    phaseText.color = COL_PHASE_WARN;
                    break;
                case FloodPhase.DisasterStrikes:
                    phaseText.text = "\u26A0 DISASTER";
                    phaseText.color = COL_PHASE_DIS;
                    break;
                case FloodPhase.Escape:
                    phaseText.text = "\u26A0 ESCAPE";
                    phaseText.color = COL_PHASE_ESC;
                    break;
            }
        }

        // --- Mission Panel ---

        private void UpdateMissionPanel()
        {
            if (missionManager == null) return;

            MissionData active = missionManager.ActiveMission;
            bool hasMission = active != null && active.status == MissionStatus.Active;

            // Also check for any active missions if primary active is null
            if (!hasMission)
            {
                var allActive = missionManager.GetActiveMissions();
                if (allActive.Count > 0)
                {
                    active = allActive[0];
                    hasMission = true;
                }
            }

            if (missionPanel.activeSelf != hasMission)
                missionPanel.SetActive(hasMission);

            if (!hasMission) return;

            // Mission name
            var allMissions = missionManager.GetActiveMissions();
            string nameStr = active.missionName;
            if (allMissions.Count > 1)
                nameStr += $" (+{allMissions.Count - 1} more)";
            missionNameText.text = nameStr;

            // Objective
            var obj = active.GetActiveObjective();
            if (obj != null)
            {
                missionObjectiveText.text = obj.requiredCount > 1
                    ? $"{obj.description} {obj.currentCount}/{obj.requiredCount}"
                    : obj.description;
            }
            else
            {
                missionObjectiveText.text = active.description;
            }

            // Timer
            float remaining = missionManager.GetMissionTimeRemaining(active);
            if (remaining > 0f)
            {
                int min = Mathf.FloorToInt(remaining / 60f);
                int sec = Mathf.FloorToInt(remaining % 60f);
                missionTimerText.text = $"{min:00}:{sec:00}";
                missionTimerText.color = remaining < 30f ? COL_HEALTH : COL_SCORE;
                missionTimerText.gameObject.SetActive(true);
            }
            else
            {
                missionTimerText.gameObject.SetActive(false);
            }
        }

        // --- Score ---

        private void UpdateScoreDisplay()
        {
            if (scoreText == null || scoreManager == null) return;
            scoreText.text = $"\u2605 Score: {scoreManager.CurrentScore}";
        }

        // --- Low Health Warning ---

        private void UpdateLowHealthWarning()
        {
            if (vignetteImage == null || playerHealth == null) return;

            if (playerHealth.HealthPercent < LOW_HEALTH_THRESHOLD && playerHealth.IsAlive)
            {
                float pulse = Mathf.Lerp(VIGNETTE_MIN_ALPHA, VIGNETTE_MAX_ALPHA,
                    (Mathf.Sin(Time.time * VIGNETTE_PULSE_SPEED * Mathf.PI) + 1f) * 0.5f);
                float intensity = 1f - (playerHealth.HealthPercent / LOW_HEALTH_THRESHOLD);
                SetVignetteAlpha(pulse * intensity);
            }
            else
            {
                SetVignetteAlpha(0f);
            }
        }

        private void SetVignetteAlpha(float alpha)
        {
            if (vignetteImage == null) return;
            Color c = vignetteImage.color;
            c.a = alpha;
            vignetteImage.color = c;
        }

        // --- Compass ---

        private void UpdateCompass()
        {
            if (compassStrip == null || playerTransform == null) return;

            float yRot = playerTransform.eulerAngles.y;
            // Map rotation to strip offset: 0 degrees = N centered
            float stripW = COMPASS_WIDTH * 4f;
            float offset = -(yRot / 360f) * stripW;
            compassStrip.anchoredPosition = new Vector2(offset, 0f);

            // Update waypoint markers on compass if mission active
            UpdateCompassWaypoints(yRot);
        }

        private void UpdateCompassWaypoints(float playerYRot)
        {
            if (missionManager == null || playerTransform == null) return;

            MissionData active = missionManager.ActiveMission;
            if (active == null || active.waypointPosition == Vector3.zero) return;

            Vector3 dir = (active.waypointPosition - playerTransform.position).normalized;
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float diff = Mathf.DeltaAngle(playerYRot, angle);

            // Only visible if within compass FOV
            float halfFOV = 90f;
            if (Mathf.Abs(diff) < halfFOV)
            {
                // Could add a dynamic marker here; for now the compass directions suffice
            }
        }

        // --- Minimap ---

        private void UpdateMinimap()
        {
            if (minimapCamera == null || playerTransform == null) return;

            minimapCamera.transform.position = playerTransform.position + Vector3.up * MINIMAP_CAM_HEIGHT;
            // Keep rotation looking straight down, with north up
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // ═══════════════════════ EVENT HANDLERS ════════════════════════════

        private void OnPlayerDeath()
        {
            SetVignetteAlpha(VIGNETTE_MAX_ALPHA);
        }

        private void OnPhaseChanged(FloodPhase newPhase)
        {
            string message = newPhase switch
            {
                FloodPhase.Warning        => "\u26A0 FLOOD PHASE 1: WARNING ISSUED!",
                FloodPhase.DisasterStrikes => "\u26A0 FLOOD PHASE 2: DISASTER STRIKES!",
                FloodPhase.Escape          => "\u26A0 FLOOD PHASE 3: ESCAPE NOW!",
                _                          => ""
            };

            Color col = newPhase switch
            {
                FloodPhase.Warning        => COL_PHASE_WARN,
                FloodPhase.DisasterStrikes => COL_PHASE_DIS,
                FloodPhase.Escape          => COL_PHASE_ESC,
                _                          => COL_WHITE
            };

            if (!string.IsNullOrEmpty(message))
                ShowWarningBanner(message, col);
        }

        private void OnMissionStarted(MissionData mission)
        {
            ShowWarningBanner($"NEW MISSION: {mission.missionName}", COL_SCORE);
        }

        private void OnMissionCompleted(MissionData mission)
        {
            ShowWarningBanner($"MISSION COMPLETE: {mission.missionName} (+{mission.rewardPoints}pts)",
                COL_PHASE_WARN);
        }

        private void OnMissionFailed(MissionData mission)
        {
            ShowWarningBanner($"MISSION FAILED: {mission.missionName}", COL_HEALTH);
        }

        // ═══════════════════════ WARNING BANNER ANIMATION ══════════════════

        private void ShowWarningBanner(string text, Color textColor)
        {
            if (warningCoroutine != null)
                StopCoroutine(warningCoroutine);

            warningCoroutine = StartCoroutine(WarningBannerRoutine(text, textColor));
        }

        private IEnumerator WarningBannerRoutine(string text, Color textColor)
        {
            warningBannerText.text = text;
            warningBannerText.color = textColor;
            warningBannerRect.gameObject.SetActive(true);

            float slideStart = 150f;
            float slideEnd = 100f;

            // Slide in from top + fade in
            float t = 0f;
            while (t < WARNING_FADE_DURATION)
            {
                t += Time.deltaTime;
                float p = t / WARNING_FADE_DURATION;
                float eased = 1f - (1f - p) * (1f - p); // Ease out quad
                warningBannerGroup.alpha = eased;
                warningBannerRect.anchoredPosition = new Vector2(0f,
                    Mathf.Lerp(slideStart, slideEnd, eased));
                yield return null;
            }
            warningBannerGroup.alpha = 1f;
            warningBannerRect.anchoredPosition = new Vector2(0f, slideEnd);

            // Hold
            yield return new WaitForSeconds(WARNING_BANNER_DURATION);

            // Fade out
            t = 0f;
            while (t < WARNING_FADE_DURATION)
            {
                t += Time.deltaTime;
                float p = t / WARNING_FADE_DURATION;
                warningBannerGroup.alpha = 1f - p;
                yield return null;
            }
            warningBannerGroup.alpha = 0f;
            warningBannerRect.gameObject.SetActive(false);
            warningCoroutine = null;
        }

        // ═══════════════════════ UI FACTORY HELPERS ════════════════════════

        private RectTransform CreatePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, Color color, float cornerRadius = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin; // Pivot matches anchor for intuitive positioning
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            return rect;
        }

        private Image CreateImage(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            return img;
        }

        private Image CreateImage(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, Color color)
        {
            return CreateImage(name, (Transform)parent, anchorMin, anchorMax, anchoredPos, size, color);
        }

        private TextMeshProUGUI CreateTMP(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, string text, float fontSize, Color color,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            return tmp;
        }

        private TextMeshProUGUI CreateTMP(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, string text, float fontSize, Color color,
            TextAlignmentOptions alignment)
        {
            return CreateTMP(name, (Transform)parent, anchorMin, anchorMax, anchoredPos, size,
                text, fontSize, color, alignment);
        }

        private void AddRoundedCorners(Image img, float radius)
        {
            // Unity UI doesn't have built-in rounded corners without custom shaders.
            // The panels use simple solid backgrounds which look clean at small sizes.
            // This is a placeholder if a custom shader or sprite is added later.
        }

        // ═══════════════════════ PUBLIC API ═════════════════════════════════

        /// <summary>
        /// Show a custom warning banner from external scripts.
        /// </summary>
        public void ShowWarning(string message, Color color)
        {
            ShowWarningBanner(message, color);
        }

        /// <summary>
        /// Force-refresh all HUD elements. Call after major state changes.
        /// </summary>
        public void RefreshAll()
        {
            if (playerHealth != null)
            {
                UpdateHealthBar(playerHealth.HealthPercent);
                UpdateStaminaBar(playerHealth.StaminaPercent);
                UpdateOxygenBar(playerHealth.OxygenPercent);
            }
            UpdateRescueCounter();
            UpdatePhaseIndicator();
            UpdateMissionPanel();
            UpdateScoreDisplay();
        }
    }
}
