using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.InputSystem;

public class ButterflyController : MonoBehaviour
{
    public enum State { Follow, Pathing, Wait }
    public State currentState = State.Follow;

    [Header("Follow Settings")]
    public Transform player;
    public Vector3 followOffset = new Vector3(-1.5f, 1.5f, 0);
    public float followSmoothTime = 0.3f;

    [Header("Float Effect")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;

    [Header("Pathing Settings")]
    public float pathSpeed = 3f;
    public Ease pathEase = Ease.Linear;
    public bool useCurvedPath = true;

    [Header("Visuals & VFX (视觉与特效)")]
    public Transform visuals;                 // 挂载 SpriteRenderer 的子物体
    public GameObject smallPulseVFXPrefab;    // 蝴蝶等待点击时的小脉冲特效
    public GameObject bigPulseVFXPrefab;      // 抵达终点释放的大脉冲特效

    [Header("Interaction Settings")]
    public LayerMask clickableLayers;         // 设置可点击的层，避免点到其他物体

    public event Action OnButterflyClicked;
    
    private Transform[] currentPath;
    private Vector3 velocity = Vector3.zero;
    private Tween pathTween;
    private float lastDirectionX = 0f;

    private bool isPulsing = false;
    private Action currentPathCallback;
    private GameObject activeSmallPulseVFX;   // 缓存生成的小脉冲实例

    void Update()
    {
        HandleMovementAndFloating();
        HandleInteraction(); // 独立的点击检测逻辑
    }

    private void HandleMovementAndFloating()
    {
        Vector3 floatOffset = new Vector3(0, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0);

        switch (currentState)
        {
            case State.Follow:
                if (player != null)
                {
                    Vector3 targetPos = player.position + followOffset + floatOffset;
                    transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, followSmoothTime);
                    FlipSprite(player.position.x - transform.position.x);
                }
                break;

            case State.Wait:
                transform.position += floatOffset * Time.deltaTime * floatSpeed;
                break;
        }
    }

    private void HandleInteraction()
    {
        bool isClicked = false;
        Vector2 inputScreenPos = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            isClicked = true;
            inputScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isClicked = true;
            inputScreenPos = Mouse.current.position.ReadValue();
        }

        if (isClicked)
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(inputScreenPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos, clickableLayers);

            if (hit != null)
            {
                Debug.Log($"<color=cyan>鼠标点到了物体: {hit.gameObject.name}</color>");

                ButterflyController clickedButterfly = hit.GetComponentInParent<ButterflyController>();

                if (clickedButterfly == this)
                {
                    Debug.Log("<color=green>成功触发蝴蝶交互！</color>");
                    OnButterflyClicked?.Invoke();
                }
            }
        }
    }

    public void StartPath(Transform[] path, Action onComplete = null)
    {
        if (path == null || path.Length == 0) return;

        currentPathCallback = onComplete;
        currentPath = path;
        currentState = State.Pathing;

        transform.DOKill();
        pathTween = null;

        Vector3[] waypoints = new Vector3[currentPath.Length];
        for (int i = 0; i < currentPath.Length; i++)
        {
            waypoints[i] = currentPath[i].position;
        }

        if (useCurvedPath && waypoints.Length > 2)
        {
            pathTween = transform.DOPath(waypoints, pathSpeed, PathType.CatmullRom)
                .SetEase(pathEase)
                .SetAutoKill(true)
                .OnComplete(OnPathComplete)
                .OnWaypointChange(OnWaypointChanged);
        }
        else
        {
            pathTween = transform.DOPath(waypoints, pathSpeed, PathType.Linear)
                .SetEase(pathEase)
                .SetAutoKill(true)
                .OnComplete(OnPathComplete)
                .OnWaypointChange(OnWaypointChanged);
        }

        if (waypoints.Length > 0)
        {
            lastDirectionX = waypoints[0].x - transform.position.x;
            FlipSprite(lastDirectionX);
        }
    }

    private void OnWaypointChanged(int index)
    {
        if (currentPath != null && index < currentPath.Length && index >= 0)
        {
            Vector3 waypointPos = currentPath[index].position;
            lastDirectionX = waypointPos.x - transform.position.x;
            FlipSprite(lastDirectionX);
        }
    }

    private void OnPathComplete()
    {
        currentState = State.Wait;
        pathTween = null;

        currentPathCallback?.Invoke();
        currentPathCallback = null;
    }

    public void ResumeFollow()
    {
        transform.DOKill();
        pathTween = null;
        velocity = Vector3.zero;
        currentState = State.Follow;
    }

    private void FlipSprite(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.01f || visuals == null) return;

        // 仅翻转 visuals 的 X 轴，不再干扰缩放逻辑
        float signX = directionX > 0 ? -1f : 1f;
        visuals.localScale = new Vector3(signX, 1f, 1f); 
    }

    // --- 修改：使用 VFX 替代 DOTween 控制小脉冲 ---
    public void StartPulse()
    {
        if (isPulsing) return;
        isPulsing = true;

        if (smallPulseVFXPrefab != null && activeSmallPulseVFX == null)
        {
            // 将小脉冲特效作为蝴蝶的子物体生成，跟随蝴蝶移动
            activeSmallPulseVFX = Instantiate(smallPulseVFXPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    public void StopPulse()
    {
        if (!isPulsing) return;
        isPulsing = false;

        if (activeSmallPulseVFX != null)
        {
            Destroy(activeSmallPulseVFX);
            activeSmallPulseVFX = null;
        }
    }

    // --- 修改：使用 VFX 替代 DOTween 控制大脉冲爆发 ---
    public void PlayBigPulse(Action onComplete)
    {
        StopPulse(); 
        
        if (bigPulseVFXPrefab != null)
        {
            // 生成大脉冲特效（不作为子物体，防止特效因蝴蝶飞走而被带走）
            Instantiate(bigPulseVFXPrefab, transform.position, Quaternion.identity);
        }

        // 使用 DOVirtual.DelayedCall 模拟一个特效播放时间（比如 0.5 秒）
        // 0.5秒后通知关卡管理器去拉下开关
        DOVirtual.DelayedCall(0.5f, () => {
            onComplete?.Invoke();
        });
    }

    void OnDestroy()
    {
        transform.DOKill();
        
        if (activeSmallPulseVFX != null)
        {
            Destroy(activeSmallPulseVFX);
            activeSmallPulseVFX = null;
        }
    }
}