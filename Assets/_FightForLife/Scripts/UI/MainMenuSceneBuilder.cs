using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace FightForLife.UI
{
    /// <summary>
    /// Editor tool that builds the entire Main Menu scene with all UI panels.
    /// Use: Unity Menu -> Fight For Life -> Build Main Menu Scene
    /// </summary>
    public class MainMenuSceneBuilder : MonoBehaviour
    {
#if UNITY_EDITOR
        // Color palette
        private static readonly Color BG_DARK = new Color(0.05f, 0.08f, 0.12f, 1f);
        private static readonly Color PANEL_BG = new Color(0.08f, 0.12f, 0.18f, 0.95f);
        private static readonly Color ACCENT_BLUE = new Color(0.2f, 0.5f, 0.85f, 1f);
        private static readonly Color ACCENT_ORANGE = new Color(0.9f, 0.5f, 0.1f, 1f);
        private static readonly Color ACCENT_RED = new Color(0.85f, 0.2f, 0.15f, 1f);
        private static readonly Color ACCENT_GREEN = new Color(0.15f, 0.7f, 0.3f, 1f);
        private static readonly Color TEXT_WHITE = new Color(0.95f, 0.95f, 0.97f, 1f);
        private static readonly Color TEXT_GRAY = new Color(0.6f, 0.65f, 0.7f, 1f);
        private static readonly Color BUTTON_BG = new Color(0.15f, 0.2f, 0.3f, 0.9f);
        private static readonly Color BUTTON_HOVER = new Color(0.2f, 0.3f, 0.45f, 1f);
        private static readonly Color WATER_BLUE = new Color(0.1f, 0.25f, 0.4f, 0.6f);

        [MenuItem("Fight For Life/Build Main Menu Scene")]
        public static void BuildScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Setup camera
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = BG_DARK;
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            // Create directional light (dim, moody)
            var lightObj = GameObject.Find("Directional Light");
            if (lightObj != null)
            {
                var light = lightObj.GetComponent<Light>();
                light.intensity = 0.3f;
                light.color = new Color(0.4f, 0.5f, 0.7f);
            }

            // Create GameManager
            var gmObj = new GameObject("GameManager");
            gmObj.AddComponent<Core.GameManager>();

            // Create Rain Background
            var rainObj = new GameObject("RainBackground");
            var particleBg = rainObj.AddComponent<ParticleBackground>();
            if (lightObj != null)
            {
                // Set directional light reference via serialized field
                var so = new SerializedObject(particleBg);
                so.FindProperty("directionalLight").objectReferenceValue = lightObj.GetComponent<Light>();
                so.ApplyModifiedProperties();
            }

            // Create Canvas
            var canvasObj = new GameObject("MainCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create EventSystem if not exists
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Create Audio Sources
            var audioObj = new GameObject("AudioManager");
            audioObj.transform.SetParent(canvasObj.transform);
            var bgmSource = audioObj.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0.5f;
            var sfxObj = new GameObject("SFX");
            sfxObj.transform.SetParent(audioObj.transform);
            var sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            // Create MainMenuController
            var menuController = canvasObj.AddComponent<MainMenuController>();

            // ============= MAIN MENU PANEL =============
            var mainMenuPanel = CreatePanel(canvasObj.transform, "MainMenuPanel");

            // Background gradient overlay
            var bgOverlay = CreateUIElement<Image>(mainMenuPanel.transform, "BackgroundOverlay",
                Vector2.zero, new Vector2(1920, 1080));
            bgOverlay.color = new Color(0.03f, 0.06f, 0.1f, 0.7f);

            // Water effect bar at bottom
            var waterBar = CreateUIElement<Image>(mainMenuPanel.transform, "WaterEffect",
                new Vector2(0, -440), new Vector2(1920, 200));
            waterBar.color = WATER_BLUE;

            // Title container
            var titleContainer = CreateUIElement<RectTransform>(mainMenuPanel.transform, "TitleContainer",
                new Vector2(0, 200), new Vector2(800, 300));
            titleContainer.gameObject.AddComponent<FloatingAnimation>();

            // Game Title
            var titleText = CreateTMPText(titleContainer.transform, "TitleText",
                "FIGHT FOR LIFE", 72, TEXT_WHITE, FontStyles.Bold,
                new Vector2(0, 40), new Vector2(800, 100));
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.enableWordWrapping = false;

            // Subtitle
            var subtitleText = CreateTMPText(titleContainer.transform, "SubtitleText",
                "DISASTER MANAGEMENT", 28, ACCENT_BLUE, FontStyles.Normal,
                new Vector2(0, -30), new Vector2(800, 50));
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.characterSpacing = 12;

            // Tagline
            var tagline = CreateTMPText(titleContainer.transform, "Tagline",
                "Every Second Counts. Every Life Matters.", 18, TEXT_GRAY, FontStyles.Italic,
                new Vector2(0, -75), new Vector2(800, 40));
            tagline.alignment = TextAlignmentOptions.Center;

            // Divider line
            var divider = CreateUIElement<Image>(mainMenuPanel.transform, "Divider",
                new Vector2(0, 80), new Vector2(400, 2));
            divider.color = ACCENT_BLUE;

            // Button container
            float buttonStartY = 20f;
            float buttonSpacing = 65f;
            float buttonWidth = 350f;
            float buttonHeight = 55f;

            string[] buttonNames = { "NEW GAME", "CONTINUE", "MISSION SELECT", "SETTINGS", "CREDITS", "QUIT" };
            string[] methodNames = { "OnNewGameClicked", "OnContinueClicked", "OnMissionSelectClicked", "OnSettingsClicked", "OnCreditsClicked", "OnQuitClicked" };

            for (int i = 0; i < buttonNames.Length; i++)
            {
                float yPos = buttonStartY - (i * buttonSpacing);
                var btn = CreateStyledButton(mainMenuPanel.transform, buttonNames[i],
                    new Vector2(0, yPos), new Vector2(buttonWidth, buttonHeight),
                    BUTTON_BG, TEXT_WHITE, 22);

                var button = btn.GetComponent<Button>();
                var target = menuController;
                string method = methodNames[i];

                // Use UnityEvent persistence
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,
                    (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                        typeof(UnityEngine.Events.UnityAction), target, method));

                // Add hover effects
                var effects = btn.AddComponent<UIButtonEffects>();
                var effectsSO = new SerializedObject(effects);
                effectsSO.FindProperty("menuController").objectReferenceValue = menuController;
                effectsSO.ApplyModifiedProperties();
            }

            // Version text
            var versionText = CreateTMPText(mainMenuPanel.transform, "VersionText",
                "v0.1.0 - ALPHA | Flood Module", 14, TEXT_GRAY, FontStyles.Normal,
                new Vector2(-20, -510), new Vector2(300, 30));
            versionText.alignment = TextAlignmentOptions.Left;
            versionText.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            versionText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
            versionText.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
            versionText.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, 20);

            // ============= ROLE SELECT PANEL =============
            var rolePanel = CreatePanel(canvasObj.transform, "RoleSelectPanel");
            rolePanel.SetActive(false);

            var roleBg = CreateUIElement<Image>(rolePanel.transform, "BG",
                Vector2.zero, new Vector2(1920, 1080));
            roleBg.color = new Color(0.03f, 0.06f, 0.1f, 0.9f);

            var roleTitle = CreateTMPText(rolePanel.transform, "RoleTitle",
                "SELECT YOUR ROLE", 48, TEXT_WHITE, FontStyles.Bold,
                new Vector2(0, 350), new Vector2(800, 70));
            roleTitle.alignment = TextAlignmentOptions.Center;

            var roleSubtitle = CreateTMPText(rolePanel.transform, "RoleSubtitle",
                "Choose how you face the disaster", 20, TEXT_GRAY, FontStyles.Italic,
                new Vector2(0, 300), new Vector2(800, 40));
            roleSubtitle.alignment = TextAlignmentOptions.Center;

            // Civilian Card
            var civilianCard = CreateRoleCard(rolePanel.transform, "CivilianCard",
                new Vector2(-250, 0),
                "CIVILIAN",
                "An ordinary person caught in the disaster.\nLimited tools, raw survival instinct.",
                "HP: 100  |  Stamina: Medium\nSwim: Slow  |  Carry: 1 Item\n\nAbility: Adrenaline Rush\nSpeed boost when health < 25%",
                ACCENT_BLUE);

            var civButton = civilianCard.GetComponentInChildren<Button>();
            if (civButton != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(civButton.onClick,
                    (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                        typeof(UnityEngine.Events.UnityAction), menuController, "OnRoleCivilianClicked"));
            }

            // Expert Card
            var expertCard = CreateRoleCard(rolePanel.transform, "ExpertCard",
                new Vector2(250, 0),
                "DISASTER EXPERT",
                "Trained professional with specialized\nequipment and rescue knowledge.",
                "HP: 120  |  Stamina: High\nSwim: Fast  |  Carry: 3 Items\n\nAbility: Command Mode\nReveal civilians & hazards on map",
                ACCENT_ORANGE);

            var expButton = expertCard.GetComponentInChildren<Button>();
            if (expButton != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(expButton.onClick,
                    (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                        typeof(UnityEngine.Events.UnityAction), menuController, "OnRoleExpertClicked"));
            }

            // Back button
            var roleBackBtn = CreateStyledButton(rolePanel.transform, "BACK",
                new Vector2(0, -400), new Vector2(200, 50), BUTTON_BG, TEXT_WHITE, 20);
            var roleBackButton = roleBackBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(roleBackButton.onClick,
                (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), menuController, "OnBackClicked"));

            // ============= COMING SOON PANEL =============
            var comingSoonPanel = CreatePanel(canvasObj.transform, "ComingSoonPanel");
            comingSoonPanel.SetActive(false);
            var csCg = comingSoonPanel.AddComponent<CanvasGroup>();

            var csBg = CreateUIElement<Image>(comingSoonPanel.transform, "BG",
                Vector2.zero, new Vector2(1920, 1080));
            csBg.color = new Color(0.03f, 0.06f, 0.1f, 0.95f);

            // Coming Soon central content
            var csContainer = CreateUIElement<RectTransform>(comingSoonPanel.transform, "Container",
                new Vector2(0, 50), new Vector2(700, 500));

            // Warning icon (text-based)
            var warningIcon = CreateTMPText(csContainer.transform, "WarningIcon",
                "//", 80, ACCENT_ORANGE, FontStyles.Bold,
                new Vector2(0, 180), new Vector2(200, 100));
            warningIcon.alignment = TextAlignmentOptions.Center;

            var csTitle = CreateTMPText(csContainer.transform, "CSTitle",
                "COMING SOON", 64, TEXT_WHITE, FontStyles.Bold,
                new Vector2(0, 80), new Vector2(700, 80));
            csTitle.alignment = TextAlignmentOptions.Center;

            var csSubtitle = CreateTMPText(csContainer.transform, "CSSubtitle",
                "FLOOD DISASTER MODULE", 24, ACCENT_BLUE, FontStyles.Normal,
                new Vector2(0, 25), new Vector2(700, 40));
            csSubtitle.alignment = TextAlignmentOptions.Center;
            csSubtitle.characterSpacing = 8;

            var csDivider = CreateUIElement<Image>(csContainer.transform, "Divider",
                new Vector2(0, -10), new Vector2(300, 2));
            csDivider.color = ACCENT_BLUE;

            var csDesc = CreateTMPText(csContainer.transform, "CSDescription",
                "The waters are rising. Villages are flooding.\nThe city is next.\n\nWill you survive? Will you save others?\n\nFlooded Village  -  5 Primary Missions\nFlooded City  -  5 Primary Missions\n25+ Rescue Missions  -  2 Playable Roles",
                18, TEXT_GRAY, FontStyles.Normal,
                new Vector2(0, -110), new Vector2(600, 200));
            csDesc.alignment = TextAlignmentOptions.Center;
            csDesc.lineSpacing = 8;

            // Feature tags
            string[] features = { "3D OPEN WORLD", "DYNAMIC FLOODS", "NPC RESCUE", "DUAL ROLES" };
            Color[] featureColors = { ACCENT_BLUE, WATER_BLUE, ACCENT_GREEN, ACCENT_ORANGE };
            for (int i = 0; i < features.Length; i++)
            {
                float xPos = -225f + (i * 150f);
                var tag = CreateUIElement<Image>(csContainer.transform, $"Tag_{features[i]}",
                    new Vector2(xPos, -250), new Vector2(140, 35));
                tag.color = new Color(featureColors[i].r, featureColors[i].g, featureColors[i].b, 0.3f);

                var tagText = CreateTMPText(tag.transform, "Text",
                    features[i], 11, TEXT_WHITE, FontStyles.Bold,
                    Vector2.zero, new Vector2(140, 35));
                tagText.alignment = TextAlignmentOptions.Center;
            }

            var csBackBtn = CreateStyledButton(comingSoonPanel.transform, "BACK TO MENU",
                new Vector2(0, -380), new Vector2(280, 55), BUTTON_BG, TEXT_WHITE, 20);
            var csBackButton = csBackBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(csBackButton.onClick,
                (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), menuController, "OnBackClicked"));

            // Add ComingSoonUI component
            var csUI = comingSoonPanel.AddComponent<ComingSoonUI>();
            var csUISO = new SerializedObject(csUI);
            csUISO.FindProperty("pulseText").objectReferenceValue = csTitle;
            csUISO.FindProperty("descriptionText").objectReferenceValue = csDesc;
            csUISO.FindProperty("canvasGroup").objectReferenceValue = csCg;
            csUISO.ApplyModifiedProperties();

            // ============= SETTINGS PANEL =============
            var settingsPanel = CreatePanel(canvasObj.transform, "SettingsPanel");
            settingsPanel.SetActive(false);

            var setBg = CreateUIElement<Image>(settingsPanel.transform, "BG",
                Vector2.zero, new Vector2(1920, 1080));
            setBg.color = new Color(0.03f, 0.06f, 0.1f, 0.95f);

            var setTitle = CreateTMPText(settingsPanel.transform, "SettingsTitle",
                "SETTINGS", 48, TEXT_WHITE, FontStyles.Bold,
                new Vector2(0, 380), new Vector2(600, 70));
            setTitle.alignment = TextAlignmentOptions.Center;

            // Settings container
            var setContainer = CreateUIElement<Image>(settingsPanel.transform, "SettingsContainer",
                new Vector2(0, 20), new Vector2(700, 550));
            setContainer.color = PANEL_BG;

            // Audio section
            var audioTitle = CreateTMPText(setContainer.transform, "AudioTitle",
                "AUDIO", 24, ACCENT_BLUE, FontStyles.Bold,
                new Vector2(0, 230), new Vector2(600, 40));
            audioTitle.alignment = TextAlignmentOptions.Left;

            CreateSliderRow(setContainer.transform, "Master Volume", new Vector2(0, 185));
            CreateSliderRow(setContainer.transform, "Music Volume", new Vector2(0, 135));
            CreateSliderRow(setContainer.transform, "SFX Volume", new Vector2(0, 85));

            // Graphics section
            var gfxTitle = CreateTMPText(setContainer.transform, "GFXTitle",
                "GRAPHICS", 24, ACCENT_BLUE, FontStyles.Bold,
                new Vector2(0, 20), new Vector2(600, 40));
            gfxTitle.alignment = TextAlignmentOptions.Left;

            CreateDropdownRow(setContainer.transform, "Quality", new Vector2(0, -25));
            CreateDropdownRow(setContainer.transform, "Resolution", new Vector2(0, -75));
            CreateToggleRow(setContainer.transform, "Fullscreen", new Vector2(-100, -130));
            CreateToggleRow(setContainer.transform, "VSync", new Vector2(100, -130));

            var setBackBtn = CreateStyledButton(settingsPanel.transform, "BACK",
                new Vector2(0, -380), new Vector2(200, 50), BUTTON_BG, TEXT_WHITE, 20);
            var setBackButton = setBackBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(setBackButton.onClick,
                (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), menuController, "OnBackClicked"));

            settingsPanel.AddComponent<SettingsUI>();

            // ============= CREDITS PANEL =============
            var creditsPanel = CreatePanel(canvasObj.transform, "CreditsPanel");
            creditsPanel.SetActive(false);

            var crBg = CreateUIElement<Image>(creditsPanel.transform, "BG",
                Vector2.zero, new Vector2(1920, 1080));
            crBg.color = new Color(0.03f, 0.06f, 0.1f, 0.95f);

            var crTitle = CreateTMPText(creditsPanel.transform, "CreditsTitle",
                "CREDITS", 48, TEXT_WHITE, FontStyles.Bold,
                new Vector2(0, 380), new Vector2(600, 70));
            crTitle.alignment = TextAlignmentOptions.Center;

            // Scrolling credits content
            var crScrollArea = CreateUIElement<RectTransform>(creditsPanel.transform, "ScrollArea",
                new Vector2(0, -30), new Vector2(600, 600));
            var crMask = crScrollArea.gameObject.AddComponent<RectMask2D>();

            var crContent = CreateUIElement<RectTransform>(crScrollArea.transform, "ScrollContent",
                new Vector2(0, -300), new Vector2(600, 1200));

            string creditsContent =
                "FIGHT FOR LIFE\n" +
                "Disaster Management Game\n\n\n" +
                "<size=24><color=#E8963F>MENTOR</color></size>\n" +
                "<size=28>Ms. Daksha Borada</size>\n\n\n" +
                "<size=24><color=#3380D9>AI & GAME DEVELOPMENT</color></size>\n" +
                "Himansh Raj\n\n" +
                "<size=24><color=#3380D9>TEAM</color></size>\n" +
                "Deeksha Darshi\n" +
                "Divyansh Kaushik\n" +
                "Gaurav\n" +
                "Harsh\n\n\n" +
                "<size=24><color=#3380D9>SPECIAL THANKS</color></size>\n" +
                "Unity Technologies\n" +
                "Real-World Disaster Response Teams\n" +
                "All First Responders Worldwide\n\n\n" +
                "<size=24><color=#3380D9>CONNECT WITH HIMANSH</color></size>\n\n" +
                "<size=16><color=#999>GitHub</color></size>\n" +
                "iamthehimansh\n\n" +
                "<size=16><color=#999>LinkedIn</color></size>\n" +
                "iamthehimansh\n\n" +
                "<size=16><color=#999>Instagram</color></size>\n" +
                "iamthehimansh\n\n" +
                "<size=16><color=#999>X (Twitter)</color></size>\n" +
                "iamthehimansh_\n\n" +
                "<size=16><color=#999>Website</color></size>\n" +
                "himansh.in\n\n\n" +
                "<size=18><color=#666>Built with Unity</color></size>\n" +
                "<size=16><color=#555>v0.1.0 Alpha</color></size>";

            var crText = CreateTMPText(crContent.transform, "CreditsText",
                creditsContent, 20, TEXT_WHITE, FontStyles.Normal,
                Vector2.zero, new Vector2(600, 1200));
            crText.alignment = TextAlignmentOptions.Center;
            crText.lineSpacing = 5;
            crText.richText = true;

            var creditsUI = creditsPanel.AddComponent<CreditsUI>();
            var creditsUISO = new SerializedObject(creditsUI);
            creditsUISO.FindProperty("scrollContent").objectReferenceValue = crContent;
            creditsUISO.ApplyModifiedProperties();

            var crBackBtn = CreateStyledButton(creditsPanel.transform, "BACK",
                new Vector2(0, -450), new Vector2(200, 50), BUTTON_BG, TEXT_WHITE, 20);
            var crBackButton = crBackBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(crBackButton.onClick,
                (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), menuController, "OnBackClicked"));

            // ============= WIRE UP MENU CONTROLLER =============
            var mcSO = new SerializedObject(menuController);
            mcSO.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            mcSO.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            mcSO.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
            mcSO.FindProperty("comingSoonPanel").objectReferenceValue = comingSoonPanel;
            mcSO.FindProperty("roleSelectPanel").objectReferenceValue = rolePanel;
            mcSO.FindProperty("bgmSource").objectReferenceValue = bgmSource;
            mcSO.FindProperty("sfxSource").objectReferenceValue = sfxSource;
            mcSO.ApplyModifiedProperties();

            // Save scene
            string scenePath = "Assets/_FightForLife/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"<color=green>[Fight For Life]</color> Main Menu scene built and saved to: {scenePath}");

            // Add to build settings
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool exists = false;
            foreach (var s in buildScenes)
            {
                if (s.path == scenePath) { exists = true; break; }
            }
            if (!exists)
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
                Debug.Log("[Fight For Life] MainMenu scene added to Build Settings");
            }
        }

        // ============= HELPER METHODS =============

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return obj;
        }

        private static T CreateUIElement<T>(Transform parent, string name, Vector2 position, Vector2 size) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            if (typeof(T) == typeof(RectTransform))
                return rt as T;

            return obj.AddComponent<T>();
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
            float fontSize, Color color, FontStyles style, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.richText = true;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;

            return tmp;
        }

        private static GameObject CreateStyledButton(Transform parent, string text,
            Vector2 position, Vector2 size, Color bgColor, Color textColor, float fontSize)
        {
            var obj = new GameObject($"Btn_{text.Replace(" ", "")}");
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var image = obj.AddComponent<Image>();
            image.color = bgColor;

            var button = obj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.15f, bgColor.b + 0.2f, 1f);
            colors.pressedColor = new Color(bgColor.r - 0.05f, bgColor.g - 0.05f, bgColor.b - 0.05f, 1f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            // Button border (outline)
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(ACCENT_BLUE.r, ACCENT_BLUE.g, ACCENT_BLUE.b, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            var label = CreateTMPText(obj.transform, "Label", text,
                fontSize, textColor, FontStyles.Bold,
                Vector2.zero, size);
            label.alignment = TextAlignmentOptions.Center;
            label.characterSpacing = 3;

            return obj;
        }

        private static GameObject CreateRoleCard(Transform parent, string name,
            Vector2 position, string roleName, string description, string stats, Color accentColor)
        {
            var card = new GameObject(name);
            card.transform.SetParent(parent, false);
            var rt = card.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(400, 500);

            var bg = card.AddComponent<Image>();
            bg.color = PANEL_BG;

            // Accent bar at top
            var accent = CreateUIElement<Image>(card.transform, "AccentBar",
                new Vector2(0, 240), new Vector2(400, 10));
            accent.color = accentColor;

            // Role name
            var nameText = CreateTMPText(card.transform, "RoleName",
                roleName, 32, accentColor, FontStyles.Bold,
                new Vector2(0, 190), new Vector2(360, 50));
            nameText.alignment = TextAlignmentOptions.Center;

            // Description
            var descText = CreateTMPText(card.transform, "Description",
                description, 16, TEXT_GRAY, FontStyles.Normal,
                new Vector2(0, 120), new Vector2(360, 80));
            descText.alignment = TextAlignmentOptions.Center;

            // Divider
            var div = CreateUIElement<Image>(card.transform, "Divider",
                new Vector2(0, 70), new Vector2(300, 1));
            div.color = new Color(1, 1, 1, 0.2f);

            // Stats
            var statsText = CreateTMPText(card.transform, "Stats",
                stats, 15, TEXT_WHITE, FontStyles.Normal,
                new Vector2(0, -20), new Vector2(360, 150));
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.lineSpacing = 8;

            // Select button
            var selectBtn = CreateStyledButton(card.transform, "SELECT",
                new Vector2(0, -190), new Vector2(200, 50),
                new Color(accentColor.r, accentColor.g, accentColor.b, 0.8f),
                TEXT_WHITE, 20);

            return card;
        }

        private static void CreateSliderRow(Transform parent, string label, Vector2 position)
        {
            var row = new GameObject($"Row_{label.Replace(" ", "")}");
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(600, 40);

            var labelText = CreateTMPText(row.transform, "Label",
                label, 16, TEXT_WHITE, FontStyles.Normal,
                new Vector2(-200, 0), new Vector2(200, 30));
            labelText.alignment = TextAlignmentOptions.Left;

            // Slider
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            var sliderRt = sliderObj.AddComponent<RectTransform>();
            sliderRt.anchoredPosition = new Vector2(60, 0);
            sliderRt.sizeDelta = new Vector2(300, 20);

            // Slider background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillRt = fillArea.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImgRt = fill.AddComponent<RectTransform>();
            fillImgRt.anchorMin = Vector2.zero;
            fillImgRt.anchorMax = Vector2.one;
            fillImgRt.offsetMin = Vector2.zero;
            fillImgRt.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = ACCENT_BLUE;

            // Handle
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRt = handleArea.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = Vector2.zero;
            handleAreaRt.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20, 30);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = TEXT_WHITE;

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillImgRt;
            slider.handleRect = handleRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.75f;
            slider.targetGraphic = handleImg;

            // Value label
            var valueText = CreateTMPText(row.transform, "Value",
                "75%", 14, TEXT_GRAY, FontStyles.Normal,
                new Vector2(250, 0), new Vector2(60, 30));
            valueText.alignment = TextAlignmentOptions.Right;
        }

        private static void CreateDropdownRow(Transform parent, string label, Vector2 position)
        {
            var row = new GameObject($"Row_{label}");
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(600, 40);

            var labelText = CreateTMPText(row.transform, "Label",
                label, 16, TEXT_WHITE, FontStyles.Normal,
                new Vector2(-200, 0), new Vector2(200, 30));
            labelText.alignment = TextAlignmentOptions.Left;

            // Dropdown placeholder
            var dropObj = new GameObject("Dropdown");
            dropObj.transform.SetParent(row.transform, false);
            var dropRt = dropObj.AddComponent<RectTransform>();
            dropRt.anchoredPosition = new Vector2(100, 0);
            dropRt.sizeDelta = new Vector2(300, 35);
            var dropImg = dropObj.AddComponent<Image>();
            dropImg.color = new Color(0.15f, 0.18f, 0.25f, 1f);

            // TMP Dropdown
            var dropdown = dropObj.AddComponent<TMP_Dropdown>();

            // Caption text
            var captionText = CreateTMPText(dropObj.transform, "Label",
                label, 14, TEXT_WHITE, FontStyles.Normal,
                new Vector2(10, 0), new Vector2(260, 35));
            captionText.alignment = TextAlignmentOptions.Left;
            dropdown.captionText = captionText;

            // Template (minimal)
            var template = new GameObject("Template");
            template.transform.SetParent(dropObj.transform, false);
            var templateRt = template.AddComponent<RectTransform>();
            templateRt.anchoredPosition = new Vector2(0, -35);
            templateRt.sizeDelta = new Vector2(300, 150);
            template.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 1f);
            template.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            var vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 30);

            var item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            var itemRt = item.AddComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(0, 30);
            item.AddComponent<Toggle>();

            var itemLabel = CreateTMPText(item.transform, "Item Label",
                "Option", 14, TEXT_WHITE, FontStyles.Normal,
                Vector2.zero, new Vector2(300, 30));
            itemLabel.alignment = TextAlignmentOptions.Left;
            dropdown.itemText = itemLabel;

            template.SetActive(false);
            dropdown.template = templateRt;
        }

        private static void CreateToggleRow(Transform parent, string label, Vector2 position)
        {
            var row = new GameObject($"Toggle_{label}");
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(200, 30);

            // Toggle background
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(row.transform, false);
            var toggleRt = toggleObj.AddComponent<RectTransform>();
            toggleRt.anchoredPosition = new Vector2(-40, 0);
            toggleRt.sizeDelta = new Vector2(25, 25);

            var bgImg = toggleObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);

            // Checkmark
            var checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(toggleObj.transform, false);
            var checkRt = checkObj.AddComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.15f, 0.15f);
            checkRt.anchorMax = new Vector2(0.85f, 0.85f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            var checkImg = checkObj.AddComponent<Image>();
            checkImg.color = ACCENT_BLUE;

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.graphic = checkImg;
            toggle.targetGraphic = bgImg;
            toggle.isOn = true;

            var labelText = CreateTMPText(row.transform, "Label",
                label, 16, TEXT_WHITE, FontStyles.Normal,
                new Vector2(20, 0), new Vector2(150, 30));
            labelText.alignment = TextAlignmentOptions.Left;
        }
#endif
    }
}
