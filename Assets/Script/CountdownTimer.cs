using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class CountdownTimer : MonoBehaviour
{
    #region Singleton
    public static CountdownTimer Instance { get; private set; }
    public UnityEvent OnTimerStart;
    public UnityEvent OnTimerEnd;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Timer Settings
    [Header("Timer Settings")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float initialTime = 100f;
    #endregion

    #region Timer State
    public float TimeLeft { get; private set; }
    private bool isTimerRunning;
    public bool IsTimerRunning => isTimerRunning;
    #endregion

    #region Timer Control
    public void StartTimer(float duration = 0f)
    {
        TimeLeft = duration > 0 ? duration : initialTime;
        isTimerRunning = true;
        UpdateTimerDisplay();
        StartCoroutine(RunTimer());
        OnTimerStart?.Invoke();
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void ResetTimer()
    {
        StopTimer();
        TimeLeft = initialTime;
        UpdateTimerDisplay();
    }
    #endregion

    #region Timer Logic
    private IEnumerator RunTimer()
    {
        while (TimeLeft > 0 && isTimerRunning)
        {
            TimeLeft -= Time.deltaTime;
            UpdateTimerDisplay();
            yield return null;
        }

        if (TimeLeft <= 0)
        {
            TimeLeft = 0;
            isTimerRunning = false;
            HandleTimerEnd();
        }
    }

    private void HandleTimerEnd()
    {
        OnTimerEnd?.Invoke();
        GameManager.Instance.TriggerLose();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = $"Countdown: {Mathf.CeilToInt(TimeLeft)}s";
        }
    }
    #endregion
}