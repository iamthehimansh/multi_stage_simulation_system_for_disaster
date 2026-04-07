using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FightForLife.Core;

namespace FightForLife.UI
{
    /// <summary>
    /// In-game pause menu. Press Escape to toggle.
    /// Add this component to any persistent GameObject in the gameplay scene
    /// (it creates its own Canvas and UI programmatically).
    ///
    /// Pauses the game by setting Time.timeScale = 0 and shows cursor.
    /// Restores timeScale on resume and hides cursor.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private GameObject pauseCanvas;
        private GameObject pausePanel;
        private bool isPaused;

        // Colors matching the project palette
        private static readonly Color BG_OVERLAY = new Color(0.02f, 0.04f, 0.08f, 0.85f);
        private static readonly Color PANEL_BG = new Color(0.08f, 0.12f, 0.18f, 0.95f);
        private static readonly Color ACCENT_BLUE = new Color(0.2f, 0.5f, 0.85f, 1f);
        private static readonly Color ACCENT_RED = new Color(0.85f, 0.2f, 0.15f, 1f);
        private static readonly Color TEXT_WHITE = new Color(0.95f, 0.95f, 0.97f, 1f);
        private static readonly Color TEXT_GRAY = new Color(0.6f, 0.65f, 0.7f, 1f);
        private static readonly Color BUTTON_BG = new Color(0.15f, 0.2f, 0.3f, 0.9f);

        private void Awake()
        {
            BuildPauseUI();
            pauseCanvas.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Only respond if in Playing or Paused state
                if (GameManager.Instance == null) return;

                var state = GameManager.Instance.CurrentState;
                if (state == GameState.Playing)
                    Pause();
                else if (state == GameState.Paused)
                    Resume();
            }
        }

        public void Pause()
        {
            if (isPaused) return;
            isPaused = true;

            if (GameManager.Instance != null)
                GameManager.Instance.PauseGame();

            pauseCanvas.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            if (!isPaused) return;
            isPaused = false;

            pauseCanvas.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.ResumeGame();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void OnSettingsClicked()
        {
            // Placeholder: could open an in-game settings overlay later
            Debug.Log("[PauseMenu] Settings not yet implemented in-game.");
        }

        public void OnQuitToMenuClicked()
        {
            isPaused = false;
            pauseCanvas.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMenu();
        }

        // ===================== BUILD UI =====================

        private void BuildPauseUI()
        {
            // Dedicated overlay canvas (renders above gameplay UI)
            pauseCanvas = new GameObject("PauseMenuCanvas");
            pauseCanvas.transform.SetParent(transform, false);

            var canvas = pauseCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = pauseCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            pauseCanvas.AddComponent<GraphicRaycaster>();

            // Full-screen dark overlay
            var overlay = CreateRect(pauseCanvas.transform, "Overlay");
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            var overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color = BG_OVERLAY;

            // Center panel
            pausePanel = new GameObject("PausePanel");
            pausePanel.transform.SetParent(pauseCanvas.transform, false);
            var panelRt = pausePanel.AddComponent<RectTransform>();
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(420, 400);
            var panelImg = pausePanel.AddComponent<Image>();
            panelImg.color = PANEL_BG;

            // Accent bar at top
            var accent = CreateRect(pausePanel.transform, "AccentBar");
            accent.anchoredPosition = new Vector2(0, 195);
            accent.sizeDelta = new Vector2(420, 6);
            accent.gameObject.AddComponent<Image>().color = ACCENT_BLUE;

            // Title
            CreateText(pausePanel.transform, "Title", "PAUSED", 38, TEXT_WHITE, FontStyles.Bold,
                new Vector2(0, 140), new Vector2(400, 55));

            // Divider
            var div = CreateRect(pausePanel.transform, "Divider");
            div.anchoredPosition = new Vector2(0, 105);
            div.sizeDelta = new Vector2(300, 2);
            div.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            // Buttons
            float y = 50f;
            float spacing = 65f;

            CreateButton(pausePanel.transform, "RESUME", new Vector2(0, y),
                new Vector2(300, 50), BUTTON_BG, TEXT_WHITE, 22, Resume);
            y -= spacing;

            CreateButton(pausePanel.transform, "SETTINGS", new Vector2(0, y),
                new Vector2(300, 50), BUTTON_BG, TEXT_WHITE, 22, OnSettingsClicked);
            y -= spacing;

            CreateButton(pausePanel.transform, "QUIT TO MENU", new Vector2(0, y),
                new Vector2(300, 50), new Color(ACCENT_RED.r, ACCENT_RED.g, ACCENT_RED.b, 0.7f),
                TEXT_WHITE, 22, OnQuitToMenuClicked);

            // Hint text
            CreateText(pausePanel.transform, "Hint", "Press ESC to resume", 14, TEXT_GRAY, FontStyles.Italic,
                new Vector2(0, -160), new Vector2(300, 25));
        }

        // ===================== HELPERS =====================

        private RectTransform CreateRect(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.AddComponent<RectTransform>();
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            float fontSize, Color color, FontStyles style, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void CreateButton(Transform parent, string label, Vector2 pos, Vector2 size,
            Color bgColor, Color textColor, float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var obj = new GameObject($"Btn_{label.Replace(" ", "")}");
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = obj.AddComponent<Image>();
            img.color = bgColor;

            var btn = obj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.15f, bgColor.b + 0.2f, 1f);
            colors.pressedColor = new Color(bgColor.r - 0.05f, bgColor.g - 0.05f, bgColor.b - 0.05f, 1f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(ACCENT_BLUE.r, ACCENT_BLUE.g, ACCENT_BLUE.b, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            CreateText(obj.transform, "Label", label, fontSize, textColor, FontStyles.Bold,
                Vector2.zero, size);
        }
    }
}
