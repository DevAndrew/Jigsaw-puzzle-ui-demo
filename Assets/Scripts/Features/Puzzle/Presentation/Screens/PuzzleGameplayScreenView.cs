using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JigsawPrototype.Features.Puzzle.Presentation.Screens
{
    public sealed class PuzzleGameplayScreenView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _backButton;

        public event Action BackRequested;

        private void OnEnable()
        {
            _backButton.onClick.AddListener(OnBack);
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(OnBack);
        }

        public void SetTitle(string text)
        {
            _titleText.text = text ?? "";
        }

        private void OnBack()
        {
            BackRequested?.Invoke();
        }
    }
}
