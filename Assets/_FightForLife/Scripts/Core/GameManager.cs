using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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

        [Header("Difficulty")]
        [SerializeField] private Difficulty currentDifficulty = Difficulty.Normal;

        public GameState CurrentState => currentState;
        public PlayerRole SelectedRole => selectedRole;
        public string SelectedMap => selectedMap;
        public Difficulty CurrentDifficulty => currentDifficulty;
        public float TotalPlayTime => totalPlayTime;

        // Events
        public event Action<GameState> OnGameStateChanged;

        private float totalPlayTime;
        private bool isInitialized;

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

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                totalPlayTime += Time.deltaTime;
            }
        }

        #region Setup

        public void SetRole(PlayerRole role)
        {
            selectedRole = role;
        }

        public void SetMap(string mapName)
        {
            selectedMap = mapName;
        }

        public void SetDifficulty(Difficulty difficulty)
        {
            currentDifficulty = difficulty;
        }

        #endregion

        #region Game Flow

        public void StartGame()
        {
            SetState(GameState.Loading);
            totalPlayTime = 0f;
            isInitialized = false;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(selectedMap);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (scene.name == selectedMap)
            {
                InitializeGame();
            }
        }

        public void InitializeGame()
        {
            if (isInitialized) return;
            isInitialized = true;

            // Reset score
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.ResetScore();

            // Reset missions
            if (MissionManager.Instance != null)
                MissionManager.Instance.ResetAllMissions();

            SetState(GameState.Playing);
            SetCursorLocked(true);

            Debug.Log($"Game initialized - Role: {selectedRole}, Map: {selectedMap}, Difficulty: {currentDifficulty}");
        }

        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;

            SetState(GameState.Paused);
            Time.timeScale = 0f;
            SetCursorLocked(false);
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;

            SetState(GameState.Playing);
            Time.timeScale = 1f;
            SetCursorLocked(true);
        }

        public void GameOver(bool won)
        {
            Time.timeScale = 0f;

            if (won)
            {
                SetState(GameState.MissionComplete);
                Debug.Log("Mission Complete!");
            }
            else
            {
                SetState(GameState.GameOver);
                Debug.Log("Game Over!");
            }

            SetCursorLocked(false);
        }

        public void RestartMission()
        {
            Time.timeScale = 1f;
            totalPlayTime = 0f;
            isInitialized = false;

            SetState(GameState.Loading);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(selectedMap);
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            totalPlayTime = 0f;
            isInitialized = false;

            SetState(GameState.MainMenu);
            SetCursorLocked(false);
            SceneManager.LoadScene("MainMenu");
        }

        #endregion

        #region State Management

        private void SetState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            Debug.Log($"GameState: {previousState} -> {newState}");
            OnGameStateChanged?.Invoke(newState);
        }

        #endregion

        #region Cursor

        private void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        #endregion
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

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }
}
