using System;
using UnityEngine;
using UnityEngine.UI;

namespace JigsawPrototype.Features.Home.Presentation
{
    public sealed class PuzzleTileView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private RawImage _previewImage;
        [SerializeField] private AspectRatioFitter _previewAspectFitter;

        private string _puzzleId;
        private Action<string> _onSelected;

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Bind(string puzzleId, Action<string> onSelected, Color placeholderColor)
        {
            _puzzleId = puzzleId ?? "";
            _onSelected = onSelected;
            SetPreview(null, placeholderColor);
            SetInteractable(true);
        }

        public void SetPreview(Texture2D texture, Color placeholderColor)
        {
            if (_previewImage == null) return;

            _previewImage.texture = texture;
            _previewImage.color = texture == null ? placeholderColor : Color.white;

            if (_previewAspectFitter != null && texture != null && texture.height > 0)
            {
                _previewAspectFitter.aspectRatio = (float)texture.width / texture.height;
            }
        }

        public void SetInteractable(bool value)
        {
            if (_button != null)
            {
                _button.interactable = value;
            }
        }

        private void OnClicked()
        {
            _onSelected?.Invoke(_puzzleId);
        }
    }
}

