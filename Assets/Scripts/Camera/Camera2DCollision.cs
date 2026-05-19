using UnityEngine;
using Cinemachine;

public class Camera2DCollision : MonoBehaviour
{
    public Transform target;           // 跟随的目标（角色）
    public float smoothTime = 0.2f;    // 平滑跟随时间
    public float collisionRadius = 0.5f; // 检测半径（略小于相机视口宽度的一半）
    public LayerMask wallLayer;        // 墙壁层（2D）

    private CinemachineVirtualCamera vcam;
    private Vector3 velocity = Vector3.zero;
    private Camera mainCamera;

    void Start()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // 理想位置 = 目标位置 + 偏移（可根据需求调整）
        Vector3 desiredPosition = target.position + new Vector3(0, 0, -10);
        
        // 检测从相机到目标的方向上是否有墙
        Vector2 direction = (target.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, target.position);
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, collisionRadius, direction, distance, wallLayer);

        if (hit.collider != null)
        {
            // 如果检测到墙，把相机位置推到墙的前面
            desiredPosition = hit.point - direction * collisionRadius;
            desiredPosition.z = -10;
        }

        // 平滑移动到最终位置
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}