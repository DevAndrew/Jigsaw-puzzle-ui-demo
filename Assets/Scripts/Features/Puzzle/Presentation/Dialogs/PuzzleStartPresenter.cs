using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JigsawPrototype.App;
using JigsawPrototype.Core.Services.Ads;
using JigsawPrototype.Core.Services.Currency;
using JigsawPrototype.Core.UI;
using JigsawPrototype.Features.Puzzle.Presentation.Screens;
using JigsawPrototype.Features.Puzzle.Preview;
using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Presentation.Dialogs
{
    public sealed class PuzzleStartPresenter
    {
        private readonly ICurrencyService _currency;
        private readonly IAdsService _ads;
        private readonly PuzzleStartPreviewController _previewController;
        private readonly ScreenStack _screens;
        private readonly PuzzleStartDialogView _view;
        private readonly PuzzleGameplayPresenter _gameplayPresenter;
        private readonly DialogHost _dialogHost;

        private PiecesPreset _piecesPreset;
        private bool _isActionBusy;
        private CancellationTokenSource _actionCts;

        public PuzzleStartPresenter(
            ICurrencyService currency,
            IAdsService ads,
            IPuzzlePreviewService preview,
            ScreenStack screens,
            PuzzleStartDialogView view,
            PuzzleGameplayPresenter gameplayPresenter,
            DialogHost dialogHost)
        {
            _currency = currency;
            _ads = ads;
            _previewController = new PuzzleStartPreviewController(preview);
            _screens = screens;
            _view = view;
            _gameplayPresenter = gameplayPresenter;
            _dialogHost = dialogHost;
        }

        public void Bind()
        {
            if (_piecesPreset == PiecesPreset.Unknown)
            {
                _piecesPreset = _view.InitialPiecesPreset;
            }

            _view.SetCoinsCost(AppConstants.Economy.PuzzleStartCoinsCost);
            _view.SetPiecesSelected(_piecesPreset);
            _view.SetCoins(_currency.Balance);
            _view.SetStatus("");
            _view.SetError("");

            _view.CloseRequested += OnClose;
            _view.PiecesSelected += OnPiecesSelected;
            _view.StartFreeRequested += OnStartFree;
            _view.StartCoinsRequested += OnStartCoins;
            _view.StartAdRequested += OnStartAd;
            _view.Shown += OnDialogShown;
            _view.Hidden += OnDialogHidden;
            _currency.BalanceChanged += OnBalanceChanged;

            if (_view.IsVisible)
            {
                OnDialogShown();
            }
        }

        public void Unbind()
        {
            CancelAll();
            _currency.BalanceChanged -= OnBalanceChanged;

            _view.CloseRequested -= OnClose;
            _view.PiecesSelected -= OnPiecesSelected;
            _view.StartFreeRequested -= OnStartFree;
            _view.StartCoinsRequested -= OnStartCoins;
            _view.StartAdRequested -= OnStartAd;
            _view.Shown -= OnDialogShown;
            _view.Hidden -= OnDialogHidden;
        }

        public UniTask OpenDialogAsync(PuzzleStartArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            ApplyArgs(args);

            _view.SetInteractable(true);
            _view.SetStatus("");
            _view.SetError("");

            if (_previewController.TryGetCachedForSelected(out var cachedPreview))
                _view.SetPreview(cachedPreview);
            else
                _view.SetPreviewLoading();

            return _dialogHost.PushAsync(_view, modal: true);
        }

        public UniTask ReopenLastDialogAsync()
        {
            return OpenDialogAsync(_previewController.CreateArgs(_piecesPreset));
        }

        private void OnDialogShown()
        {
            if (_previewController.TryConsumeCachedForSelected(out var cachedPreview, out var pendingError))
            {
                _view.SetPreview(cachedPreview);
                _view.SetStatus("");
                _view.SetError(pendingError);
                return;
            }

            LoadPreviewAsync().Forget();
        }

        private void OnDialogHidden()
        {
            CancelAll();
            _isActionBusy = false;
        }

        private void OnPiecesSelected(PiecesPreset preset)
        {
            if (_isActionBusy) return;
            _piecesPreset = preset;
            _view.SetPiecesSelected(preset);
        }

        private void OnStartFree()
        {
            if (_isActionBusy) return;
            CancelPreview();
            GoGameplayAsync().Forget();
        }

        private void OnStartCoins()
        {
            if (_isActionBusy) return;
            CancelPreview();
            if (_currency.TrySpend(AppConstants.Economy.PuzzleStartCoinsCost))
            {
                GoGameplayAsync().Forget();
                return;
            }

            GoStoreAsync().Forget();
        }

        private void OnStartAd()
        {
            if (_isActionBusy) return;
            CancelPreview();
            RunAdAsync().Forget();
        }

        private void OnClose()
        {
            if (_isActionBusy) return;
            _dialogHost.Hide(_view);
        }

        private void OnBalanceChanged(int coins) => _view.SetCoins(coins);

        private async UniTask LoadPreviewAsync()
        {
            CancelPreview();

            _view.SetStatus("Loading preview...");
            _view.SetError("");
            try
            {
                var result = await _previewController.LoadSelectedAsync();
                if (!_isActionBusy && _view.IsVisible)
                {
                    _view.SetPreview(result.Texture);
                    _view.SetError(result.Error);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (!_isActionBusy && _view.IsVisible)
                    _view.SetStatus("");
            }
        }

        private async UniTask RunAdAsync()
        {
            CancelAction();
            _actionCts = new CancellationTokenSource();
            var token = _actionCts.Token;

            BeginBusy("Loading ad...");
            try
            {
                var result = await _ads.ShowRewardedAsync(AdPlacements.PuzzleStart, token);
                if (result == AdResult.Success)
                    await GoGameplayCoreAsync();
                else
                    _view.SetError("Ad failed. Please retry.");
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogException(e);
                _view.SetError("Ad error. Please retry.");
            }
            finally
            {
                EndBusy();
                CancelAction();
            }
        }

        private async UniTask GoGameplayAsync()
        {
            BeginBusy();
            try { await GoGameplayCoreAsync(); }
            finally { EndBusy(); }
        }

        private async UniTask GoStoreAsync()
        {
            BeginBusy();
            try
            {
                await _dialogHost.HideAsync(_view);
                _screens.Push(ScreenId.Store);
            }
            finally { EndBusy(); }
        }

        private async UniTask GoGameplayCoreAsync()
        {
            await _dialogHost.HideAllAsync();
            _gameplayPresenter.SetPieces((int)_piecesPreset);
            _screens.Replace(ScreenId.PuzzleGameplay);
        }

        private void BeginBusy(string status = "")
        {
            _isActionBusy = true;
            _view.SetInteractable(false);
            _view.SetStatus(status);
            _view.SetError("");
        }

        private void EndBusy()
        {
            _isActionBusy = false;
            _view.SetInteractable(true);
            _view.SetStatus("");
        }

        private void CancelPreview()
        {
            _previewController.CancelLoad();
        }

        private void CancelAction()
        {
            if (_actionCts == null) return;
            _actionCts.Cancel();
            _actionCts.Dispose();
            _actionCts = null;
        }

        private void CancelAll()
        {
            CancelPreview();
            CancelAction();
        }

        private void ApplyArgs(PuzzleStartArgs args)
        {
            _previewController.SetSelection(args);

            if (args.InitialPiecesPreset.HasValue && args.InitialPiecesPreset.Value != PiecesPreset.Unknown)
            {
                _piecesPreset = args.InitialPiecesPreset.Value;
                _view.SetPiecesSelected(_piecesPreset);
            }
        }
    }
}
