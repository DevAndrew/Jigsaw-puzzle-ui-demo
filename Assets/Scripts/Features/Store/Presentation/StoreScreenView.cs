using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JigsawPrototype.Features.Store.Presentation
{
    public sealed class StoreScreenView : MonoBehaviour
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _buySmallButton;
        [SerializeField] private Button _buyBigButton;
        [SerializeField] private TMP_Text _coinsText;

        public event Action BackRequested;
        public event Action BuySmallRequested;
        public event Action BuyBigRequested;

        private void OnEnable()
        {
            _backButton.onClick.AddListener(OnBack);
            _buySmallButton.onClick.AddListener(OnBuySmall);
            _buyBigButton.onClick.AddListener(OnBuyBig);
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(OnBack);
            _buySmallButton.onClick.RemoveListener(OnBuySmall);
            _buyBigButton.onClick.RemoveListener(OnBuyBig);
        }

        public void SetCoins(int coins)
        {
            _coinsText.text = coins.ToString();
        }

        private void OnBack() => BackRequested?.Invoke();
        private void OnBuySmall() => BuySmallRequested?.Invoke();
        private void OnBuyBig() => BuyBigRequested?.Invoke();
    }
}

