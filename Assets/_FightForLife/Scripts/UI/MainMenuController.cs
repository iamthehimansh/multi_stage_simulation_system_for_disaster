using UnityEngine;
using UnityEngine.SceneManagement;

namespace FightForLife.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject comingSoonPanel;
        [SerializeField] private GameObject roleSelectPanel;

        [Header("Audio")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip buttonHoverSFX;

        private void Start()
        {
            ShowMainMenu();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ShowMainMenu()
        {
            SetAllPanelsInactive();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        public void OnNewGameClicked()
        {
            PlayClickSound();
            SetAllPanelsInactive();
            if (roleSelectPanel != null)
                roleSelectPanel.SetActive(true);
            else if (comingSoonPanel != null)
                comingSoonPanel.SetActive(true);
        }

        public void OnContinueClicked()
        {
            PlayClickSound();
            ShowComingSoon();
        }

        public void OnMissionSelectClicked()
        {
            PlayClickSound();
            ShowComingSoon();
        }

        public void OnSettingsClicked()
        {
            PlayClickSound();
            SetAllPanelsInactive();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void OnCreditsClicked()
        {
            PlayClickSound();
            SetAllPanelsInactive();
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        public void OnQuitClicked()
        {
            PlayClickSound();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnBackClicked()
        {
            PlayClickSound();
            ShowMainMenu();
        }

        public void OnRoleCivilianClicked()
        {
            PlayClickSound();
            ShowComingSoon();
            // Future: PlayerPrefs.SetInt("PlayerRole", 0);
            // Future: SceneManager.LoadScene("Village_Flood");
        }

        public void OnRoleExpertClicked()
        {
            PlayClickSound();
            ShowComingSoon();
            // Future: PlayerPrefs.SetInt("PlayerRole", 1);
            // Future: SceneManager.LoadScene("Village_Flood");
        }

        public void ShowComingSoon()
        {
            SetAllPanelsInactive();
            if (comingSoonPanel != null) comingSoonPanel.SetActive(true);
        }

        public void PlayClickSound()
        {
            if (sfxSource != null && buttonClickSFX != null)
                sfxSource.PlayOneShot(buttonClickSFX);
        }

        public void PlayHoverSound()
        {
            if (sfxSource != null && buttonHoverSFX != null)
                sfxSource.PlayOneShot(buttonHoverSFX);
        }

        private void SetAllPanelsInactive()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (comingSoonPanel != null) comingSoonPanel.SetActive(false);
            if (roleSelectPanel != null) roleSelectPanel.SetActive(false);
        }
    }
}
