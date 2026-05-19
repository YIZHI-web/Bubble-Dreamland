using UnityEngine;
using DG.Tweening;
using System;

public enum BlockType
{
    Normal,
    Slime,
    Sticky,
    Teleport
}

public class BlockController : MonoBehaviour
{
    [Header("Block Properties")]
    public BlockType type = BlockType.Normal;
    
    [HideInInspector] public bool isExitPortal = false;

    [Header("Specific Settings")]
    public float bounceForce = 25f;        
    public float stickyMultiplier = 0.4f;  
    public float stickyDuration = 1.0f;    

    [Header("Animation Timing (时序控制)")]
    public float spawnDuration = 0.3f;     
    public float vanishDelay = 0.5f;       
    public float vanishDuration = 0.2f;    

    [Header("References")]
    public Transform visuals;

    public static event Action<BlockController> OnBlockTouchedWithData; 

    private bool hasBeenTouched = false;

    private void Awake()
    {
        if (type == BlockType.Teleport)
        {
            spawnDuration = 0.1f;
            vanishDelay = 0.05f;
            vanishDuration = 0.1f;
        }
    }

    private void OnEnable()
    {
        hasBeenTouched = false;
        visuals.DOKill(); 
        visuals.localScale = Vector3.zero; 
        
        visuals.DOScale(Vector3.one, spawnDuration).SetEase(type == BlockType.Teleport ? Ease.OutQuad : Ease.OutBack);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasBeenTouched || type == BlockType.Teleport) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 hitNormal = collision.GetContact(0).normal;
            
            Vector2 dirToPlayer = (collision.transform.position - transform.position).normalized;
            if (Vector2.Dot(hitNormal, dirToPlayer) < 0)
            {
                hitNormal = -hitNormal;
            }
            
            TriggerBlock(collision.gameObject.GetComponent<PlayerController>(), hitNormal);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasBeenTouched || type != BlockType.Teleport) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            TriggerBlock(collision.GetComponent<PlayerController>(), Vector2.up);
        }
    }

    private void TriggerBlock(PlayerController player, Vector2 contactNormal)
    {
        hasBeenTouched = true;

        OnBlockTouchedWithData?.Invoke(this);

        if (player != null)
        {
            switch (type)
            {
                case BlockType.Slime:
                    player.BounceInDirection(contactNormal * bounceForce);
                    visuals.DOScale(new Vector3(1.3f, 0.5f, 1f), 0.1f).OnComplete(() => {
                        visuals.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutElastic);
                    });
                    break;
                
                case BlockType.Sticky:
                    player.ApplyStickyEffect(stickyMultiplier, stickyDuration);
                    visuals.DOScale(new Vector3(1.1f, 0.9f, 1f), 0.2f);
                    break;

                case BlockType.Teleport:
                    if (!isExitPortal)
                    {
                        Vector3 dest = LevelSequenceManager.Instance.GetNextTeleportTargetPosition();
                        player.TeleportTo(dest);
                    }
                    break;
            }
        }

        visuals.DOScale(Vector3.zero, vanishDuration)
               .SetDelay(vanishDelay)
               .SetEase(Ease.InBack)
               .OnComplete(() => {
                   LevelSequenceManager.Instance.DestroyOrRecycleBlock(this.gameObject);
               });
    }
}