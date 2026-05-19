using UnityEngine;
using DG.Tweening;

namespace GabrielBissonnette.Primo
{
    public class DialoguePanelBouncer : MonoBehaviour
    {
        [Header("Bounce Settings")]
        // 【修改】把起始缩放改小一点 (0.8)，让弹跳效果非常夸张，方便测试是否生效
        [SerializeField] private float bounceStartScale = 0.8f; 
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease easeType = Ease.OutBack;

        private Vector3 originalScale;

        void Awake()
        {
            originalScale = transform.localScale; 
        }

        public void PlayBounceEffect()
        {
            transform.DOKill();
            transform.localScale = originalScale * bounceStartScale;
            
            // 【修改】加入了 .SetUpdate(true)，这样即使游戏暂停，UI 照样能弹跳
            transform.DOScale(originalScale, animationDuration).SetEase(easeType).SetUpdate(true);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = originalScale;
        }
    }
}