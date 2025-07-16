using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameSceneManager : Singleton<GameSceneManager>
{
    [Header("Loading Screen Settings")]
    [SerializeField] private GameObject loadingScreenPrefab;
    [SerializeField] private float minimumLoadTime = 2f;
    
    [Header("Capsule Animation")]
    [SerializeField] private Image rotatingCapsule;
    [SerializeField] private float rotationSpeed = 180f;

    private GameObject _currentLoadingScreen;
    private AsyncOperation _currentLoadingOperation;
    private bool _isLoading;

    public void LoadScene(string sceneName)
    {
        if (!_isLoading)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isLoading = true;
        ShowLoadingScreen();
        
        float loadStartTime = Time.time;
        _currentLoadingOperation = SceneManager.LoadSceneAsync(sceneName);
        _currentLoadingOperation.allowSceneActivation = false;

        while (!_currentLoadingOperation.isDone)
        {
            // Enforce minimum load time
            if (Time.time - loadStartTime >= minimumLoadTime && 
                _currentLoadingOperation.progress >= 0.9f)
            {
                _currentLoadingOperation.allowSceneActivation = true;
            }
            
            yield return null;
        }

        HideLoadingScreen();
        _isLoading = false;
    }

    private void Update()
    {
        if (_isLoading && rotatingCapsule != null)
        {
            rotatingCapsule.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }

    private void ShowLoadingScreen()
    {
        if (loadingScreenPrefab)
        {
            _currentLoadingScreen = Instantiate(loadingScreenPrefab);
            rotatingCapsule = _currentLoadingScreen.GetComponentInChildren<Image>();
        }
    }

    private void HideLoadingScreen()
    {
        if (_currentLoadingScreen)
        {
            Destroy(_currentLoadingScreen);
        }
    }
}