using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JigsawPrototype.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace JigsawPrototype.Features.Puzzle.Presentation.Dialogs
{
    public enum PiecesPreset
    {
        Unknown = 0,
        P36 = 36,
        P64 = 64,
        P100 = 100,
    }

    public sealed class PuzzleStartDialogView : MonoBehaviour, IUIView
    {
        [Header("Header")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _coinsText;
        
        [Header("Preview")]
        [SerializeField] private RawImage _previewImage;
        
        [Header("Pieces")]
        [SerializeField] private Button _pieces36Button; 
        [SerializeField] private Button _pieces64Button;
        [SerializeField] private Button _pieces100Button;
        
        [Header("CTA")]
        [SerializeField] private Button _startFreeButton;
        [SerializeField] private Button _startCoinsButton;
        [SerializeField] private Button _startAdButton;
        [SerializeField] private TMP_Text _startCoinsLabel;
        
        [Header("Status")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _errorText;
        
        [Header("Animation (optional)")]
        [SerializeField] private DoTweenDialogAnimator _animator;

        public event Action CloseRequested;
        public event Action<PiecesPreset> PiecesSelected;
        public event Action StartFreeRequested;
        public event Action StartCoinsRequested;
        public event Action StartAdRequested;

        public bool IsVisible => gameObject.activeSelf;

        public PiecesPreset InitialPiecesPreset => PiecesPreset.P64;

        public event Action Shown;
        public event Action Hidden;

        private UnityAction _onPieces36;
        private UnityAction _onPieces64;
        private UnityAction _onPieces100;
        private UnityAction _onStartFree;
        private UnityAction _onStartCoins;
        private UnityAction _onStartAd;
        private CancellationTokenSource _animationCts;
        private Vector2 _previewBoundsSize;

        private void Awake()
        {
            _onPieces36 = () => PiecesSelected?.Invoke(PiecesPreset.P36);
            _onPieces64 = () => PiecesSelected?.Invoke(PiecesPreset.P64);
            _onPieces100 = () => PiecesSelected?.Invoke(PiecesPreset.P100);
            _onStartFree = () => StartFreeRequested?.Invoke();
            _onStartCoins = () => StartCoinsRequested?.Invoke();
            _onStartAd = () => StartAdRequested?.Invoke();

            if (_previewImage != null)
            {
                _previewBoundsSize = _previewImage.rectTransform.sizeDelta;
            }
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnClose);
            _pieces36Button.onClick.AddListener(_onPieces36);
            _pieces64Button.onClick.AddListener(_onPieces64);
            _pieces100Button.onClick.AddListener(_onPieces100);
            _startFreeButton.onClick.AddListener(_onStartFree);
            _startCoinsButton.onClick.AddListener(_onStartCoins);
            _startAdButton.onClick.AddListener(_onStartAd);
        }

        private void OnDisable()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;

            _closeButton.onClick.RemoveListener(OnClose);
            _pieces36Button.onClick.RemoveListener(_onPieces36);
            _pieces64Button.onClick.RemoveListener(_onPieces64);
            _pieces100Button.onClick.RemoveListener(_onPieces100);
            _startFreeButton.onClick.RemoveListener(_onStartFree);
            _startCoinsButton.onClick.RemoveListener(_onStartCoins);
            _startAdButton.onClick.RemoveListener(_onStartAd);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void HideImmediate()
        {
            gameObject.SetActive(false);
            Hidden?.Invoke();
        }

        public async UniTask ShowAsync()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            var animationToken = RenewAnimationToken();

            if (_animator != null)
            {
                var result = await _animator.PlayShowAsync(cancellationToken: animationToken);
                if (result != DialogAnimationResult.Completed)
                {
                    return;
                }
            }

            Shown?.Invoke();
        }

        public async UniTask HideAsync()
        {
            if (!gameObject.activeSelf)
            {
                Hidden?.Invoke();
                return;
            }

            var animationToken = RenewAnimationToken();
            if (_animator != null)
            {
                var result = await _animator.PlayHideAsync(cancellationToken: animationToken);
                if (result != DialogAnimationResult.Completed)
                {
                    return;
                }
            }

            gameObject.SetActive(false);
            Hidden?.Invoke();
        }

        public void SetCoins(int coins)
        {
            _coinsText.text = coins.ToString();
        }

        public void SetPreview(Texture2D texture)
        {
            if (_previewImage == null) return;

            _previewImage.texture = texture;
            _previewImage.color = Color.white;
            FitPreviewToBounds(texture);
        }

        public void SetPreviewLoading()
        {
            if (_previewImage == null) return;

            var rt = _previewImage.rectTransform;
            if (_previewBoundsSize.x <= 0f || _previewBoundsSize.y <= 0f)
            {
                _previewBoundsSize = rt.sizeDelta;
            }

            rt.sizeDelta = _previewBoundsSize;
            _previewImage.texture = null;
            _previewImage.color = new Color(1f, 1f, 1f, 0f);
        }

        public void SetPiecesSelected(PiecesPreset preset)
        {
            SetSelected(_pieces36Button, preset == PiecesPreset.P36);
            SetSelected(_pieces64Button, preset == PiecesPreset.P64);
            SetSelected(_pieces100Button, preset == PiecesPreset.P100);
        }

        public void SetCoinsCost(int cost)
        {
            if (_startCoinsLabel != null)
            {
                _startCoinsLabel.text = cost.ToString();
            }
        }

        public void SetInteractable(bool value)
        {
            _pieces36Button.interactable = value;
            _pieces64Button.interactable = value;
            _pieces100Button.interactable = value;
            _startFreeButton.interactable = value;
            _startCoinsButton.interactable = value;
            _startAdButton.interactable = value;
            _closeButton.interactable = value;
        }

        public void SetStatus(string text)
        {
            _statusText.text = text ?? "";
        }

        public void SetError(string text)
        {
            _errorText.text = text ?? "";
        }

        private static void SetSelected(Selectable selectable, bool selected)
        {
            var img = selectable.targetGraphic as Image;
            if (img == null) return;
            img.color = selected ? new Color(0.65f, 0.7f, 0.95f) : new Color(0.92f, 0.92f, 0.92f);
        }

        private void FitPreviewToBounds(Texture texture)
        {
            if (_previewImage == null) return;

            var rt = _previewImage.rectTransform;
            if (_previewBoundsSize.x <= 0f || _previewBoundsSize.y <= 0f)
            {
                _previewBoundsSize = rt.sizeDelta;
            }

            if (texture == null || texture.width <= 0 || texture.height <= 0 ||
                _previewBoundsSize.x <= 0f || _previewBoundsSize.y <= 0f)
            {
                rt.sizeDelta = _previewBoundsSize;
                return;
            }

            var boundsAspect = _previewBoundsSize.x / _previewBoundsSize.y;
            var textureAspect = (float)texture.width / texture.height;

            float width;
            float height;
            if (textureAspect > boundsAspect)
            {
                width = _previewBoundsSize.x;
                height = width / textureAspect;
            }
            else
            {
                height = _previewBoundsSize.y;
                width = height * textureAspect;
            }

            rt.sizeDelta = new Vector2(width, height);
        }

        private void OnClose()
        {
            CloseRequested?.Invoke();
        }

        private CancellationToken RenewAnimationToken()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = new CancellationTokenSource();
            return _animationCts.Token;
        }
    }
}

