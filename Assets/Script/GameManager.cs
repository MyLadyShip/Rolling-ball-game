using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    #region Game Settings
    [Header("Game Settings")]
    public int totalCollectibles = 25;
    public float gameTime = 100f;
    #endregion

    #region References
    [Header("System References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CollectibleCounter collectibleCounter;
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private CollectibleSpawner collectibleSpawner;
    [SerializeField] private StartCountdown startCountdown;
    #endregion

    #region Game State
    private static bool _isRestarting;
    public bool IsGameOver { get; private set; }
    private GameState _currentGameState;
    #endregion

    protected override void Awake()
    {
        base.Awake(); // Calls the Singleton's Awake
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    #region Scene Management
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeGame();
        if (_isRestarting) HandleRestart();
    }

    private void InitializeGame()
    {
        ResetGameState();
        LoadPlayerPreferences();
        UIManager.Instance.InitializeUI();
        if (!_isRestarting) InitializeUI();
    }

    private void ResetGameState()
    {
        IsGameOver = false;
        Time.timeScale = 1f;
    }

    private void LoadPlayerPreferences()
    {
        int ballIndex = SaveLoadManager.Instance.LoadSelectedBall();
        SaveLoadManager.Instance.InitializeDefaults();
        PlayerController.Instance.SelectBall(ballIndex);
        PlayerController.Instance.InitializeBall();
        CollectibleSpawner.Instance.HandleGameStateChange(GameState.Initializing);
        CollectibleCounter.Instance.ResetCounter();
    }
    #endregion

    #region Game Flow
    public void StartNewGame()
    {
        startCountdown.BeginCountdown();
        collectibleCounter.ResetCounter();
        collectibleSpawner.SpawnCollectibles();
        playerController.EnableControls();
        uiManager.OnStartGame();
        countdownTimer.StartTimer(gameTime);
    }

    public void RestartGame()
    {
        _isRestarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleRestart()
    {
        _isRestarting = false;
        StartNewGame();
    }
    #endregion

    #region Gameplay Logic
    public void TriggerWin()
    {
        IsGameOver = true;
        countdownTimer.StopTimer();
        float timeRemaining = countdownTimer.TimeLeft;
        SaveLoadManager.Instance.AddHighScore(timeRemaining);
        uiManager.ShowWinPanel(timeRemaining);
    }

    public void TriggerLose()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        uiManager.ShowLosePanel();
        countdownTimer.StopTimer();
    }
    #endregion

    #region Pause/Resume
    public void PauseGame()
    {
        Time.timeScale = 0f;
        playerController.DisableControls();
        uiManager.OnPauseGame();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        playerController.EnableControls();
        uiManager.OnResumeGame();
        countdownTimer.StartTimer(gameTime);
    }
    #endregion

    #region System
    private void InitializeUI()
    {
        uiManager.InitializeUI();
    }
    #endregion

    private void OnEnable()
    {
        CollectibleCounter.OnAllCollectiblesCollected += TriggerWin;
    }

    private void OnDisable()
    {
        CollectibleCounter.OnAllCollectiblesCollected -= TriggerWin;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void SetGameState(GameState newState)
    {
        _currentGameState = newState;

        // Handle state-specific logic
        switch (newState)
        {
            case GameState.NameInput:
                AudioManager.Instance?.ReturnToLobbyMusic();
                break;

            case GameState.Playing:
                AudioManager.Instance?.SwitchToGameMusic();
                break;

            case GameState.Win:
            case GameState.Lose:
                AudioManager.Instance?.ReturnToLobbyMusic();
                break;
        }
    }
}