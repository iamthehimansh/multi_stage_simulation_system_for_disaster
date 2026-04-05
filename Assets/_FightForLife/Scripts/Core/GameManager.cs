using UnityEngine;
using UnityEngine.SceneManagement;

namespace FightForLife.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        [Header("Player Settings")]
        [SerializeField] private PlayerRole selectedRole = PlayerRole.Civilian;
        [SerializeField] private string selectedMap = "Village_Flood";

        public GameState CurrentState => currentState;
        public PlayerRole SelectedRole => selectedRole;
        public string SelectedMap => selectedMap;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetRole(PlayerRole role)
        {
            selectedRole = role;
        }

        public void SetMap(string mapName)
        {
            selectedMap = mapName;
        }

        public void StartGame()
        {
            currentState = GameState.Playing;
            SceneManager.LoadScene(selectedMap);
        }

        public void ReturnToMenu()
        {
            currentState = GameState.MainMenu;
            SceneManager.LoadScene("MainMenu");
        }

        public void PauseGame()
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
        }
    }

    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        MissionComplete,
        GameOver
    }

    public enum PlayerRole
    {
        Civilian,
        DisasterManagementExpert
    }
}
