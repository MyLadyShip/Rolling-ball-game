using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ParticleEffectPool : MonoBehaviour
{
    public static ParticleEffectPool Instance { get; private set; }

    [Header("Particle Settings")]
    [SerializeField] private GameObject[] particlePrefabs;
    [SerializeField] private int initialPoolSize = 10;

    private Dictionary<GameObject, Queue<GameObject>> _pool;
    private Dictionary<GameObject, ParticleSystem> _particleSystemCache = new Dictionary<GameObject, ParticleSystem>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        _pool = new Dictionary<GameObject, Queue<GameObject>>();

        foreach (var prefab in particlePrefabs)
        {
            if (prefab.GetComponent<ParticleSystem>() == null)
            {
                Debug.LogError($"Prefab {prefab.name} is missing a ParticleSystem component!");
                continue;
            }

            Queue<GameObject> objectQueue = new Queue<GameObject>();
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject obj = CreatePooledObject(prefab);
                objectQueue.Enqueue(obj);
            }
            _pool[prefab] = objectQueue;
        }
    }

    private GameObject CreatePooledObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        obj.transform.SetParent(transform); // Organize hierarchy
        return obj;
    }

    // Spawns a random particle effect at a position and auto-returns it to the pool
    public void PlayRandomParticleEffect(Vector3 position)
    {
        if (particlePrefabs.Length == 0)
        {
            Debug.LogWarning("No particle prefabs assigned!");
            return;
        }

        int randomIndex = Random.Range(0, particlePrefabs.Length);
        PlayParticleEffect(particlePrefabs[randomIndex], position);
    }
    // Spawns a specific particle effect and auto-returns it to the pool
    public void PlayParticleEffect(GameObject prefab, Vector3 position)
    {
        if (!_pool.ContainsKey(prefab))
        {
            Debug.LogWarning($"Prefab {prefab.name} not registered in pool!");
            return;
        }

        GameObject obj = null;
        if (_pool[prefab].Count > 0)
        {
            obj = _pool[prefab].Dequeue();
        }
        else
        {
            obj = CreatePooledObject(prefab);
        }

        obj.transform.position = position;
        obj.SetActive(true);

        if (!_particleSystemCache.TryGetValue(obj, out ParticleSystem ps))
        {
            ps = obj.GetComponent<ParticleSystem>();
            if (ps != null) _particleSystemCache[obj] = ps;
        }

        if (ps != null)
        {
            ps.Play();
            StartCoroutine(ReturnToPoolAfterPlay(ps, prefab, obj));
        }
        else
        {
            Debug.LogWarning($"Missing ParticleSystem on {obj.name}");
            ReturnToPool(prefab, obj);
        }
    }
    private IEnumerator ReturnToPoolAfterPlay(ParticleSystem ps, GameObject prefab, GameObject obj)
    {
        // Wait until the particle system finishes playing
        yield return new WaitWhile(() => ps.isPlaying);
        ReturnToPool(prefab, obj);
    }

    private void ReturnToPool(GameObject prefab, GameObject obj)
    {
        if (_pool.ContainsKey(prefab))
        {
            obj.SetActive(false);
            _pool[prefab].Enqueue(obj);
        }
        else
        {
            Debug.LogWarning($"Prefab {prefab.name} not registered in pool!");
        }
    }

    private void ExpandPool(GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject obj = CreatePooledObject(prefab);
            _pool[prefab].Enqueue(obj);
        }
    }
}