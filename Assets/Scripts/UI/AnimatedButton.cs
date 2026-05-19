using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace GabrielBissonnette.Primo
{
    public class AnimatedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float clickScale = 0.9f;
        [SerializeField] private float animationDuration = 0.2f;
        [SerializeField] private Ease easeType = Ease.OutBack;

        private Vector3 originalScale;

        void Start()
        {
            originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ScaleTo(originalScale * hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ScaleTo(originalScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ScaleTo(originalScale * clickScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ScaleTo(originalScale * hoverScale);
        }

        public void OnSelect(BaseEventData eventData)
        {
            ScaleTo(originalScale * hoverScale);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            ScaleTo(originalScale);
        }

        private void ScaleTo(Vector3 targetScale)
        {
            transform.DOKill();
            transform.DOScale(targetScale, animationDuration).SetEase(easeType);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = originalScale;
        }
    }
}