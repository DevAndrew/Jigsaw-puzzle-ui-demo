using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DoTweenDialogAnimator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _content;
        
        [Header("Show")]
        [SerializeField] private float _showDuration = 0.2f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Vector3 _showFromScale = new Vector3(0.9f, 0.9f, 0.9f);
        
        [Header("Hide")]
        [SerializeField] private float _hideDuration = 0.15f;
        [SerializeField] private Ease _hideEase = Ease.InBack;
        [SerializeField] private Vector3 _hideToScale = new Vector3(0.9f, 0.9f, 0.9f);

        private CancellationTokenSource _animationCts;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_content == null && !TryGetComponent(out _content))
            {
                Debug.LogError($"{nameof(DoTweenDialogAnimator)} requires RectTransform on {name}", this);
            }
        }

        private void OnDisable()
        {
            CancelAnimation();
        }

        public UniTask PlayShowAsync(Action onShown = null, CancellationToken cancellationToken = default)
        {
            _canvasGroup.alpha = 0f;
            if (_content != null) _content.localScale = _showFromScale;

            return PlayAsync(_showDuration, 1f, Vector3.one, _showEase, onShown, cancellationToken);
        }

        public UniTask PlayHideAsync(Action onHidden = null, CancellationToken cancellationToken = default)
        {
            return PlayAsync(_hideDuration, 0f, _hideToScale, _hideEase, onHidden, cancellationToken);
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
                .Append(_canvasGroup.DOFade(fadeTarget, duration).SetEase(Ease.Linear));

#pragma warning disable CS4014 // Sequence builder, not an awaitable call
            if (_content != null)
                seq.Join(_content.DOScale(scaleTarget, duration).SetEase(scaleEase));
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

