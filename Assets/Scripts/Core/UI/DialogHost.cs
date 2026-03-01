using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JigsawPrototype.Core.Async;
using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    /// <summary>
    /// Simple dialog stack with optional modal background.
    /// Sequential operations only: Push / Pop / Hide are serialized internally.
    /// New calls cancel an in-flight transition (latest-wins).
    /// </summary>
    public sealed class DialogHost : MonoBehaviour
    {
        private struct DialogEntry
        {
            public IUIView View;
            public bool Modal;
        }
        
        [Header("Parents")]
        [SerializeField] private Transform _dialogsParent;
        
        [Header("Modal background")]
        [SerializeField] private CanvasGroup _modalBackground;
        [SerializeField] private float _modalFadeInDuration = 0.12f;
        [SerializeField] private float _modalFadeOutDuration = 0.10f;

       private readonly AsyncLock _transitionLock = new AsyncLock();
        private readonly List<DialogEntry> _stack = new();
        private Tween _modalTween;
 
        private CancellationTokenSource _transitionCts;

        public bool HasAny => _stack.Count > 0;
        public IUIView Top => _stack.Count > 0 ? _stack[^1].View : null;

        private void Awake()
        {
            if (_dialogsParent == null)
                _dialogsParent = transform;

            ResetModalBackgroundImmediate();
        }

        private void OnDestroy()
        {
            _modalTween?.Kill(false);
            _modalTween = null;

            _transitionCts?.Cancel();
            _transitionCts?.Dispose();
            _transitionCts = null;
        }

        public void Push(IUIView dialog, bool modal = true) => PushAsync(dialog, modal).Forget();

        public async UniTask PushAsync(IUIView dialog, bool modal = true)
        {
            if (dialog == null)
                return;

            var token = RenewTransitionToken();
            try
            {
                using (await _transitionLock.LockAsync(token))
                {
                    try
                    {
                        var currentTop = Top;

                        // Если уже наверху — просто обновим modal-флаг.
                        if (ReferenceEquals(currentTop, dialog))
                        {
                            var topEntry = _stack[^1];
                            topEntry.Modal = modal;
                            _stack[^1] = topEntry;

                            EnsureDialogParentAndOrder(dialog);
                            return;
                        }

                        // Если такой dialog уже есть где-то в стеке — удалим его старую позицию.
                        var existingIndex = IndexOf(dialog);
                        if (existingIndex >= 0)
                        {
                            _stack.RemoveAt(existingIndex);
                        }

                        currentTop?.SetInteractable(false);

                        EnsureDialogParentAndOrder(dialog);

                        if (!dialog.IsVisible)
                            await dialog.ShowAsync(token);

                        token.ThrowIfCancellationRequested();

                        _stack.Add(new DialogEntry
                        {
                            View = dialog,
                            Modal = modal
                        });
                    }
                    finally
                    {
                        ApplyInteractable();
                        UpdateModalBackground();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Latest-wins: ignore canceled transition.
            }
        }

        public void Pop() => PopAsync().Forget();

        public async UniTask PopAsync()
        {
            if (_stack.Count == 0)
                return;

            var token = RenewTransitionToken();
            try
            {
                using (await _transitionLock.LockAsync(token))
                {
                    try
                    {
                        if (_stack.Count == 0)
                            return;

                        var top = _stack[^1];

                        if (top.View != null && top.View.IsVisible)
                            await top.View.HideAsync(token);

                        token.ThrowIfCancellationRequested();

                        // Commit stack mutation only after hide completes (or is canceled).
                        _stack.RemoveAt(_stack.Count - 1);
                    }
                    finally
                    {
                        ApplyInteractable();
                        UpdateModalBackground();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Latest-wins: ignore canceled transition.
            }
        }

        public void Hide(IUIView dialog) => HideAsync(dialog).Forget();

        public async UniTask HideAsync(IUIView dialog)
        {
            if (dialog == null)
                return;

            var token = RenewTransitionToken();
            try
            {
                using (await _transitionLock.LockAsync(token))
                {
                    try
                    {
                        var index = IndexOf(dialog);
                        if (index < 0)
                            return;

                        // Stack semantics: hide/remove the target dialog and anything above it.
                        for (var i = _stack.Count - 1; i >= index; i--)
                        {
                            var entry = _stack[i];
                            if (entry.View != null && entry.View.IsVisible)
                                await entry.View.HideAsync(token);

                            token.ThrowIfCancellationRequested();

                            // Commit stack mutation only after hide completes (or is canceled).
                            _stack.RemoveAt(i);
                        }
                    }
                    finally
                    {
                        ApplyInteractable();
                        UpdateModalBackground();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Latest-wins: ignore canceled transition.
            }
        }

        private int IndexOf(IUIView dialog)
        {
            for (var i = 0; i < _stack.Count; i++)
            {
                if (ReferenceEquals(_stack[i].View, dialog))
                    return i;
            }

            return -1;
        }

        private void EnsureDialogParentAndOrder(IUIView dialog)
        {
            if (dialog is not MonoBehaviour mb || _dialogsParent == null)
                return;

            var tr = mb.transform;

            if (tr.parent != _dialogsParent)
                tr.SetParent(_dialogsParent, false);

            tr.SetAsLastSibling();
        }

        private void ApplyInteractable()
        {
            for (var i = 0; i < _stack.Count; i++)
            {
                _stack[i].View?.SetInteractable(false);
            }

            if (_stack.Count > 0)
            {
                _stack[^1].View?.SetInteractable(true);
            }
        }

        private void ResetModalBackgroundImmediate()
        {
            if (_modalBackground == null)
                return;

            _modalTween?.Kill(false);
            _modalTween = null;

            _modalBackground.alpha = 0f;
            _modalBackground.blocksRaycasts = false;
            _modalBackground.interactable = false;
            _modalBackground.gameObject.SetActive(false);
        }

        private void UpdateModalBackground()
        {
            if (_modalBackground == null)
                return;

            var shouldShow = _stack.Count > 0 && _stack[^1].Modal;
            _modalBackground.interactable = false;

            PlaceModalBackgroundBeforeTopDialog();

            _modalTween?.Kill(false);
            _modalTween = null;

            if (shouldShow)
            {
                _modalBackground.gameObject.SetActive(true);
                _modalBackground.blocksRaycasts = true;

                if (_modalFadeInDuration <= 0f)
                {
                    _modalBackground.alpha = 1f;
                    return;
                }

                _modalTween = _modalBackground
                    .DOFade(1f, _modalFadeInDuration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            }
            else
            {
                _modalBackground.blocksRaycasts = false;

                if (!_modalBackground.gameObject.activeSelf)
                {
                    _modalBackground.alpha = 0f;
                    return;
                }

                if (_modalFadeOutDuration <= 0f)
                {
                    _modalBackground.alpha = 0f;
                    _modalBackground.gameObject.SetActive(false);
                    return;
                }

                _modalTween = _modalBackground
                    .DOFade(0f, _modalFadeOutDuration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (_modalBackground != null)
                            _modalBackground.gameObject.SetActive(false);
                    });
            }
        }

        private void PlaceModalBackgroundBeforeTopDialog()
        {
            if (_modalBackground == null || _dialogsParent == null || Top is not MonoBehaviour topMb)
                return;

            var backgroundTransform = _modalBackground.transform;

            if (backgroundTransform.parent != _dialogsParent)
                backgroundTransform.SetParent(_dialogsParent, false);

            var topIndex = topMb.transform.GetSiblingIndex();
            backgroundTransform.SetSiblingIndex(Mathf.Max(0, topIndex - 1));
        }

        private CancellationToken RenewTransitionToken()
        {
            _transitionCts?.Cancel();
            _transitionCts?.Dispose();
            _transitionCts = new CancellationTokenSource();
            return _transitionCts.Token;
        }
    }
}