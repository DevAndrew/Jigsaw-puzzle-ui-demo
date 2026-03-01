using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JigsawPrototype.Features.Puzzle.Presentation.Screens
{
    public sealed class PuzzleStartedScreenView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button backButton;

        public event Action BackRequested;

        private void OnEnable()
        {
            backButton.onClick.AddListener(OnBack);
        }

        private void OnDisable()
        {
            backButton.onClick.RemoveListener(OnBack);
        }

        public void SetTitle(string text)
        {
            titleText.text = text ?? "";
        }

        private void OnBack()
        {
            BackRequested?.Invoke();
        }
    }
}

