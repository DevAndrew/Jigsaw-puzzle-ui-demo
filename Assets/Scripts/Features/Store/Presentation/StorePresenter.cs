using JigsawPrototype.Core.Services.Currency;
using JigsawPrototype.Core.UI;
using JigsawPrototype.App;
using JigsawPrototype.Features.Puzzle.Presentation.Dialogs;
using Cysharp.Threading.Tasks;

namespace JigsawPrototype.Features.Store.Presentation
{
    public sealed class StorePresenter
    {
        private readonly ICurrencyService _currency;
        private readonly ScreenStack _screens;
        private readonly PuzzleStartPresenter _puzzleStartPresenter;

        private StoreScreenView _view;

        public StorePresenter(ICurrencyService currency, ScreenStack screens, PuzzleStartPresenter puzzleStartPresenter)
        {
            _currency = currency;
            _screens = screens;
            _puzzleStartPresenter = puzzleStartPresenter;
        }

        public void Bind(StoreScreenView view)
        {
            _view = view;
            _view.SetCoins(_currency.Balance);
            _view.BackRequested += OnBack;
            _view.BuySmallRequested += OnBuySmall;
            _view.BuyBigRequested += OnBuyBig;
            _currency.BalanceChanged += OnBalanceChanged;
        }

        public void Unbind()
        {
            if (_view == null) return;
            _view.BackRequested -= OnBack;
            _view.BuySmallRequested -= OnBuySmall;
            _view.BuyBigRequested -= OnBuyBig;
            _currency.BalanceChanged -= OnBalanceChanged;
            _view = null;
        }

        private void OnBuySmall()
        {
            _currency.Add(AppConstants.Economy.StoreSmallPackCoins);
            Return();
        }

        private void OnBuyBig()
        {
            _currency.Add(AppConstants.Economy.StoreBigPackCoins);
            Return();
        }

        private void OnBack()
        {
            Return();
        }

        private void Return()
        {
            ReturnAsync().Forget();
        }

        private async UniTask ReturnAsync()
        {
            _screens.Pop();
            await _puzzleStartPresenter.ReopenLastDialogAsync();
        }

        private void OnBalanceChanged(int coins)
        {
            _view?.SetCoins(coins);
        }
    }
}

