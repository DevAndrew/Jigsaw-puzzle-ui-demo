using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    /// <summary>
    /// Minimal popup/dialog stack with an optional modal background.
    /// This component exists as a separate script file so Unity can add it via "Add Component".
    /// </summary>
    public sealed class DialogHost : MonoBehaviour
    {
        private struct DialogEntry
        {
            public IUIView View;
            public bool Modal;
        }

        [Header("Parents")]
        [SerializeField] private Transform dialogsParent;

        [Header("Modal background")]
        [SerializeField] private CanvasGroup modalBackground;
        [SerializeField] private float modalFadeInDuration = 0.12f;
        [SerializeField] private float modalFadeOutDuration = 0.1f;

        private Tween _modalTween;

        private readonly Stack<DialogEntry> _stack = new Stack<DialogEntry>();

        public bool HasAny => _stack.Count > 0;
        public IUIView Top => _stack.Count > 0 ? _stack.Peek().View : null;

        private void Awake()
        {
            if (dialogsParent == null) dialogsParent = transform;

            if (modalBackground != null)
            {
                _modalTween?.Kill(complete: false);
                _modalTween = null;
                modalBackground.alpha = 0f;
                modalBackground.blocksRaycasts = false;
                modalBackground.interactable = false;
                modalBackground.gameObject.SetActive(false);
            }
        }

        public UniTask PushAsync(IUIView dialog, bool modal = true)
        {
            if (dialog == null) return UniTask.CompletedTask;

            // Ensure hierarchy.
            if (dialog is MonoBehaviour mb && dialogsParent != null)
            {
                mb.transform.SetParent(dialogsParent, worldPositionStays: false);
                mb.transform.SetAsLastSibling();
            }

            var current = Top;
            if (ReferenceEquals(current, dialog))
            {
                // Update modal flag for the current top, then ensure it is shown.
                var e = _stack.Pop();
                e.Modal = modal;
                _stack.Push(e);
                return EnsureShownTopAsync();
            }

            current?.SetInteractable(false);

            _stack.Push(new DialogEntry { View = dialog, Modal = modal });
            return EnsureShownTopAsync();
        }

        public void Push(IUIView dialog, bool modal = true) => PushAsync(dialog, modal).Forget();

        public async UniTask PopAsync()
        {
            if (_stack.Count == 0) return;

            var top = _stack.Pop().View;
            await top.HideAsync();

            Top?.SetInteractable(true);
            UpdateModalBackground();
        }

        public void Pop() => PopAsync().Forget();

        public UniTask HideAsync(IUIView dialog)
        {
            if (dialog == null) return UniTask.CompletedTask;
            if (ReferenceEquals(Top, dialog))
            {
                return PopAsync();
            }

            // Remove the dialog from the stack if present (keep order).
            if (_stack.Count > 0)
            {
                var temp = new Stack<DialogEntry>(_stack.Count);
                var found = false;

                while (_stack.Count > 0)
                {
                    var e = _stack.Pop();
                    if (!ReferenceEquals(e.View, dialog))
                    {
                        temp.Push(e);
                    }
                    else
                    {
                        found = true;
                    }
                }

                while (temp.Count > 0) _stack.Push(temp.Pop());

                if (found)
                {
                    UpdateModalBackground();
                }
            }

            return dialog.HideAsync();
        }

        public void Hide(IUIView dialog) => HideAsync(dialog).Forget();

        private async UniTask EnsureShownTopAsync()
        {
            UpdateModalBackground();

            var top = Top;
            if (top == null) return;

            top.SetInteractable(true);
            await top.ShowAsync();
        }

        private void UpdateModalBackground()
        {
            if (modalBackground == null) return;

            var shouldShow = HasAny && _stack.Peek().Modal;
            modalBackground.interactable = false;

            // Keep background behind the topmost dialog.
            if (dialogsParent != null && modalBackground.transform.parent == dialogsParent && Top is MonoBehaviour topMb)
            {
                var topIndex = topMb.transform.GetSiblingIndex();
                modalBackground.transform.SetSiblingIndex(Mathf.Max(0, topIndex - 1));
            }

            _modalTween?.Kill(complete: false);
            _modalTween = null;

            if (shouldShow)
            {
                modalBackground.gameObject.SetActive(true);
                modalBackground.blocksRaycasts = true;

                if (modalFadeInDuration <= 0f)
                {
                    modalBackground.alpha = 1f;
                    return;
                }

                _modalTween = modalBackground.DOFade(1f, modalFadeInDuration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            }
            else
            {
                modalBackground.blocksRaycasts = false;

                if (!modalBackground.gameObject.activeSelf)
                {
                    modalBackground.alpha = 0f;
                    return;
                }

                if (modalFadeOutDuration <= 0f)
                {
                    modalBackground.alpha = 0f;
                    modalBackground.gameObject.SetActive(false);
                    return;
                }

                _modalTween = modalBackground.DOFade(0f, modalFadeOutDuration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (modalBackground != null)
                        {
                            modalBackground.gameObject.SetActive(false);
                        }
                    });
            }
        }
    }
}

