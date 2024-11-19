using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(PlayerController))]
public class PlayerMovementBoundary : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private float edgeThreshold = 0.1f;

    private Camera mainCamera;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private CinemachineConfiner2D confiner;

    private void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
    }

    private void FixedUpdate()
    {
        if (!enabled || confiner.BoundingShape2D == null) return;

        Vector2 currentPosition = transform.position;
        Vector2 nextPosition = GetNextPosition();
        Vector2 restrictedPosition = RestrictPositionToCameraBounds(nextPosition);

        if (restrictedPosition != nextPosition)
        {
            // 현재 이동 방향 계산
            Vector2 moveDirection = (nextPosition - currentPosition).normalized;
            Vector2 restrictedDirection = Vector2.zero;

            // X축과 Y축 각각에 대해 제한 여부 확인
            if (Mathf.Approximately(restrictedPosition.x, nextPosition.x))
                restrictedDirection.x = moveDirection.x;
            if (Mathf.Approximately(restrictedPosition.y, nextPosition.y))
                restrictedDirection.y = moveDirection.y;

            // 제한되지 않은 방향으로만 이동 허용
            Vector2 allowedVelocity = Vector2.Scale(rb.linearVelocity, restrictedDirection);
            rb.linearVelocity = allowedVelocity;

            // 위치 보정
            transform.position = restrictedPosition;
        }
    }

    private Vector2 GetNextPosition()
    {
        return rb.position + rb.linearVelocity * Time.fixedDeltaTime;
    }

    private Vector2 RestrictPositionToCameraBounds(Vector2 position)
    {
        float cameraHeight = 2f * Camera.main.orthographicSize;
        float cameraWidth = cameraHeight * Camera.main.aspect;
        Vector2 cameraHalfSize = new Vector2(cameraWidth / 2f, cameraHeight / 2f);
        Bounds confinerBounds = confiner.BoundingShape2D.bounds;

        Vector2 restrictedPosition = position;

        float minX = confinerBounds.min.x + cameraHalfSize.x;
        float maxX = confinerBounds.max.x - cameraHalfSize.x;
        restrictedPosition.x = Mathf.Clamp(position.x, minX, maxX);

        float minY = confinerBounds.min.y + cameraHalfSize.y;
        float maxY = confinerBounds.max.y - cameraHalfSize.y;
        restrictedPosition.y = Mathf.Clamp(position.y, minY, maxY);

        return restrictedPosition;
    }
}