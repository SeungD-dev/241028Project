using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize;
        [Tooltip("Ǯ�� �ִ� ũ�� (0 = ������)")]
        public int maxSize;
        [Tooltip("Ǯ�� ����� �� �� ���� ������ ������Ʈ ��")]
        public int growSize = 5;
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolConfigs;

    private static ObjectPool instance;
    public static ObjectPool Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolConfigs = new Dictionary<string, Pool>();

        foreach (Pool pool in pools)
        {
            CreateNewPool(pool);
        }
    }

    private void CreateNewPool(Pool poolConfig)
    {
        Queue<GameObject> objectPool = new Queue<GameObject>();
        GameObject poolContainer = new GameObject($"Pool-{poolConfig.tag}");
        poolContainer.transform.SetParent(transform);

        for (int i = 0; i < poolConfig.initialSize; i++)
        {
            GameObject obj = CreateNewPoolObject(poolConfig.prefab, poolContainer.transform);
            objectPool.Enqueue(obj);
        }

        poolDictionary[poolConfig.tag] = objectPool;
        poolConfigs[poolConfig.tag] = poolConfig;
    }

    private GameObject CreateNewPoolObject(GameObject prefab, Transform parent)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.SetActive(false);
        return obj;
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[tag];
        Pool config = poolConfigs[tag];

        GameObject objectToSpawn;

        // Ǯ�� ����� �� ó��
        if (pool.Count == 0)
        {
            // Ǯ Ȯ��
            GameObject poolContainer = transform.Find($"Pool-{tag}")?.gameObject;
            if (poolContainer == null)
            {
                poolContainer = new GameObject($"Pool-{tag}");
                poolContainer.transform.SetParent(transform);
            }

            // �ִ� ũ�� üũ
            int currentTotalSize = CountActiveAndInactiveObjects(tag);
            int growSize = config.growSize;

            if (config.maxSize > 0)
            {
                // �ִ� ũ�� ������ �ִ� ���
                growSize = Mathf.Min(growSize, config.maxSize - currentTotalSize);
                if (growSize <= 0)
                {
                    Debug.LogWarning($"Pool {tag} has reached its maximum size of {config.maxSize}");
                    return null;
                }
            }

            // �� ������Ʈ�� ����
            for (int i = 0; i < growSize - 1; i++) // -1 because we'll create one more below
            {
                GameObject newObj = CreateNewPoolObject(config.prefab, poolContainer.transform);
                pool.Enqueue(newObj);
            }

            objectToSpawn = CreateNewPoolObject(config.prefab, poolContainer.transform);
        }
        else
        {
            objectToSpawn = pool.Dequeue();
            if (objectToSpawn == null) // Ǯ�� �ִ� ������Ʈ�� �ı��� ���
            {
                GameObject poolContainer = transform.Find($"Pool-{tag}")?.gameObject;
                objectToSpawn = CreateNewPoolObject(config.prefab, poolContainer ? poolContainer.transform : transform);
            }
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }

    public void CreatePool(string tag, GameObject prefab, int size)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} already exists.");
            return;
        }

        Pool newPool = new Pool
        {
            tag = tag,
            prefab = prefab,
            initialSize = size,
            maxSize = 0, // ������
            growSize = 5
        };

        CreateNewPool(newPool);
    }

    private int CountActiveAndInactiveObjects(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) return 0;

        int count = poolDictionary[tag].Count;
        Transform poolContainer = transform.Find($"Pool-{tag}");
        if (poolContainer != null)
        {
            count += poolContainer.childCount;
        }
        return count;
    }
}