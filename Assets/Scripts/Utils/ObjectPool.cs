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
    private Dictionary<GameObject, string> objectToTagMap; // ������Ʈ�� �±׿� �����ϴ� ��ųʸ�

    // Scene ���������� ����ȭ�ϱ� ���� �ɼ�
    [Tooltip("true: Ǯ ������Ʈ�� ���� �������� �и�, false: ���� ��Ĵ�� ���� ���� ����")]
    [SerializeField] private bool useOptimizedHierarchy = true;
    [Tooltip("false�� ������ ������Ʈ Ǯ�� ������� ����� �� ������ ������ ���˴ϴ�")]

    // Ǯ �����̳� ĳ�� (���� ��Ŀ����� ���)
    private Dictionary<string, Transform> poolContainers;

    // ��Ȱ��ȭ�� ������Ʈ�� �θ� Transform (����ȭ ��忡���� ������� ����)
    private Transform inactiveObjectsParent;

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
        objectToTagMap = new Dictionary<GameObject, string>();
        poolContainers = new Dictionary<string, Transform>();

        // ���� ��Ŀ����� �ڱ� �ڽ��� �θ�� ����
        inactiveObjectsParent = transform;

        foreach (Pool pool in pools)
        {
            CreateNewPool(pool);
        }
    }

    private void CreateNewPool(Pool poolConfig)
    {
        Queue<GameObject> objectPool = new Queue<GameObject>();

        // ����ȭ ��忡���� ���� �����̳ʸ� �������� ����
        if (!useOptimizedHierarchy)
        {
            GameObject poolContainer = new GameObject($"Pool-{poolConfig.tag}");
            poolContainer.transform.SetParent(transform);
            poolContainers[poolConfig.tag] = poolContainer.transform;
        }

        for (int i = 0; i < poolConfig.initialSize; i++)
        {
            GameObject obj = CreateNewPoolObject(poolConfig.prefab, poolConfig.tag);
            objectPool.Enqueue(obj);
        }

        poolDictionary[poolConfig.tag] = objectPool;
        poolConfigs[poolConfig.tag] = poolConfig;
    }

    private GameObject CreateNewPoolObject(GameObject prefab, string tag)
    {
        GameObject obj = Instantiate(prefab);

        // ����ȭ ��忡���� ���� �������� ������ �и�
        if (useOptimizedHierarchy)
        {
            // Scene���� ��Ʈ ������ �����Ͽ� Transform ���� �ּ�ȭ
            obj.transform.SetParent(null);
        }
        else
        {
            // ���� ��Ĵ�� Ǯ �����̳��� �ڽ����� ����
            obj.transform.SetParent(poolContainers[tag]);
        }

        objectToTagMap[obj] = tag; // ������Ʈ�� �±� ���� ����
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
                GameObject newObj = CreateNewPoolObject(config.prefab, tag);
                pool.Enqueue(newObj);
            }

            objectToSpawn = CreateNewPoolObject(config.prefab, tag);
        }
        else
        {
            objectToSpawn = pool.Dequeue();
            if (objectToSpawn == null) // Ǯ�� �ִ� ������Ʈ�� �ı��� ���
            {
                objectToSpawn = CreateNewPoolObject(config.prefab, tag);
            }
        }

        // ������Ʈ ��ġ �� ȸ�� ���� (SetActive ���� �����Ͽ� ���ʿ��� �̺�Ʈ ȣ�� ����)
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject objectToReturn)
    {
        // �±� ������ ���� ������Ʈ�� ���� Ǯ ã��
        if (!objectToTagMap.TryGetValue(objectToReturn, out string tag))
        {
            Debug.LogWarning($"Object not managed by pool: {objectToReturn.name}");
            return;
        }

        ReturnToPool(tag, objectToReturn);
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        objectToReturn.SetActive(false);

        // ����ȭ ���: ���� �������� ������ �и�
        if (!useOptimizedHierarchy)
        {
            // ���� ��Ŀ����� Ǯ �����̳��� �ڽ����� ����
            objectToReturn.transform.SetParent(poolContainers[tag]);
        }

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

        // ��Ȱ��ȭ�� ������Ʈ ��(ť�� �ִ� ������Ʈ)
        int inactiveCount = poolDictionary[tag].Count;

        // Ȱ��ȭ�� ������Ʈ �� ���
        int activeCount = 0;
        foreach (var pair in objectToTagMap)
        {
            if (pair.Value == tag && pair.Key.activeSelf)
            {
                activeCount++;
            }
        }

        return activeCount + inactiveCount;
    }

    public void ReturnAllObjectsToPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        // ��� ������Ʈ �� �ش� �±׸� ���� Ȱ��ȭ�� ������Ʈ�� ã�� ��ȯ
        List<GameObject> objectsToReturn = new List<GameObject>();

        foreach (var pair in objectToTagMap)
        {
            if (pair.Value == tag && pair.Key.activeSelf)
            {
                objectsToReturn.Add(pair.Key);
            }
        }

        foreach (var obj in objectsToReturn)
        {
            ReturnToPool(tag, obj);
        }
    }

    // ���� API���� ȣȯ���� ���� �޼���
    public void ReturnToPool(string tag, GameObject objectToReturn, bool forceParenting = false)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        objectToReturn.SetActive(false);

        // forceParenting�� true�̸� �׻� Ǯ �����̳��� �ڽ����� ���� (���� ��İ� ȣȯ�� ����)
        if (forceParenting || !useOptimizedHierarchy)
        {
            // ���� Ǯ �����̳ʰ� �ִ� ��쿡�� �θ�� ����
            if (poolContainers.TryGetValue(tag, out Transform container))
            {
                objectToReturn.transform.SetParent(container);
            }
        }

        poolDictionary[tag].Enqueue(objectToReturn);
    }
}