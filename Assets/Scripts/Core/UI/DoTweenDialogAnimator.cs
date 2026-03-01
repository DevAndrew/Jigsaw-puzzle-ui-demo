using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    public enum DialogAnimationResult
    {
        Completed = 0,
        Canceled = 1,
    }

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

        private Tween _tween;
        private UniTaskCompletionSource<DialogAnimationResult> _currentAnimationTcs;
        private CancellationTokenRegistration _currentCancellationRegistration;
        private Action _currentCompletionCallback;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (content == null) content = transform as RectTransform;
        }

        private void OnDisable()
        {
            CompleteCurrentAnimation(DialogAnimationResult.Canceled, killTween: true, invokeCompletionCallback: false);
        }

        public UniTask<DialogAnimationResult> PlayShowAsync(Action onShown = null, CancellationToken cancellationToken = default)
        {
            canvasGroup.alpha = 0f;
            if (content != null) content.localScale = showFromScale;

            return PlayAsync(
                duration: showDuration,
                fadeTarget: 1f,
                scaleTarget: Vector3.one,
                scaleEase: showEase,
                onCompleted: onShown,
                cancellationToken: cancellationToken);
        }

        public UniTask<DialogAnimationResult> PlayHideAsync(Action onHidden = null, CancellationToken cancellationToken = default)
        {
            return PlayAsync(
                duration: hideDuration,
                fadeTarget: 0f,
                scaleTarget: hideToScale,
                scaleEase: hideEase,
                onCompleted: onHidden,
                cancellationToken: cancellationToken);
        }

        private UniTask<DialogAnimationResult> PlayAsync(
            float duration,
            float fadeTarget,
            Vector3 scaleTarget,
            Ease scaleEase,
            Action onCompleted,
            CancellationToken cancellationToken)
        {
            CompleteCurrentAnimation(DialogAnimationResult.Canceled, killTween: true, invokeCompletionCallback: false);

            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromResult(DialogAnimationResult.Canceled);
            }

            var tcs = new UniTaskCompletionSource<DialogAnimationResult>();
            _currentAnimationTcs = tcs;
            _currentCompletionCallback = onCompleted;

            if (cancellationToken.CanBeCanceled)
            {
                _currentCancellationRegistration = cancellationToken.Register(() =>
                {
                    CompleteCurrentAnimation(DialogAnimationResult.Canceled, killTween: true, invokeCompletionCallback: false);
                });
            }

            var seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(fadeTarget, duration).SetEase(Ease.Linear));

            if (content != null)
            {
                seq.Join(content.DOScale(scaleTarget, duration).SetEase(scaleEase));
            }

            _tween = seq.OnComplete(() =>
            {
                CompleteCurrentAnimation(DialogAnimationResult.Completed, killTween: false, invokeCompletionCallback: true);
            });

            return tcs.Task;
        }

        private void CompleteCurrentAnimation(DialogAnimationResult result, bool killTween, bool invokeCompletionCallback)
        {
            var tween = _tween;
            _tween = null;

            if (killTween && tween != null && tween.active)
            {
                tween.Kill(complete: false);
            }

            _currentCancellationRegistration.Dispose();
            _currentCancellationRegistration = default;

            var callback = _currentCompletionCallback;
            _currentCompletionCallback = null;

            var tcs = _currentAnimationTcs;
            _currentAnimationTcs = null;

            if (invokeCompletionCallback && result == DialogAnimationResult.Completed)
            {
                callback?.Invoke();
            }

            tcs?.TrySetResult(result);
        }
    }
}

