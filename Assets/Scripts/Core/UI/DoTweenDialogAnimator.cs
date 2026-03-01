using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    /// <summary>
    /// Lightweight show/hide animation helper for dialogs.
    /// This component exists as a separate script file so Unity can add it via "Add Component".
    /// </summary>
    public sealed class DoTweenDialogAnimator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform content;

        [Header("Show")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Vector3 showFromScale = new Vector3(0.9f, 0.9f, 0.9f);

        [Header("Hide")]
        [SerializeField] private float hideDuration = 0.15f;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private Vector3 hideToScale = new Vector3(0.9f, 0.9f, 0.9f);

        private Tween _tween;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (content == null) content = transform as RectTransform;
        }

        private void OnDisable()
        {
            _tween?.Kill(complete: false);
            _tween = null;
        }

        public UniTask PlayShowAsync(Action onShown = null)
        {
            _tween?.Kill(complete: false);

            var tcs = new UniTaskCompletionSource();
            canvasGroup.alpha = 0f;
            if (content != null) content.localScale = showFromScale;

            var seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.Linear));

            if (content != null)
            {
                seq.Join(content.DOScale(Vector3.one, showDuration).SetEase(showEase));
            }

            _tween = seq.OnComplete(() =>
            {
                onShown?.Invoke();
                tcs.TrySetResult();
            });

            return tcs.Task;
        }

        public UniTask PlayHideAsync(Action onHidden = null)
        {
            _tween?.Kill(complete: false);

            var tcs = new UniTaskCompletionSource();

            var seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.Linear));

            if (content != null)
            {
                seq.Join(content.DOScale(hideToScale, hideDuration).SetEase(hideEase));
            }

            _tween = seq.OnComplete(() =>
            {
                onHidden?.Invoke();
                tcs.TrySetResult();
            });

            return tcs.Task;
        }
    }
}

