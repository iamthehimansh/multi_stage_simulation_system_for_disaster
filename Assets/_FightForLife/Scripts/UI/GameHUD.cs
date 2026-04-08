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
    [DefaultExecutionOrder(100)]
    public class GameHUD : MonoBehaviour
    {
        /// <summary>
        /// Runtime bootstrap: removes the legacy GameHUD_Canvas/HUDManager built by
        /// VillageFloodSceneBuilder and installs this GameHUD on a fresh GameObject.
        /// Runs automatically on every scene load — no manual setup required.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            BootstrapForActiveScene();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => BootstrapForActiveScene();
        }

        private static void BootstrapForActiveScene()
        {
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            // Skip non-gameplay scenes
            if (sceneName == null) return;
            string lower = sceneName.ToLowerInvariant();
            if (lower.Contains("menu") || lower.Contains("credits") || lower.Contains("settings"))
            {
                // Also clean up any GameHUD that snuck in
                var existing = Object.FindAnyObjectByType<GameHUD>();
                if (existing != null) Object.Destroy(existing.gameObject);
                return;
            }

            // Remove legacy HUD canvas if present
            var legacy = GameObject.Find("GameHUD_Canvas");
            if (legacy != null) Object.Destroy(legacy);

            // If a GameHUD already exists in the scene, force-enable it (it ships disabled).
            var existingHud = Object.FindAnyObjectByType<GameHUD>(FindObjectsInactive.Include);
            if (existingHud != null)
            {
                existingHud.gameObject.SetActive(true);
                existingHud.enabled = true;
                return;
            }

            var go = new GameObject("GameHUD");
            go.AddComponent<GameHUD>();
        }

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
        private FightForLife.Camera.ThirdPersonCamera thirdPersonCam;

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

        // Minimap (procedural - no camera)
        private RawImage minimapImage;
        private Texture2D minimapTex;
        private Image playerArrow;
        private Color[] minimapClearPixels;
        private RectTransform minimapBorder;
        private bool minimapMaximized;

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
        private static Sprite whiteSprite;

        /// <summary>Creates a 1x1 white sprite at runtime. Required for Image.Type.Filled to work.</summary>
        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null) return whiteSprite;
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            return whiteSprite;
        }

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
            StartCoroutine(DelayedRefresh());
            isInitialized = true;
        }

        private System.Collections.IEnumerator DelayedRefresh()
        {
            yield return null;
            if (playerHealth != null)
            {
                UpdateHealthBar(playerHealth.HealthPercent);
                UpdateStaminaBar(playerHealth.StaminaPercent);
            }
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
            UpdateMinimapToggle();
        }

        // Toggle minimap maximize on M
        private void UpdateMinimapToggle()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                minimapMaximized = !minimapMaximized;
                ApplyMinimapSize();
            }
        }

        private void ApplyMinimapSize()
        {
            if (minimapBorder == null) return;
            if (minimapMaximized)
            {
                // Center large overlay
                minimapBorder.anchorMin = new Vector2(0.5f, 0.5f);
                minimapBorder.anchorMax = new Vector2(0.5f, 0.5f);
                minimapBorder.pivot = new Vector2(0.5f, 0.5f);
                minimapBorder.anchoredPosition = Vector2.zero;
                minimapBorder.sizeDelta = new Vector2(700f, 700f);
            }
            else
            {
                // Bottom-right small
                minimapBorder.anchorMin = new Vector2(1f, 0f);
                minimapBorder.anchorMax = new Vector2(1f, 0f);
                minimapBorder.pivot = new Vector2(1f, 0f);
                minimapBorder.anchoredPosition = new Vector2(-20f, 20f);
                minimapBorder.sizeDelta = new Vector2(MINIMAP_SIZE + 6f, MINIMAP_SIZE + 6f);
            }
        }

        // Bresenham-style dashed line for the minimap route marker
        private void DrawDashedLine(int x0, int y0, int x1, int y1, Color color, int dashOn, int dashOff)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int counter = 0;
            int period = dashOn + dashOff;
            int size = MINIMAP_TEX_RES;
            while (true)
            {
                if ((counter % period) < dashOn)
                {
                    if (x0 >= 0 && x0 < size && y0 >= 0 && y0 < size)
                        minimapTex.SetPixel(x0, y0, color);
                }
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
                counter++;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (minimapTex != null)
                Destroy(minimapTex);
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
            thirdPersonCam    = FindAnyObjectByType<FightForLife.Camera.ThirdPersonCamera>();

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

            // Bar fill - uses sprite + fillAmount (fillAmount requires a sprite to work)
            var fill = CreateImage(name + "Fill", barBg.rectTransform,
                Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, fillColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            fill.sprite = GetWhiteSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
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
            trail.sprite = GetWhiteSprite();
            trail.type = Image.Type.Filled;
            trail.fillMethod = Image.FillMethod.Horizontal;
            trail.fillOrigin = (int)Image.OriginHorizontal.Left;
            trail.fillAmount = 1f;
            // Make sure trail renders behind fill
            trail.rectTransform.SetAsFirstSibling();

            return trail;
        }

        // ─────────────────── MISSION PANEL (top-right) ─────────────────────

        private void CreateMissionPanel()
        {
            float panelW = 340f;
            float panelH = 110f;

            // Create panel GameObject directly so we control pivot/anchors precisely
            var panelGO = new GameObject("MissionPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = COL_PANEL;
            panelImg.raycastTarget = false;
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(panelW, panelH);
            panelRect.anchoredPosition = new Vector2(-20f, -70f);
            missionPanel = panelGO;

            // Accent strip (left side, bright color)
            var accent = new GameObject("Accent");
            accent.transform.SetParent(panelRect, false);
            var accentImg = accent.AddComponent<Image>();
            accentImg.color = COL_SCORE;
            accentImg.raycastTarget = false;
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(4f, 0f);
            accentRect.anchoredPosition = Vector2.zero;

            // Mission name (top of panel)
            var nameGO = new GameObject("MissionName");
            nameGO.transform.SetParent(panelRect, false);
            missionNameText = nameGO.AddComponent<TextMeshProUGUI>();
            missionNameText.text = "MISSION";
            missionNameText.fontSize = 16f;
            missionNameText.color = COL_WHITE;
            missionNameText.alignment = TextAlignmentOptions.TopLeft;
            missionNameText.fontStyle = FontStyles.Bold;
            missionNameText.raycastTarget = false;
            missionNameText.enableWordWrapping = false;
            missionNameText.overflowMode = TextOverflowModes.Ellipsis;
            var nameRect = missionNameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.offsetMin = new Vector2(14f, -34f);
            nameRect.offsetMax = new Vector2(-90f, -8f);

            // Objective text (below name)
            var objGO = new GameObject("MissionObj");
            objGO.transform.SetParent(panelRect, false);
            missionObjectiveText = objGO.AddComponent<TextMeshProUGUI>();
            missionObjectiveText.text = "Objective";
            missionObjectiveText.fontSize = 13f;
            missionObjectiveText.color = new Color(0.85f, 0.85f, 0.85f);
            missionObjectiveText.alignment = TextAlignmentOptions.TopLeft;
            missionObjectiveText.raycastTarget = false;
            missionObjectiveText.enableWordWrapping = true;
            missionObjectiveText.overflowMode = TextOverflowModes.Ellipsis;
            var objRect = missionObjectiveText.rectTransform;
            objRect.anchorMin = new Vector2(0f, 0f);
            objRect.anchorMax = new Vector2(1f, 1f);
            objRect.pivot = new Vector2(0.5f, 0.5f);
            objRect.offsetMin = new Vector2(14f, 8f);
            objRect.offsetMax = new Vector2(-14f, -36f);

            // Timer text (top right)
            var timerGO = new GameObject("MissionTimer");
            timerGO.transform.SetParent(panelRect, false);
            missionTimerText = timerGO.AddComponent<TextMeshProUGUI>();
            missionTimerText.text = "";
            missionTimerText.fontSize = 14f;
            missionTimerText.color = COL_SCORE;
            missionTimerText.alignment = TextAlignmentOptions.TopRight;
            missionTimerText.fontStyle = FontStyles.Bold;
            missionTimerText.raycastTarget = false;
            missionTimerText.enableWordWrapping = false;
            var timerRect = missionTimerText.rectTransform;
            timerRect.anchorMin = new Vector2(1f, 1f);
            timerRect.anchorMax = new Vector2(1f, 1f);
            timerRect.pivot = new Vector2(1f, 1f);
            timerRect.sizeDelta = new Vector2(80f, 24f);
            timerRect.anchoredPosition = new Vector2(-12f, -8f);

            // Show panel by default — UpdateMissionPanel will hide it if no mission active
            missionPanel.SetActive(true);
            missionNameText.text = "Awaiting Mission...";
            missionObjectiveText.text = "";
        }

        // ─────────────────── MINIMAP (bottom-right) ────────────────────────

        private void CreateMinimap()
        {
            float margin = 20f;
            int texSize = MINIMAP_TEX_RES;

            // Border frame
            var border = CreatePanel("MinimapBorder", canvas.transform,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-margin, margin),
                new Vector2(MINIMAP_SIZE + 6f, MINIMAP_SIZE + 6f),
                new Color(0.15f, 0.15f, 0.15f, 0.9f), 4f);
            minimapBorder = border;

            // Inner frame
            var inner = CreatePanel("MinimapInner", border,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(MINIMAP_SIZE, MINIMAP_SIZE),
                new Color(0.12f, 0.18f, 0.12f), 2f);

            // RawImage for procedural texture
            var mmGO = new GameObject("MinimapImage");
            mmGO.transform.SetParent(inner, false);
            minimapImage = mmGO.AddComponent<RawImage>();
            var mmRect = minimapImage.rectTransform;
            mmRect.anchorMin = Vector2.zero;
            mmRect.anchorMax = Vector2.one;
            mmRect.offsetMin = Vector2.zero;
            mmRect.offsetMax = Vector2.zero;

            // Create procedural texture
            minimapTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            minimapTex.filterMode = FilterMode.Point;
            minimapImage.texture = minimapTex;

            // Allocate clear pixel buffer (will be re-sampled per frame from player position)
            minimapClearPixels = new Color[texSize * texSize];
            Color initBg = new Color(0.2f, 0.3f, 0.15f);
            for (int i = 0; i < minimapClearPixels.Length; i++)
                minimapClearPixels[i] = initBg;
            minimapTex.SetPixels(minimapClearPixels);
            minimapTex.Apply();

            // Player arrow in center
            playerArrow = CreateImage("PlayerArrow", inner,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(10f, 14f), Color.white);
            playerArrow.sprite = GetWhiteSprite();

            // N/S/E/W labels on minimap edges
            CreateTMP("MM_N", inner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -4f), new Vector2(20, 14), "N", 10f, Color.white, TextAlignmentOptions.Center);
            CreateTMP("MM_S", inner, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 4f), new Vector2(20, 14), "S", 10f, Color.white, TextAlignmentOptions.Center);
            CreateTMP("MM_E", inner, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-4f, 0), new Vector2(20, 14), "E", 10f, Color.white, TextAlignmentOptions.Center);
            CreateTMP("MM_W", inner, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 0), new Vector2(20, 14), "W", 10f, Color.white, TextAlignmentOptions.Center);
        }

        private void SetupMinimapCamera()
        {
            // No camera needed - using procedural texture minimap
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

            // Use the bg directly as the label parent (no clip mask — alpha fade handles edges)
            compassStrip = bg;

            // Create 8 direction labels parented directly to bg
            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            compassLetters = new TextMeshProUGUI[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
            {
                bool isCardinal = (i % 2 == 0);
                float fontSize = isCardinal ? 18f : 12f;
                Color col = isCardinal ? COL_WHITE : new Color(0.85f, 0.85f, 0.85f);

                // Use the working CreateTMP helper that score/civ counter use
                var tmp = CreateTMP("Dir_" + dirs[i], bg,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(40f, COMPASS_HEIGHT),
                    dirs[i], fontSize, col, TextAlignmentOptions.Center);
                tmp.enableWordWrapping = false;
                if (isCardinal) tmp.fontStyle = FontStyles.Bold;
                compassLetters[i] = tmp;
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
                healthBarFill.fillAmount = Mathf.Clamp01(percent);
            if (healthText != null && playerHealth != null)
                healthText.text = $"{Mathf.CeilToInt(playerHealth.Health)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}";
        }

        private void UpdateSmoothHealthBar()
        {
            if (healthBarSmooth == null) return;
            currentSmoothHealth = Mathf.Lerp(currentSmoothHealth, smoothHealthTarget,
                Time.deltaTime * HEALTH_SMOOTH_SPEED);
            healthBarSmooth.fillAmount = Mathf.Clamp01(currentSmoothHealth);
        }

        private void UpdateStaminaBar(float percent)
        {
            if (staminaBarFill != null)
                staminaBarFill.fillAmount = Mathf.Clamp01(percent);
            if (staminaText != null && playerHealth != null)
                staminaText.text = $"{Mathf.CeilToInt(playerHealth.Stamina)}/{Mathf.CeilToInt(playerHealth.MaxStamina)}";
        }

        private void UpdateOxygenBar(float percent)
        {
            if (oxygenBarFill != null)
                oxygenBarFill.fillAmount = Mathf.Clamp01(percent);
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

        private int _missionDebugCount;
        private void UpdateMissionPanel()
        {
            // Re-find missionManager in case it was null at start
            if (missionManager == null)
                missionManager = MissionManager.Instance ?? FindAnyObjectByType<MissionManager>();

            // Log first 3 calls so we can definitively see what state UpdateMissionPanel sees
            if (_missionDebugCount < 3)
            {
                _missionDebugCount++;
                var mm = missionManager;
                var act = mm != null ? mm.ActiveMission : null;
                var allActive = mm != null ? mm.GetActiveMissions().Count : -1;
                Debug.Log($"[HUD-DBG #{_missionDebugCount}] frame={Time.frameCount} mm={(mm==null?"NULL":"OK")} active={(act==null?"NULL":act.missionName)} count={allActive} panel={(missionPanel==null?"NULL":missionPanel.activeSelf.ToString())} nameTxt={(missionNameText==null?"NULL":"OK")}");
            }

            if (missionManager == null)
            {
                if (missionNameText != null) missionNameText.text = "No MissionManager";
                return;
            }

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

            // Keep panel always visible — display state in text instead of hiding
            if (missionPanel != null && !missionPanel.activeSelf)
                missionPanel.SetActive(true);

            if (!hasMission)
            {
                if (missionNameText != null) missionNameText.text = "No active mission";
                if (missionObjectiveText != null) missionObjectiveText.text = "";
                if (missionTimerText != null) missionTimerText.gameObject.SetActive(false);
                return;
            }

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
            if (compassStrip == null || compassLetters == null) return;

            float yaw = 0f;
            if (playerTransform != null)
                yaw = playerTransform.eulerAngles.y;
            else if (thirdPersonCam != null)
                yaw = thirdPersonCam.Yaw;

            // Pixels per degree across the visible compass width.
            // Visible window = COMPASS_WIDTH pixels showing 180° of horizon.
            const float visibleDegrees = 180f;
            float pxPerDegree = (COMPASS_WIDTH - 8f) / visibleDegrees;

            // Reposition each label using shortest signed angle relative to player heading
            for (int i = 0; i < compassLetters.Length; i++)
            {
                if (compassLetters[i] == null) continue;
                float labelAngle = i * 45f; // N=0, NE=45, E=90, ...
                float diff = Mathf.DeltaAngle(yaw, labelAngle); // -180..+180
                float xPos = diff * pxPerDegree;
                var rt = compassLetters[i].rectTransform;
                rt.anchoredPosition = new Vector2(xPos, 0f);
                // Hard-cull labels outside the visible window; full opacity inside
                bool visible = Mathf.Abs(diff) <= visibleDegrees * 0.5f;
                compassLetters[i].gameObject.SetActive(visible);
            }

            UpdateCompassWaypoints(yaw);
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

        // --- Minimap (procedural, player-centered) ---

        private int minimapFrameCounter;
        private const float MINIMAP_VIEW_RANGE = 200f; // 200m radius around player
        private List<Vector3> buildingPositions;
        private float buildingCacheTime;

        private void RefreshBuildingCache()
        {
            buildingPositions = new List<Vector3>();
            // Find by tag if available
            try
            {
                var byTag = GameObject.FindGameObjectsWithTag("Building");
                foreach (var go in byTag) buildingPositions.Add(go.transform.position);
            }
            catch { }
            // Also find by common name patterns (no tag required)
            string[] keywords = { "House", "Building", "Hut", "Temple", "Barn", "Shed", "Cabin", "Tower" };
            var allRoots = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allRoots)
            {
                if (t == null || t.name == null) continue;
                foreach (var k in keywords)
                {
                    if (t.name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        buildingPositions.Add(t.position);
                        break;
                    }
                }
            }
            buildingCacheTime = Time.time;
        }

        private void DrawBuildingDots(float playerX, float playerZ, float pixelToWorld, int size)
        {
            // Refresh cache every 10 seconds (objects may stream in)
            if (buildingPositions == null || Time.time - buildingCacheTime > 10f)
                RefreshBuildingCache();

            Color buildingColor = new Color(0.55f, 0.4f, 0.25f);
            foreach (var pos in buildingPositions)
            {
                float dx = pos.x - playerX;
                float dz = pos.z - playerZ;
                if (Mathf.Abs(dx) > MINIMAP_VIEW_RANGE || Mathf.Abs(dz) > MINIMAP_VIEW_RANGE) continue;
                int px = Mathf.Clamp(Mathf.RoundToInt((dx / pixelToWorld) + size / 2f), 1, size - 2);
                int py = Mathf.Clamp(Mathf.RoundToInt((dz / pixelToWorld) + size / 2f), 1, size - 2);
                for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                        minimapTex.SetPixel(px + ox, py + oy, buildingColor);
            }
        }

        private void UpdateMinimap()
        {
            if (minimapTex == null || playerTransform == null) return;

            // Only redraw every 5 frames for performance (256x256 = 65k pixels)
            minimapFrameCounter++;
            if (minimapFrameCounter % 5 != 0) return;

            int size = MINIMAP_TEX_RES;
            float playerX = playerTransform.position.x;
            float playerZ = playerTransform.position.z;
            var terrain = Terrain.activeTerrain;

            // Sample terrain around player and draw to texture
            float pixelToWorld = MINIMAP_VIEW_RANGE * 2f / size;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float worldX = playerX + (x - size / 2f) * pixelToWorld;
                    float worldZ = playerZ + (y - size / 2f) * pixelToWorld;

                    Color c;
                    if (terrain != null)
                    {
                        float h = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
                        if (h < 2f) c = new Color(0.15f, 0.3f, 0.5f); // Water
                        else if (h < 10f) c = new Color(0.75f, 0.7f, 0.5f); // Sand/beach
                        else if (h < 30f) c = new Color(0.3f, 0.5f, 0.2f); // Grass
                        else if (h < 80f) c = new Color(0.2f, 0.4f, 0.15f); // Hills
                        else c = new Color(0.4f, 0.35f, 0.3f); // Mountains
                    }
                    else
                    {
                        c = new Color(0.2f, 0.3f, 0.15f);
                    }
                    minimapClearPixels[y * size + x] = c;
                }
            }

            minimapTex.SetPixels(minimapClearPixels);

            // Draw player heading cone (60° arc forward of player position)
            if (playerTransform != null)
            {
                float playerYaw = playerTransform.eulerAngles.y * Mathf.Deg2Rad;
                int coneRange = 32; // pixels
                float halfFOV = 30f * Mathf.Deg2Rad;
                Color coneColor = new Color(1f, 1f, 0.6f, 0.4f);
                for (int r = 2; r < coneRange; r++)
                {
                    int steps = Mathf.Max(8, r * 2);
                    for (int s = 0; s <= steps; s++)
                    {
                        float a = -halfFOV + (s / (float)steps) * (halfFOV * 2f);
                        float worldAngle = playerYaw + a;
                        int px = size / 2 + Mathf.RoundToInt(Mathf.Sin(worldAngle) * r);
                        int py = size / 2 + Mathf.RoundToInt(Mathf.Cos(worldAngle) * r);
                        if (px >= 0 && px < size && py >= 0 && py < size)
                        {
                            Color existing = minimapTex.GetPixel(px, py);
                            minimapTex.SetPixel(px, py, Color.Lerp(existing, coneColor, 0.45f));
                        }
                    }
                }
            }

            // Draw building/POI dots
            DrawBuildingDots(playerX, playerZ, pixelToWorld, size);

            // Draw NPCs as colored dots
            if (npcSpawner != null)
            {
                var npcs = npcSpawner.GetAllNPCs();
                if (npcs != null)
                {
                    foreach (var npc in npcs)
                    {
                        if (npc == null) continue;
                        float dx = npc.transform.position.x - playerX;
                        float dz = npc.transform.position.z - playerZ;
                        if (Mathf.Abs(dx) > MINIMAP_VIEW_RANGE || Mathf.Abs(dz) > MINIMAP_VIEW_RANGE) continue;

                        int px = Mathf.Clamp(Mathf.RoundToInt((dx / pixelToWorld) + size / 2f), 2, size - 3);
                        int py = Mathf.Clamp(Mathf.RoundToInt((dz / pixelToWorld) + size / 2f), 2, size - 3);

                        Color dotColor = Color.green;
                        var state = npc.CurrentState;
                        if (state == FightForLife.NPC.NPCState.Panicking || state == FightForLife.NPC.NPCState.Alert)
                            dotColor = Color.yellow;
                        else if (state == FightForLife.NPC.NPCState.Struggling || state == FightForLife.NPC.NPCState.Drowning || state == FightForLife.NPC.NPCState.Trapped)
                            dotColor = Color.red;

                        for (int ox = -2; ox <= 2; ox++)
                            for (int oy = -2; oy <= 2; oy++)
                                minimapTex.SetPixel(px + ox, py + oy, dotColor);
                    }
                }
            }

            // Draw mission waypoint + route line from player to waypoint
            if (missionManager != null && missionManager.ActiveMission != null)
            {
                var wp = missionManager.ActiveMission.waypointPosition;
                if (wp != Vector3.zero)
                {
                    float dx = wp.x - playerX;
                    float dz = wp.z - playerZ;
                    // Clamp to edge if outside range
                    float maxDist = MINIMAP_VIEW_RANGE * 0.95f;
                    if (Mathf.Abs(dx) > maxDist || Mathf.Abs(dz) > maxDist)
                    {
                        float scale = maxDist / Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
                        dx *= scale;
                        dz *= scale;
                    }
                    int wpx = Mathf.Clamp(Mathf.RoundToInt((dx / pixelToWorld) + size / 2f), 3, size - 4);
                    int wpy = Mathf.Clamp(Mathf.RoundToInt((dz / pixelToWorld) + size / 2f), 3, size - 4);

                    // Route line from player center to waypoint (dashed)
                    Color routeColor = new Color(1f, 0.85f, 0.2f, 1f);
                    DrawDashedLine(size / 2, size / 2, wpx, wpy, routeColor, 4, 3);

                    // Gold cross marker at waypoint
                    Color gold = new Color(1f, 0.85f, 0f);
                    for (int ox = -3; ox <= 3; ox++)
                    {
                        minimapTex.SetPixel(wpx + ox, wpy, gold);
                        minimapTex.SetPixel(wpx, wpy + ox, gold);
                    }
                    // Outline the gold cross with darker gold for visibility
                    Color goldDark = new Color(0.6f, 0.45f, 0f);
                    minimapTex.SetPixel(wpx + 4, wpy, goldDark);
                    minimapTex.SetPixel(wpx - 4, wpy, goldDark);
                    minimapTex.SetPixel(wpx, wpy + 4, goldDark);
                    minimapTex.SetPixel(wpx, wpy - 4, goldDark);
                }
            }

            // Player white cross in center
            int cx = size / 2;
            int cy = size / 2;
            for (int i = -3; i <= 3; i++)
            {
                minimapTex.SetPixel(cx + i, cy, Color.white);
                minimapTex.SetPixel(cx, cy + i, Color.white);
            }

            minimapTex.Apply();

            // Rotate player arrow to match player facing direction
            if (playerArrow != null && playerTransform != null)
                playerArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -playerTransform.eulerAngles.y);
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
