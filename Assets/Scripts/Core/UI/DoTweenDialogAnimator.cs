using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    /// <summary>
    /// Lightweight show/hide animation helper for dialogs.
    /// This component exists as a separate script file so Unity can add it via "Add Component".
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
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

        private CancellationTokenSource _animationCts;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (content == null) content = transform as RectTransform;
        }

        private void OnDisable()
        {
            CancelAnimation();
        }

        public UniTask PlayShowAsync(Action onShown = null, CancellationToken cancellationToken = default)
        {
            canvasGroup.alpha = 0f;
            if (content != null) content.localScale = showFromScale;

            return PlayAsync(showDuration, 1f, Vector3.one, showEase, onShown, cancellationToken);
        }

        public UniTask PlayHideAsync(Action onHidden = null, CancellationToken cancellationToken = default)
        {
            return PlayAsync(hideDuration, 0f, hideToScale, hideEase, onHidden, cancellationToken);
        }

        private async UniTask PlayAsync(
            float duration,
            float fadeTarget,
            Vector3 scaleTarget,
            Ease scaleEase,
            Action onCompleted,
            CancellationToken cancellationToken)
        {
            CancelAnimation();

            cancellationToken.ThrowIfCancellationRequested();

            var cts = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();
            _animationCts = cts;

            var seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(fadeTarget, duration).SetEase(Ease.Linear));

#pragma warning disable CS4014 // Sequence builder, not an awaitable call
            if (content != null)
                seq.Join(content.DOScale(scaleTarget, duration).SetEase(scaleEase));
#pragma warning restore CS4014

            try
            {
                await seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cts.Token);

                onCompleted?.Invoke();
            }
            finally
            {
                if (_animationCts == cts)
                    _animationCts = null;
                cts.Dispose();
            }
        }

        private void CancelAnimation()
        {
            var cts = _animationCts;
            _animationCts = null;
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}

