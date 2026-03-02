using System;
using UnityEngine;
using UnityEngine.UI;

namespace JigsawPrototype.Features.Home.Presentation
{
    public sealed class PuzzleTileView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _previewImage;

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

        public void SetPreview(Sprite sprite, Color placeholderColor)
        {
            if (_previewImage == null) return;

            _previewImage.sprite = sprite;
            _previewImage.color = sprite == null ? placeholderColor : Color.white;
            _previewImage.preserveAspect = true;
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

