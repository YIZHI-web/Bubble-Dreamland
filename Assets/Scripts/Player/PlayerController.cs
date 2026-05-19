using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class PlayerController : MonoBehaviour
{
    [Header("Movement & Jump")]
    public float jumpForce = 20f;
    public float moveSpeed = 8f;
    private float horizontalInput;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float checkRadius = 0.25f;

    [Header("Feel Polish (手感优化)")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    public float fallMultiplier = 2.5f;

    [Header("Death & Respawn")]
    public Transform respawnPoint;
    public float respawnDelay = 1f;
    public LayerMask deathLayer;
    public GameObject deathVFX;           // 死亡时生成的VFX预制体

    [Header("References")]
    public Transform visuals;
    public Rigidbody2D rb;
    public Collider2D playerCollider;
    
    [Header("Animation (动画)")]
    public Animator animator;             // 添加Animator引用

    [Header("Mobile Controls (移动端设置)")]
    public float maxDragDistance = 150f;
    private int leftTouchId = -1;
    private Vector2 leftTouchStartPos;

    [Header("State (状态)")]
    public bool isInputLocked = false;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isGrounded;
    private bool wasGrounded;             // 记录上一帧是否在地面，用于检测落地瞬间
    private float defaultGravity;
    private float defaultMoveSpeed;
    private float speedResetTimer;
    private bool isDead = false;
    private Vector3 initialSpawnPosition;
    private float inputLockTimer;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        defaultGravity = rb.gravityScale;
        defaultMoveSpeed = moveSpeed;
        initialSpawnPosition = transform.position;

        if (respawnPoint == null)
        {
            respawnPoint = transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        HandleMobileTouch();

        // 记录上一帧的接地状态
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (speedResetTimer > 0)
        {
            speedResetTimer -= Time.deltaTime;
            if (speedResetTimer <= 0) moveSpeed = defaultMoveSpeed;
        }

        if (isInputLocked)
        {
            horizontalInput = 0f;
        }

        if (inputLockTimer > 0)
        {
            inputLockTimer -= Time.deltaTime;
        }
        else
        {
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        }

        if (rb.velocity.y < 0)
        {
            rb.gravityScale = defaultGravity * fallMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravity;
        }

        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            Jump();
        }

        AlignVisualsWithSlope();
        UpdateAnimations(); // 更新动画状态
    }

    // --- 新增：动画状态更新逻辑 ---
    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("MoveSpeed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);

        // 2. 检测落地瞬间：恢复动画播放速度，播放Jump动画的收尾(落地)部分
        if (!wasGrounded && isGrounded)
        {
            animator.speed = 1f;
        }

        // 3. 处理角色视觉翻转 (基于输入方向)
        if (horizontalInput > 0.01f)
        {
            visuals.localScale = new Vector3(Mathf.Abs(visuals.localScale.x), visuals.localScale.y, visuals.localScale.z);
        }
        else if (horizontalInput < -0.01f)
        {
            visuals.localScale = new Vector3(-Mathf.Abs(visuals.localScale.x), visuals.localScale.y, visuals.localScale.z);
        }
    }

    // --- 新增：供Animation Event调用的方法 ---
    public void PauseJumpAnimation()
    {
        // 只有在角色处于空中时才暂停动画
        if (!isGrounded)
        {
            animator.speed = 0f; 
        }
    }

    private void HandleMobileTouch()
    {
        if (isInputLocked)
        {
            leftTouchId = -1;
            horizontalInput = 0f;
            return;
        }

        bool leftTouchActive = false;

        foreach (var touch in Touch.activeTouches)
        {
            if (touch.screenPosition.x < Screen.width / 2f)
            {
                leftTouchActive = true;

                if (touch.phase == TouchPhase.Began)
                {
                    leftTouchStartPos = touch.screenPosition;
                    leftTouchId = touch.touchId;
                }
                else if (touch.touchId == leftTouchId && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                {
                    float dragDeltaX = touch.screenPosition.x - leftTouchStartPos.x;
                    horizontalInput = Mathf.Clamp(dragDeltaX / maxDragDistance, -1f, 1f);
                }
            }
            else
            {
                if (touch.phase == TouchPhase.Began)
                {
                    jumpBufferCounter = jumpBufferTime;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (rb.velocity.y > 0)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
                        coyoteTimeCounter = 0f;
                    }
                }
            }
        }

        if (!leftTouchActive && leftTouchId != -1)
        {
            horizontalInput = 0f;
            leftTouchId = -1;
        }
    }

    public void OnMove(InputValue value)
    {
        if (isDead || isInputLocked) return;
        horizontalInput = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value)
    {
        if (isDead || isInputLocked) return;
        if (value.isPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else if (rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            coyoteTimeCounter = 0f;
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        TriggerJumpAnimation();
    }

    public void Bounce(float force)
    {
        rb.velocity = new Vector2(rb.velocity.x, force);
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        TriggerJumpAnimation();
    }

    public void BounceInDirection(Vector2 bounceVelocity)
    {
        rb.velocity = bounceVelocity;
        
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        if (Mathf.Abs(bounceVelocity.x) > 0.1f)
        {
            inputLockTimer = 0.2f;
        }

        TriggerJumpAnimation();
    }

    // --- 新增：统一起跳动画逻辑 ---
    private void TriggerJumpAnimation()
    {
        if (animator != null)
        {
            animator.speed = 1f; // 确保起跳时动画速度正常（防止在空中连续弹跳时卡死）
            animator.SetTrigger("JumpTrigger");
        }
    }

    public void ApplyStickyEffect(float speedMultiplier, float duration)
    {
        moveSpeed = defaultMoveSpeed * speedMultiplier;
        speedResetTimer = duration;
    }

    public void TeleportTo(Vector3 targetPosition)
    {
        transform.position = targetPosition + Vector3.up * 1f;
        rb.velocity = Vector2.zero;
    }

    private void AlignVisualsWithSlope()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);

        Debug.DrawRay(transform.position, Vector2.down * 1.5f, Color.red);

        if (hit)
        {
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            visuals.rotation = Quaternion.Slerp(visuals.rotation, targetRotation, Time.deltaTime * 15f);
        }
        else
        {
            visuals.rotation = Quaternion.Slerp(visuals.rotation, Quaternion.identity, Time.deltaTime * 10f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (((1 << collision.gameObject.layer) & deathLayer) != 0)
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (((1 << other.gameObject.layer) & deathLayer) != 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        // 死亡时确保动画速度恢复，避免以0倍速死亡卡住状态
        if (animator != null) animator.speed = 1f; 

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeCamera(5f, 0.4f);
        }

        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

        visuals.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            Invoke(nameof(Respawn), respawnDelay);
        });

        Debug.Log("玩家死亡！");
    }

    private void Respawn()
    {
        Vector3 spawnPos = respawnPoint != null ? respawnPoint.position : initialSpawnPosition;
        transform.position = spawnPos;

        isDead = false;
        rb.gravityScale = defaultGravity;
        rb.velocity = Vector2.zero;
        horizontalInput = 0f;

        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        // 修复：复活时保持原有的X轴朝向，而不是强行设为 Vector3.one
        float signX = visuals.localScale.x != 0 ? Mathf.Sign(visuals.localScale.x) : 1f;
        visuals.localScale = Vector3.zero;
        visuals.DOScale(new Vector3(signX, 1f, 1f), 0.3f).SetEase(Ease.OutBack);

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        if (animator != null) animator.Play("Idle"); // 复活时重置为Idle状态

        if (LevelSequenceManager.Instance != null)
        {
            LevelSequenceManager.Instance.ResetAllBlocks();
        }

        Debug.Log("玩家复活！");
    }

    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void LockInput()
    {
        isInputLocked = true;
        horizontalInput = 0f;
    }

    public void UnlockInput()
    {
        isInputLocked = false;
    }
}