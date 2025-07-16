using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollectibleSpawner : Singleton<CollectibleSpawner>
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject collectiblePrefab;
    [SerializeField] private int poolSize = 25;
    [SerializeField] private Vector3 spawnArea = new Vector3(5f, 0.5f, 5f);
    
    [Header("Visual Settings")]
    [SerializeField] private Material[] collectibleMaterials;
    [SerializeField] private float spawnDelay = 0.1f;

    private Queue<GameObject> _collectiblePool = new Queue<GameObject>();
    private List<GameObject> _activeCollectibles = new List<GameObject>();
    private Dictionary<GameObject, Material> _materialCache = new Dictionary<GameObject, Material>();

    protected override void Awake()
    {
        base.Awake();
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject collectible = CreateCollectible();
            _collectiblePool.Enqueue(collectible);
        }
    }

    private GameObject CreateCollectible()
    {
        GameObject collectible = Instantiate(collectiblePrefab, transform);
        collectible.SetActive(false);
        
        // Cache materials for performance
        var renderer = collectible.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            _materialCache[collectible] = renderer.material;
        }
        
        return collectible;
    }

    public void SpawnCollectibles()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        ClearCollectibles();
        
        for (int i = 0; i < poolSize; i++)
        {
            if (_collectiblePool.Count == 0)
            {
                ExpandPool(5);
            }

            GameObject collectible = _collectiblePool.Dequeue();
            SetupCollectible(collectible);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SetupCollectible(GameObject collectible)
    {
        collectible.transform.position = GetRandomPosition();
        ApplyRandomMaterial(collectible);
        collectible.SetActive(true);
        _activeCollectibles.Add(collectible);
    }

    private void ApplyRandomMaterial(GameObject collectible)
    {
        if (collectibleMaterials.Length == 0 || !_materialCache.ContainsKey(collectible)) return;
        
        int randomIndex = Random.Range(0, collectibleMaterials.Length);
        _materialCache[collectible] = collectibleMaterials[randomIndex];
        collectible.GetComponent<MeshRenderer>().material = _materialCache[collectible];
    }

    public void ReturnToPool(GameObject collectible)
    {
        if (collectible == null || !_activeCollectibles.Contains(collectible)) return;

        collectible.SetActive(false);
        _activeCollectibles.Remove(collectible);
        _collectiblePool.Enqueue(collectible);

        // Reset physics
        if (collectible.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ExpandPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            _collectiblePool.Enqueue(CreateCollectible());
        }
    }

    private void ClearCollectibles()
    {
        foreach (GameObject collectible in _activeCollectibles)
        {
            collectible.SetActive(false);
            _collectiblePool.Enqueue(collectible);
        }
        _activeCollectibles.Clear();
    }

    public void SetCollectiblesActive(bool active)
    {
        foreach (GameObject collectible in _activeCollectibles)
        {
            if (collectible != null)
            {
                collectible.SetActive(active);
            }
        }
    }

    private Vector3 GetRandomPosition()
    {
        Vector3 center = transform.position;
        return center + new Vector3(
            Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
            center.y + 0.5f,
            Random.Range(-spawnArea.z / 2, spawnArea.z / 2)
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, spawnArea);
    }

    // GameState integration
    public void HandleGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                SpawnCollectibles();
                break;
            case GameState.Paused:
                SetCollectiblesActive(false);
                break;
            case GameState.Win:
            case GameState.Lose:
                ClearCollectibles();
                break;
        }
    }
}