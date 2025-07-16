using TMPro;
using UnityEngine;
using System.Collections;

public class StartCountdown : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private GameObject startButtonPanel;
    [SerializeField] private string[] countdownSteps = { "3", "2", "1", "GO!" };
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private GameObject gameUI;

    [Header("Audio References")]
    [SerializeField] private AudioSource countdownAudio;
    [SerializeField] private AudioClip beepSound;
    [SerializeField] private AudioClip goSound;

    [Header("Timing Settings")]
    [SerializeField] private float countdownStepDuration = 1f;
    [SerializeField] private float finalDelay = 0.5f;

    private bool _isCountingDown;

    private void OnEnable()
    {
        InitializeUIState();
    }

    public void BeginCountdown()
    {
        if (!_isCountingDown && gameObject.activeInHierarchy)
        {
            StartCoroutine(CountdownSequence());
        }
    }

    private IEnumerator CountdownSequence()
{
    // ... setup ...

    foreach (string step in countdownSteps)
    {
        if (step == "GO!") 
            yield return FinalizeCountdown();
        else
            yield return CountdownStep(step);
    }
}

    private IEnumerator CountdownStep(string text)
    {
        countdownText.text = text;
        PlaySound(beepSound);
        yield return new WaitForSecondsRealtime(countdownStepDuration);
    }

    private IEnumerator FinalizeCountdown()
    {
        countdownText.text = "GO!";
        PlaySound(goSound);
        yield return new WaitForSecondsRealtime(finalDelay);
        
        countdownText.gameObject.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);
    }

    private void InitializeUIState()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (gameUI != null) gameUI.SetActive(false);
    }

    
    private void InitializeCountdownDisplay()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (countdownAudio != null && clip != null)
        {
            countdownAudio.PlayOneShot(clip);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isCountingDown = false;
    }
}