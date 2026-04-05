using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FightForLife.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI masterValueLabel;
        [SerializeField] private TextMeshProUGUI musicValueLabel;
        [SerializeField] private TextMeshProUGUI sfxValueLabel;

        private Resolution[] resolutions;

        private void OnEnable()
        {
            LoadSettings();
        }

        private void Start()
        {
            // Setup quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                var qualityNames = QualitySettings.names;
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(qualityNames));
                qualityDropdown.value = QualitySettings.GetQualityLevel();
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }

            // Setup resolution dropdown
            if (resolutionDropdown != null)
            {
                resolutions = Screen.resolutions;
                resolutionDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string>();
                int currentIndex = 0;
                for (int i = 0; i < resolutions.Length; i++)
                {
                    string option = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRateRatio.value:F0}Hz";
                    options.Add(option);
                    if (resolutions[i].width == Screen.currentResolution.width &&
                        resolutions[i].height == Screen.currentResolution.height)
                        currentIndex = i;
                }
                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = currentIndex;
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            // Setup toggles
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (vsyncToggle != null)
            {
                vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
                vsyncToggle.onValueChanged.AddListener(OnVsyncChanged);
            }

            // Setup sliders
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index);
            PlayerPrefs.SetInt("QualityLevel", index);
        }

        private void OnResolutionChanged(int index)
        {
            if (resolutions != null && index < resolutions.Length)
            {
                var res = resolutions[index];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            }
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        }

        private void OnVsyncChanged(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            PlayerPrefs.SetInt("VSync", enabled ? 1 : 0);
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
            if (masterValueLabel != null) masterValueLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            if (musicValueLabel != null) musicValueLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            if (sfxValueLabel != null) sfxValueLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void LoadSettings()
        {
            float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float music = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

            if (masterVolumeSlider != null) masterVolumeSlider.value = master;
            if (musicVolumeSlider != null) musicVolumeSlider.value = music;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfx;

            AudioListener.volume = master;
        }

        public void OnApplyClicked()
        {
            PlayerPrefs.Save();
        }
    }
}
