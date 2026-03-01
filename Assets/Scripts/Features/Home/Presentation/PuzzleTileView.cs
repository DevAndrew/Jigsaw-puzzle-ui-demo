using System;
using UnityEngine;
using UnityEngine.UI;

namespace JigsawPrototype.UI.Screens
{
    public sealed class PuzzleTileView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private AspectRatioFitter previewAspectFitter;

        private string _puzzleId;
        private Action<string> _onSelected;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (previewImage == null)
            {
                previewImage = GetComponentInChildren<RawImage>(includeInactive: true);
            }

            if (previewAspectFitter == null && previewImage != null)
            {
                previewAspectFitter = previewImage.GetComponent<AspectRatioFitter>();
            }
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
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
            if (previewImage == null) return;

            previewImage.texture = texture;
            previewImage.color = texture == null ? placeholderColor : Color.white;

            if (previewAspectFitter != null && texture != null && texture.height > 0)
            {
                previewAspectFitter.aspectRatio = (float)texture.width / texture.height;
            }
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private void OnClicked()
        {
            _onSelected?.Invoke(_puzzleId);
        }
    }
}

