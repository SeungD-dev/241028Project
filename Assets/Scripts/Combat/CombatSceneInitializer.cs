using UnityEngine;

public class CombatSceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ShopController shopController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private GameOverController gameOverController;

    private void Start()
    {
        // �� �ε�
        if (MapManager.Instance != null)
        {
            GameMap map = MapManager.Instance.LoadMap();

            // �� �ε� �� �÷��̾� ��ġ ����
            if (map != null && playerStats != null)
            {
                Vector2 startPosition = MapManager.Instance.GetPlayerStartPosition();
                playerStats.transform.position = startPosition;
            }
        }

        // ���� �Ŵ����� ������Ʈ ���� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCombatSceneReferences(
                playerStats,
                shopController,
                combatController,
                gameOverController
            );
        }
    }
}