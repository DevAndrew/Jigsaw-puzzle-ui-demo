using System;
using Cysharp.Threading.Tasks;
using JigsawPrototype.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text coinsText;

        [Header("Preview")]
        [SerializeField] private RawImage previewImage;

        [Header("Pieces")]
        [SerializeField] private Button pieces36Button;
        [SerializeField] private Button pieces64Button;
        [SerializeField] private Button pieces100Button;

        [Header("CTA")]
        [SerializeField] private Button startFreeButton;
        [SerializeField] private Button startCoinsButton;
        [SerializeField] private Button startAdButton;
        [SerializeField] private TMP_Text startCoinsLabel;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text errorText;

        [Header("Animation (optional)")]
        [SerializeField] private DoTweenDialogAnimator animator;

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
        private Vector2 _previewBoundsSize;

        private void Awake()
        {
            animator ??= GetComponent<DoTweenDialogAnimator>();

            _onPieces36 = () => PiecesSelected?.Invoke(PiecesPreset.P36);
            _onPieces64 = () => PiecesSelected?.Invoke(PiecesPreset.P64);
            _onPieces100 = () => PiecesSelected?.Invoke(PiecesPreset.P100);
            _onStartFree = () => StartFreeRequested?.Invoke();
            _onStartCoins = () => StartCoinsRequested?.Invoke();
            _onStartAd = () => StartAdRequested?.Invoke();

            if (previewImage != null)
            {
                _previewBoundsSize = previewImage.rectTransform.sizeDelta;
            }
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(OnClose);
            pieces36Button.onClick.AddListener(_onPieces36);
            pieces64Button.onClick.AddListener(_onPieces64);
            pieces100Button.onClick.AddListener(_onPieces100);
            startFreeButton.onClick.AddListener(_onStartFree);
            startCoinsButton.onClick.AddListener(_onStartCoins);
            startAdButton.onClick.AddListener(_onStartAd);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(OnClose);
            pieces36Button.onClick.RemoveListener(_onPieces36);
            pieces64Button.onClick.RemoveListener(_onPieces64);
            pieces100Button.onClick.RemoveListener(_onPieces100);
            startFreeButton.onClick.RemoveListener(_onStartFree);
            startCoinsButton.onClick.RemoveListener(_onStartCoins);
            startAdButton.onClick.RemoveListener(_onStartAd);
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

            if (animator != null)
            {
                await animator.PlayShowAsync();
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

            if (animator != null)
            {
                await animator.PlayHideAsync();
            }

            gameObject.SetActive(false);
            Hidden?.Invoke();
        }

        public void SetCoins(int coins)
        {
            coinsText.text = coins.ToString();
        }

        public void SetPreview(Texture2D texture)
        {
            if (previewImage == null) return;

            previewImage.texture = texture;
            previewImage.color = Color.white;
            FitPreviewToBounds(texture);
        }

        public void SetPreviewLoading()
        {
            if (previewImage == null) return;

            var rt = previewImage.rectTransform;
            if (_previewBoundsSize.x <= 0f || _previewBoundsSize.y <= 0f)
            {
                _previewBoundsSize = rt.sizeDelta;
            }

            rt.sizeDelta = _previewBoundsSize;
            previewImage.texture = null;
            previewImage.color = new Color(1f, 1f, 1f, 0f);
        }

        public void SetPiecesSelected(PiecesPreset preset)
        {
            SetSelected(pieces36Button, preset == PiecesPreset.P36);
            SetSelected(pieces64Button, preset == PiecesPreset.P64);
            SetSelected(pieces100Button, preset == PiecesPreset.P100);
        }

        public void SetCoinsCost(int cost)
        {
            if (startCoinsLabel != null)
            {
                startCoinsLabel.text = cost.ToString();
            }
        }

        public void SetInteractable(bool value)
        {
            pieces36Button.interactable = value;
            pieces64Button.interactable = value;
            pieces100Button.interactable = value;
            startFreeButton.interactable = value;
            startCoinsButton.interactable = value;
            startAdButton.interactable = value;
            closeButton.interactable = value;
        }

        public void SetStatus(string text)
        {
            statusText.text = text ?? "";
        }

        public void SetError(string text)
        {
            errorText.text = text ?? "";
        }

        private static void SetSelected(Selectable selectable, bool selected)
        {
            var img = selectable.targetGraphic as Image;
            if (img == null) return;
            img.color = selected ? new Color(0.65f, 0.7f, 0.95f) : new Color(0.92f, 0.92f, 0.92f);
        }

        private void FitPreviewToBounds(Texture texture)
        {
            if (previewImage == null) return;

            var rt = previewImage.rectTransform;
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
    }
}

