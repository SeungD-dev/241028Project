using UnityEngine;

public class MapManager : MonoBehaviour
{
    private static MapManager instance;
    public static MapManager Instance => instance;

    [Header("Map Settings")]
    [SerializeField] private GameObject mapPrefabReference;
    [SerializeField] private string mapResourcePath = "Prefabs/Map";

    private GameMap currentMap;
    public GameMap CurrentMap => currentMap;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ������ �� ���ҽ� �ε�
    public GameMap LoadMap(string mapPath = null)
    {
        // ���� �� ����
        if (currentMap != null)
        {
            Destroy(currentMap.gameObject);
            currentMap = null;
        }

        GameObject mapInstance;

        // �̸� ������ �� ������ ��� (�� ȿ����)
        if (mapPrefabReference != null)
        {
            mapInstance = Instantiate(mapPrefabReference, Vector3.zero, Quaternion.identity);
        }
        else
        {
            // ����: Resources���� �� ������ �ε�
            string path = string.IsNullOrEmpty(mapPath) ? mapResourcePath : mapPath;
            GameObject mapPrefab = Resources.Load<GameObject>(path);

            if (mapPrefab == null)
            {
                Debug.LogError($"Failed to load map from path: {path}");
                return null;
            }

            mapInstance = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
        }

        // GameMap ������Ʈ ��������
        currentMap = mapInstance.GetComponent<GameMap>();

        if (currentMap == null)
        {
            Debug.LogError("Loaded map prefab does not have a GameMap component!");
            Destroy(mapInstance);
            return null;
        }

        Debug.Log($"Map loaded: {currentMap.MapName}");
        return currentMap;
    }

    // �÷��̾� ���� ��ġ ��������
    public Vector2 GetPlayerStartPosition()
    {
        if (currentMap == null)
        {
            Debug.LogWarning("No map loaded. Using default position.");
            return Vector2.zero;
        }

        // �� �߾ӿ� �÷��̾� ��ġ
        return Vector2.zero;
    }
}
