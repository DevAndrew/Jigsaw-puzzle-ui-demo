using JigsawPrototype.Core.Services.Currency;
using JigsawPrototype.Core.UI;
using JigsawPrototype.Features.Puzzle.Presentation.Dialogs;

namespace JigsawPrototype.Features.Store.Presentation
{
    public sealed class StorePresenter
    {
        private readonly ICurrencyService _currency;
        private readonly ScreenStack _screens;
        private readonly PuzzleStartDialogView _dialog;
        private readonly DialogHost _dialogHost;

        private StoreScreenView _view;

        public StorePresenter(ICurrencyService currency, ScreenStack screens, PuzzleStartDialogView dialog, DialogHost dialogHost)
        {
            _currency = currency;
            _screens = screens;
            _dialog = dialog;
            _dialogHost = dialogHost;
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
            _currency.Add(2000);
            Return();
        }

        private void OnBuyBig()
        {
            _currency.Add(5000);
            Return();
        }

        private void OnBack()
        {
            Return();
        }

        private void Return()
        {
            _screens.Pop();
            _dialogHost.Push(_dialog, modal: true);
        }

        private void OnBalanceChanged(int coins)
        {
            _view?.SetCoins(coins);
        }
    }
}

