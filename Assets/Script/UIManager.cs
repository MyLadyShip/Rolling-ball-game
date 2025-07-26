using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private GameObject ballSelectionPanel;
    [SerializeField] private GameObject startButtonPanel;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject quitConfirmationPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [Header("Win/Lose Display")]
    [SerializeField] private TMP_Text winTimeText;
    [SerializeField] private TMP_Text winRankText;
    [SerializeField] private TMP_Text loseTimeText;
    [SerializeField] private Transform highScoreContainer;

    [Header("Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button confirmQuitButton;
    [SerializeField] private Button cancelQuitButton;
    [SerializeField] private Button restartButtonWin;
    [SerializeField] private Button quitButtonWin;
    [SerializeField] private Button restartButtonLose;
    [SerializeField] private Button quitButtonLose;

    [Header("Player Name")]
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Ball Selection")]
    [SerializeField] private Button blueBallButton;
    [SerializeField] private Button redBallButton;
    [SerializeField] private Material blueBallMaterial;
    [SerializeField] private Material redBallMaterial;
    [SerializeField] private GameObject playerBall;

    [Header("Gameplay References")]
    [SerializeField] private CollectibleSpawner collectibleSpawner;
    [SerializeField] private CollectibleCounter collectibleCounter;
    [SerializeField] private CountdownTimer countdownTimer;  
    [SerializeField] private SaveLoadManager saveLoadManager;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();
    private float _finalTime;

    private void Start()
    {
        InitializeUI();
        InitializeButtonListeners();
    }

    public void InitializeUI()
    {
        HideAllPanels();
        OpenPanel(nameInputPanel);
    }
    public void InitializeButtonListeners()
    {
    // Assign button listeners
        previousButton.onClick.AddListener(OnBackButtonClicked);
        startGameButton.onClick.AddListener(OnStartGame);
        pauseButton.onClick.AddListener(OnPauseGame);
        resumeButton.onClick.AddListener(OnResumeGame);
        quitButton.onClick.AddListener(OnQuitButtonPressed);
        confirmQuitButton.onClick.AddListener(OnConfirmQuit);
        cancelQuitButton.onClick.AddListener(OnCancelQuit);
        restartButtonWin.onClick.AddListener(OnRestartGame);
        quitButtonWin.onClick.AddListener(OnReallyQuit);
        restartButtonLose.onClick.AddListener(OnRestartGameLose);
        quitButtonLose.onClick.AddListener(OnReallyQuitLose);
        blueBallButton.onClick.AddListener(() => OnBallSelected(0));
        redBallButton.onClick.AddListener(() => OnBallSelected(1));
    }
    private void HideAllPanels()
    {
        GameObject[] panels = { nameInputPanel, ballSelectionPanel, startButtonPanel, countdownPanel, pausePanel, quitConfirmationPanel, winPanel, losePanel };
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }
    }

    private void OpenPanel(GameObject panel)
    {
        if (panel == null) return;
        
        if (panelHistory.Count > 0 && panelHistory.Peek() != null)
        {
            panelHistory.Peek().SetActive(false);
        }
        
        panelHistory.Push(panel);
        panel.SetActive(true);
    }

    public void OnBackButtonClicked()
    {
        PlayButtonSound();
        if (panelHistory.Count <= 1) return;

        GameObject currentPanel = panelHistory.Pop();
        currentPanel.SetActive(false);

        GameObject previousPanel = panelHistory.Peek();
        previousPanel.SetActive(true);
    }

    public void SavePlayerName()
    {
        PlayButtonSound();
        string playerName = nameInputField.text;
        if (!string.IsNullOrEmpty(playerName))
        {
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }
        SaveLoadManager.Instance.InitializeDefaults(); // Use SaveLoadManager
        OpenPanel(ballSelectionPanel);
    }

    public void OnBallSelected(int ballIndex)
{
    PlayButtonSound();
    PlayerController.Instance.SelectBall(ballIndex);
    OpenPanel(startButtonPanel);
}

    public void OnStartGame()
    {
        PlayButtonSound();
        GameManager.Instance.StartNewGame(); // Calls StartCountdown.BeginCountdown()
        HideAllPanels();
        OpenPanel(countdownPanel);
        ShowGameplayUI();
    }

    private void ShowGameplayUI()
    {
        Time.timeScale = 1;
        if (pauseButton != null) pauseButton.gameObject.SetActive(true);
        if (quitButton != null) quitButton.gameObject.SetActive(true);
        collectibleSpawner.SpawnCollectibles();
        collectibleCounter.ResetCounter();
        countdownTimer.StartTimer(GameManager.Instance.gameTime); // Use GameManager's time
    }

    public void OnPauseGame()
    {
        PlayButtonSound();
        GameManager.Instance.SetGameState(GameState.Paused);
        Time.timeScale = 0;
        OpenPanel(pausePanel);
        countdownTimer.StopTimer();
        collectibleSpawner.SetCollectiblesActive(false);
    }

    public void OnResumeGame()
    {
        PlayButtonSound();
        GameManager.Instance.SetGameState(GameState.Playing);
        Time.timeScale = 1;
        pausePanel.SetActive(false);
        countdownTimer.StartTimer();
        collectibleSpawner.SetCollectiblesActive(true);
    }

    public void OnQuitButtonPressed()
    {
        PlayButtonSound();
        OpenPanel(quitConfirmationPanel);
    }

    public void OnConfirmQuit()
    {
        PlayButtonSound();
        Application.Quit();
    }

    public void OnCancelQuit()
    {
        PlayButtonSound();
        quitConfirmationPanel.SetActive(false);
    }

    public void ShowWinPanel(float timeRemaining)
    {
        _finalTime = timeRemaining;
        
        if (winTimeText != null)
            winTimeText.text = $"Time: {timeRemaining:F1}s";
        
        int playerRank = CalculatePlayerRank(timeRemaining);
        if (winRankText != null)
            winRankText.text = playerRank > 0 ? $"Rank: #{playerRank}" : "New High Score!";
        
        SaveLoadManager.Instance.DisplayHighScores();
        
        Time.timeScale = 0;
        OpenPanel(winPanel);
    }

    private int CalculatePlayerRank(float timeRemaining)
    {
        var scores = SaveLoadManager.Instance.GetHighScores();
        for (int i = 0; i < scores.Count; i++)
        {
            if (Mathf.Approximately(scores[i].timeRemaining, timeRemaining))
                return i + 1;
        }
        return -1;
    }

    public void ShowLosePanel()
    {
        PlayButtonSound();
        GameManager.Instance.TriggerLose();
        if (loseTimeText != null)
            loseTimeText.text = $"Time: {countdownTimer.TimeLeft:F1}s";
        
        Time.timeScale = 0;
        OpenPanel(losePanel);
    }

    public void OnRestartGame()
    {
        PlayButtonSound();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
        ShowGameplayUI();
    }

    public void OnReallyQuit()
    {
        PlayButtonSound();
        Application.Quit();
    }

    public void OnRestartGameLose()
    {
        PlayButtonSound();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
        ShowGameplayUI();
    }

    public void OnReallyQuitLose()
    {
        PlayButtonSound();
        Application.Quit();
    }

public void PlayButtonSound()
{
    AudioManager.Instance.PlayButtonClickSound();
}
}