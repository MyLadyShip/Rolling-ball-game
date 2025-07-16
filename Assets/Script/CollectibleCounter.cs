using UnityEngine;
using TMPro;

public class CollectibleCounter : MonoBehaviour
{
    public static CollectibleCounter Instance { get; private set; }

    [Header("UI Settings")]
    [SerializeField] private TMP_Text collectibleText;
    
    [Header("Game Settings")]
    [SerializeField] private int totalCollectibles = 25;
    
    private int _collected;
    public int Collected => _collected;
    public int TotalCollectibles => totalCollectibles;

    public static event System.Action OnAllCollectiblesCollected;

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

    private void Start()
    {
        ResetCounter();
    }

    public void IncrementCount()
    {
        if (GameManager.Instance.IsGameOver) return;

        _collected++;
        UpdateUI();

        if (_collected >= totalCollectibles)
        {
            OnAllCollectiblesCollected?.Invoke();
        }
    }

    public void UpdateUI(int collected = 0)
    {
        collectibleText.text = $"Collected: {_collected}/{totalCollectibles}";
    }

    public void ResetCounter()
    {
        _collected = 0;
        UpdateUI();
    }
}