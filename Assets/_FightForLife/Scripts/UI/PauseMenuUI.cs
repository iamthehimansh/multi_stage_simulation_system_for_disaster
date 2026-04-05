using UnityEngine;
using UnityEngine.SceneManagement;
using FightForLife.Core;

namespace FightForLife.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject darkOverlay;

        private bool isPaused;

        private void Start()
        {
            SetPauseUIActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                    return;
                }

                if (isPaused)
                    OnResumePressed();
                else
                    TryPause();
            }
        }

        private void TryPause()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            isPaused = true;
            GameManager.Instance.PauseGame();
            SetPauseUIActive(true);
            ShowCursor(true);
        }

        public void OnResumePressed()
        {
            if (!isPaused) return;

            isPaused = false;

            if (GameManager.Instance != null)
                GameManager.Instance.ResumeGame();

            SetPauseUIActive(false);
            ShowCursor(false);
        }

        public void OnSettingsPressed()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }

        public void OnReturnToMenuPressed()
        {
            isPaused = false;
            Time.timeScale = 1f;
            ShowCursor(true);

            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMenu();
            else
                SceneManager.LoadScene("MainMenu");
        }

        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetPauseUIActive(bool active)
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(active);

            if (darkOverlay != null)
                darkOverlay.SetActive(active);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void ShowCursor(bool show)
        {
            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
