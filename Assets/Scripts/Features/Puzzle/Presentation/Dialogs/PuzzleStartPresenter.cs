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
        private readonly IPuzzlePreviewService _preview;
        private readonly ScreenStack _screens;
        private readonly PuzzleStartDialogView _view;
        private readonly PuzzleStartedPresenter _startedPresenter;
        private readonly DialogHost _dialogHost;
        private readonly string _defaultPuzzleId;
        private readonly string _defaultPreviewPath;

        private PiecesPreset _piecesPreset;
        private bool _busy;
        private CancellationTokenSource _cts;
        private string _selectedPuzzleId;
        private string _selectedPreviewPath;
        private string _prefetchedPuzzleId;
        private Texture2D _prefetchedPreview;
        private string _pendingLoadError;

        public PuzzleStartPresenter(
            ICurrencyService currency,
            IAdsService ads,
            IPuzzlePreviewService preview,
            ScreenStack screens,
            PuzzleStartDialogView view,
            PuzzleStartedPresenter startedPresenter,
            DialogHost dialogHost,
            string defaultPuzzleId,
            string defaultPreviewPath)
        {
            _currency = currency;
            _ads = ads;
            _preview = preview;
            _screens = screens;
            _view = view;
            _startedPresenter = startedPresenter;
            _dialogHost = dialogHost;
            _defaultPuzzleId = defaultPuzzleId ?? "";
            _defaultPreviewPath = defaultPreviewPath ?? "";
            _selectedPuzzleId = _defaultPuzzleId;
            _selectedPreviewPath = _defaultPreviewPath;
        }

        public void Bind(PuzzleStartDialogView dialog)
        {
            if (_piecesPreset == PiecesPreset.Unknown)
            {
                _piecesPreset = dialog.InitialPiecesPreset;
            }

            dialog.SetCoinsCost(AppConstants.Economy.PuzzleStartCoinsCost);
            dialog.SetPiecesSelected(_piecesPreset);
            dialog.SetCoins(_currency.Balance);
            dialog.SetStatus("");
            dialog.SetError("");

            dialog.CloseRequested += OnClose;
            dialog.PiecesSelected += OnPiecesSelected;
            dialog.StartFreeRequested += OnStartFree;
            dialog.StartCoinsRequested += OnStartCoins;
            dialog.StartAdRequested += OnStartAd;
            dialog.Shown += OnDialogShown;
            dialog.Hidden += OnDialogHidden;

            _currency.BalanceChanged += OnBalanceChanged;
            
            // If view is already visible (e.g. authored active), trigger the "shown" behavior immediately.
            if (dialog.IsVisible)
            {
                OnDialogShown();
            }
        }

        public void Unbind()
        {
            CancelAsync();
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
            ApplyArgs(args);

            // Set preview before show animation starts, so old image never flashes.
            if (_prefetchedPreview != null && _prefetchedPuzzleId == _selectedPuzzleId)
            {
                _view.SetPreview(_prefetchedPreview);
            }
            else
            {
                _view.SetPreviewLoading();
            }

            return _dialogHost.PushAsync(_view, modal: true);
        }

        public UniTask ReopenLastDialogAsync()
        {
            var args = new PuzzleStartArgs(
                _selectedPuzzleId,
                _selectedPreviewPath,
                _prefetchedPreview,
                _piecesPreset,
                _pendingLoadError);

            return OpenDialogAsync(args);
        }

        private void OnDialogShown()
        {
            if (!string.IsNullOrWhiteSpace(_prefetchedPuzzleId) &&
                _prefetchedPreview != null &&
                _prefetchedPuzzleId == _selectedPuzzleId)
            {
                _view.SetPreview(_prefetchedPreview);
                _view.SetStatus("");
                _view.SetError(_pendingLoadError);
                _pendingLoadError = "";
                return;
            }

            // Fallback: opening the dialog triggers preview load (and cancels any previous one).
            StartPreviewLoadAsync().Forget();
        }

        private void OnDialogHidden()
        {
            // Rule: hiding the dialog cancels any pending async operations (preview/ad).
            CancelAsync();
            _busy = false;
        }

        private void OnPiecesSelected(PiecesPreset preset)
        {
            if (_busy) return;
            _piecesPreset = preset;
            _view.SetPiecesSelected(preset);
        }

        private void OnStartFree()
        {
            if (_busy) return;
            GoStartedAsync().Forget();
        }

        private void OnStartCoins()
        {
            if (_busy) return;
            if (_currency.TrySpend(AppConstants.Economy.PuzzleStartCoinsCost))
            {
                GoStartedAsync().Forget();
                return;
            }

            // Variant B: silently go to store.
            GoStoreAsync().Forget();
        }

        private void OnStartAd()
        {
            if (_busy) return;
            RunAdAsync().Forget();
        }

        private void OnClose()
        {
            if (_busy) return;
            _dialogHost.Hide(_view);
        }

        private void OnBalanceChanged(int coins)
        {
            _view.SetCoins(coins);
        }

        private async UniTask StartPreviewLoadAsync()
        {
            CancelAsync();
            _cts = new CancellationTokenSource();
            await LoadPreviewAsync();
        }

        private async UniTask LoadPreviewAsync()
        {
            BeginBusy("Loading preview...");
            try
            {
                var tex = await _preview.GetPreviewAsync(_selectedPreviewPath, _cts.Token);
                _prefetchedPuzzleId = _selectedPuzzleId;
                _prefetchedPreview = tex;
                _view.SetPreview(tex);
                _view.SetStatus("");
                _view.SetError("");
                _pendingLoadError = "";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _view.SetError("Failed to load preview.");
            }
            finally
            {
                EndBusy();
            }
        }

        private async UniTask RunAdAsync()
        {
            EnsureCts();
            BeginBusy("Loading ad...");
            try
            {
                var result = await _ads.ShowRewardedAsync(AdPlacements.PuzzleStart, _cts.Token);
                if (result == AdResult.Success)
                {
                    await GoStartedAsync();
                }
                else
                {
                    _view.SetError("Ad failed. Please retry.");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _view.SetError("Ad error. Please retry.");
            }
            finally
            {
                EndBusy();
            }
        }

        private async UniTask GoStartedAsync()
        {
            _busy = true;
            await _dialogHost.HideAsync(_view);
            _startedPresenter.SetPieces((int)_piecesPreset);
            _screens.Replace(ScreenId.PuzzleStarted);
        }

        private async UniTask GoStoreAsync()
        {
            _busy = true;
            await _dialogHost.HideAsync(_view);
            _screens.Push(ScreenId.Store);
        }

        private void BeginBusy(string status)
        {
            _busy = true;
            _view.SetInteractable(false);
            _view.SetStatus(status);
            _view.SetError("");
        }

        private void EndBusy()
        {
            _busy = false;
            _view.SetInteractable(true);
            _view.SetStatus("");
        }

        private void CancelAsync()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void EnsureCts()
        {
            _cts ??= new CancellationTokenSource();
        }

        private void ApplyArgs(PuzzleStartArgs args)
        {
            if (args == null)
            {
                _selectedPuzzleId = _defaultPuzzleId;
                _selectedPreviewPath = _defaultPreviewPath;
                _prefetchedPuzzleId = _selectedPuzzleId;
                _prefetchedPreview = null;
                _pendingLoadError = "";
                return;
            }

            _selectedPuzzleId = string.IsNullOrWhiteSpace(args.PuzzleId) ? _defaultPuzzleId : args.PuzzleId;
            _selectedPreviewPath = string.IsNullOrWhiteSpace(args.PreviewPath) ? _defaultPreviewPath : args.PreviewPath;
            _prefetchedPuzzleId = _selectedPuzzleId;
            _prefetchedPreview = args.PrefetchedPreview;
            _pendingLoadError = args.InitialLoadError ?? "";

            if (args.InitialPiecesPreset.HasValue && args.InitialPiecesPreset.Value != PiecesPreset.Unknown)
            {
                _piecesPreset = args.InitialPiecesPreset.Value;
                _view.SetPiecesSelected(_piecesPreset);
            }
        }
    }
}

