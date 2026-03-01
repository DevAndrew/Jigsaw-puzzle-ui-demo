using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JigsawPrototype.Features.Store.Presentation
{
    public sealed class StoreScreenView : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button buySmallButton;
        [SerializeField] private Button buyBigButton;
        [SerializeField] private TMP_Text coinsText;

        public event Action BackRequested;
        public event Action BuySmallRequested;
        public event Action BuyBigRequested;

        private void OnEnable()
        {
            backButton.onClick.AddListener(OnBack);
            buySmallButton.onClick.AddListener(OnBuySmall);
            buyBigButton.onClick.AddListener(OnBuyBig);
        }

        private void OnDisable()
        {
            backButton.onClick.RemoveListener(OnBack);
            buySmallButton.onClick.RemoveListener(OnBuySmall);
            buyBigButton.onClick.RemoveListener(OnBuyBig);
        }

        public void SetCoins(int coins)
        {
            coinsText.text = coins.ToString();
        }

        private void OnBack() => BackRequested?.Invoke();
        private void OnBuySmall() => BuySmallRequested?.Invoke();
        private void OnBuyBig() => BuyBigRequested?.Invoke();
    }
}

