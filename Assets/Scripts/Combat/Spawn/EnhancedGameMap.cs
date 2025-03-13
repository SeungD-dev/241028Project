using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class EnhancedGameMap : MonoBehaviour
{
    [Header("Map Components")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Spawn Settings")]
    [SerializeField] private bool useFixedSpawnPoints = false;
    [SerializeField] private List<Transform> manualSpawnPoints = new List<Transform>();
    [SerializeField] private int randomSpawnPointsCount = 50;
    [SerializeField] private float edgeSpawnChance = 0.7f; // �����ڸ����� ������ Ȯ��
    [SerializeField] private float minDistanceFromPlayer = 8f; // �÷��̾�κ��� �ּ� �Ÿ�

    // ĳ�̵� ���� ��ġ��
    private List<Vector2> cachedFloorPositions = new List<Vector2>();
    private List<Vector2> cachedEdgePositions = new List<Vector2>();
    private BoundsInt mapBounds;
    private Vector2 mapSize;

    private Transform playerTransform;

    private void Awake()
    {
        InitializeMapBounds();
    }

    private void Start()
    {
        // �÷��̾� ���� ���
        if (GameManager.Instance != null)
        {
            playerTransform = GameManager.Instance.PlayerTransform;
        }

        // ���� ��ġ ĳ��
        CacheSpawnPositions();
    }

    private void InitializeMapBounds()
    {
        if (floorTilemap != null)
        {
            mapBounds = floorTilemap.cellBounds;
            CalculateMapSize();
        }
        else if (wallTilemap != null)
        {
            mapBounds = wallTilemap.cellBounds;
            CalculateMapSize();
        }
    }

    private void CalculateMapSize()
    {
        Vector3Int size = new Vector3Int(mapBounds.size.x, mapBounds.size.y, mapBounds.size.z);

        mapSize = new Vector2(
            size.x * floorTilemap.layoutGrid.cellSize.x,
            size.y * floorTilemap.layoutGrid.cellSize.y);
    }

    // ������ ����� ��ġ �̸� ����ϰ� ĳ��
    private void CacheSpawnPositions()
    {
        if (floorTilemap == null) return;

        cachedFloorPositions.Clear();
        cachedEdgePositions.Clear();

        // ��� ��ȿ�� �ٴ� Ÿ�� ��ġ ����
        BoundsInt bounds = floorTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);

                if (floorTilemap.HasTile(cellPos) && !IsPositionColliding(floorTilemap.GetCellCenterWorld(cellPos)))
                {
                    Vector3 worldPos = floorTilemap.GetCellCenterWorld(cellPos);

                    // �����ڸ� Ÿ�� ���� Ȯ��
                    bool isEdgeTile = IsEdgeTile(cellPos);

                    if (isEdgeTile)
                    {
                        cachedEdgePositions.Add(worldPos);
                    }
                    else
                    {
                        cachedFloorPositions.Add(worldPos);
                    }
                }
            }
        }

        // ������ �� �̻��̸� �����ϰ� ����
        ShuffleAndLimitPositions(cachedFloorPositions, randomSpawnPointsCount / 2);
        ShuffleAndLimitPositions(cachedEdgePositions, randomSpawnPointsCount / 2);

        Debug.Log($"Cached {cachedFloorPositions.Count} floor positions and {cachedEdgePositions.Count} edge positions for spawning");
    }

    // ����Ʈ ���� ũ�� ����
    private void ShuffleAndLimitPositions(List<Vector2> positions, int limit)
    {
        // �Ǽ�-������ ����
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2 temp = positions[i];
            positions[i] = positions[j];
            positions[j] = temp;
        }

        // ũ�� ����
        if (positions.Count > limit)
        {
            positions.RemoveRange(limit, positions.Count - limit);
        }
    }

    // Ÿ���� �����ڸ����� Ȯ��
    private bool IsEdgeTile(Vector3Int cellPos)
    {
        // �����¿� ���� Ÿ�� �� �ϳ��� Ÿ���� ������ �����ڸ��� �Ǵ�
        Vector3Int[] neighbors = {
            new Vector3Int(cellPos.x + 1, cellPos.y, 0),
            new Vector3Int(cellPos.x - 1, cellPos.y, 0),
            new Vector3Int(cellPos.x, cellPos.y + 1, 0),
            new Vector3Int(cellPos.x, cellPos.y - 1, 0)
        };

        foreach (var neighbor in neighbors)
        {
            if (!floorTilemap.HasTile(neighbor) ||
                (wallTilemap != null && wallTilemap.HasTile(neighbor)))
            {
                return true;
            }
        }

        return false;
    }

    // ���� ��ġ ��� - ���̺� �� ��Ȳ�� ���� �پ��� ��� ����
    public Vector2 GetSpawnPosition(bool forceEdgeSpawn = false)
    {
        // ���� ���� ����Ʈ ��� ���
        if (useFixedSpawnPoints && manualSpawnPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, manualSpawnPoints.Count);
            return manualSpawnPoints[randomIndex].position;
        }

        // ���� ���� ��ġ ���� (�����ڸ� vs ����)
        bool useEdgeSpawn = forceEdgeSpawn || Random.value < edgeSpawnChance;

        // �ĺ� ��ġ ����Ʈ
        List<Vector2> candidatePositions = useEdgeSpawn ? cachedEdgePositions : cachedFloorPositions;

        // ��ġ�� ������ �ٸ� ����Ʈ ���
        if (candidatePositions.Count == 0)
        {
            candidatePositions = useEdgeSpawn ? cachedFloorPositions : cachedEdgePositions;
        }

        // �׷��� ������ ���� ��ġ ��ȯ
        if (candidatePositions.Count == 0)
        {
            return GetFallbackSpawnPosition();
        }

        // �÷��̾���� �Ÿ� ����Ͽ� ��ġ ����
        if (playerTransform != null)
        {
            // ������ ��ġ�� ���͸�
            List<Vector2> validPositions = new List<Vector2>();
            Vector2 playerPos = playerTransform.position;

            foreach (Vector2 pos in candidatePositions)
            {
                if (Vector2.Distance(pos, playerPos) >= minDistanceFromPlayer)
                {
                    validPositions.Add(pos);
                }
            }

            // ������ ��ġ�� ������ �� �߿��� ����
            if (validPositions.Count > 0)
            {
                return validPositions[Random.Range(0, validPositions.Count)];
            }
        }

        // ������ �����ϴ� ��ġ�� ������ ���� ����
        return candidatePositions[Random.Range(0, candidatePositions.Count)];
    }

    // ����: ������ ���� ��ġ ����
    private Vector2 GetFallbackSpawnPosition()
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        // �� �����ڸ����� ���� ��ġ
        int side = Random.Range(0, 4);
        Vector2 position;

        switch (side)
        {
            case 0: // ���
                position = new Vector2(Random.Range(-halfWidth + 1f, halfWidth - 1f), halfHeight - 1f);
                break;
            case 1: // ����
                position = new Vector2(halfWidth - 1f, Random.Range(-halfHeight + 1f, halfHeight - 1f));
                break;
            case 2: // �ϴ�
                position = new Vector2(Random.Range(-halfWidth + 1f, halfWidth - 1f), -halfHeight + 1f);
                break;
            case 3: // ����
                position = new Vector2(-halfWidth + 1f, Random.Range(-halfHeight + 1f, halfHeight - 1f));
                break;
            default:
                position = Vector2.zero;
                break;
        }

        return position;
    }

    // �ش� ��ġ�� ���� �浹�ϴ��� Ȯ��
    public bool IsPositionColliding(Vector2 worldPosition)
    {
        if (wallTilemap == null) return false;

        Vector3Int cellPosition = wallTilemap.WorldToCell(worldPosition);
        return wallTilemap.HasTile(cellPosition);
    }

    // �� �����̰� ��̷ο� ������ ���� �߰� �޼���

    // �÷��̾� �ֺ����� ���� (���� ���� � ���)
    public Vector2 GetPositionAroundPlayer(float minDistance, float maxDistance)
    {
        if (playerTransform == null) return Vector2.zero;

        Vector2 playerPos = playerTransform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minDistance, maxDistance);

        Vector2 offset = new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );

        Vector2 position = playerPos + offset;

        // �� ���η� ����
        position.x = Mathf.Clamp(position.x, -mapSize.x / 2 + 1f, mapSize.x / 2 - 1f);
        position.y = Mathf.Clamp(position.y, -mapSize.y / 2 + 1f, mapSize.y / 2 - 1f);

        return position;
    }

    // Ư�� ���⿡�� ���� �� �����ϱ� (���ݴ� � ���)
    public List<Vector2> GetPositionsInDirection(Vector2 direction, int count, float spacing)
    {
        List<Vector2> positions = new List<Vector2>();
        direction = direction.normalized;

        // �� ��� ���
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        // �� �����ڸ����� ������ ã��
        Vector2 startPoint;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // ��/�� ����
            float x = direction.x > 0 ? -halfWidth + 1f : halfWidth - 1f;
            float t = (direction.x > 0 ? halfWidth * 2 : -halfWidth * 2) / direction.x;
            float y = Random.Range(-halfHeight + 1f, halfHeight - 1f);
            startPoint = new Vector2(x, y);
        }
        else
        {
            // ��/�� ����
            float y = direction.y > 0 ? -halfHeight + 1f : halfHeight - 1f;
            float t = (direction.y > 0 ? halfHeight * 2 : -halfHeight * 2) / direction.y;
            float x = Random.Range(-halfWidth + 1f, halfWidth - 1f);
            startPoint = new Vector2(x, y);
        }

        // ���⿡ ���� ���� ��ġ ���
        for (int i = 0; i < count; i++)
        {
            Vector2 position = startPoint + direction * i * spacing;

            // �� ���η� ����
            if (IsPositionInMap(position))
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    // ��ġ�� �� ���ο� �ִ��� Ȯ��
    public bool IsPositionInMap(Vector2 position)
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        return position.x >= -halfWidth && position.x <= halfWidth &&
               position.y >= -halfHeight && position.y <= halfHeight;
    }
}