using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JigsawPrototype.Core.Async;
using JigsawPrototype.Core.Services.Currency;
using JigsawPrototype.Features.Home.Catalog;
using JigsawPrototype.Features.Puzzle.Presentation.Dialogs;
using JigsawPrototype.Features.Puzzle.Preview;
using UnityEngine;

namespace JigsawPrototype.Features.Home.Presentation
{
    public sealed class HomePresenter
    {
        private readonly ICurrencyService _currency;
        private readonly IPuzzleCatalogService _catalog;
        private readonly IPuzzlePreviewService _preview;
        private readonly PuzzleStartPresenter _puzzleStartPresenter;

        private HomeScreenView _view;
        private CancellationTokenSource _lifetimeCts;
        private CancellationTokenSource _selectionCts;
        private readonly AsyncLock _selectionLock = new AsyncLock();
        private IReadOnlyList<PuzzleCatalogItem> _items = Array.Empty<PuzzleCatalogItem>();
        private readonly Dictionary<string, PuzzleCatalogItem> _itemsById = new(StringComparer.Ordinal);

        public HomePresenter(
            ICurrencyService currency,
            IPuzzleCatalogService catalog,
            IPuzzlePreviewService preview,
            PuzzleStartPresenter puzzleStartPresenter) // TODO: cross-feature connection (Home directly depends on PuzzleStartPresenter) 
        {
            _currency = currency;
            _catalog = catalog;
            _preview = preview;
            _puzzleStartPresenter = puzzleStartPresenter;
        }

        public void Bind(HomeScreenView view)
        {
            _view = view;
            _lifetimeCts = new CancellationTokenSource();
            _items = _catalog?.GetItems() ?? Array.Empty<PuzzleCatalogItem>();
            RebuildItemsIndex();
            _view.SetCoins(_currency.Balance);
            _view.RenderCatalog(_items);
            _view.PuzzleSelected += OnPuzzleSelected;
            _currency.BalanceChanged += OnBalanceChanged;

            FetchAndApplyCatalogPreviewsAsync(_lifetimeCts.Token).Forget();
        }

        public void Unbind()
        {
            _currency.BalanceChanged -= OnBalanceChanged;
            if (_view == null) return;
            CancelAsync();
            _view.PuzzleSelected -= OnPuzzleSelected;
            _view = null;
        }

        private void OnPuzzleSelected(string puzzleId)
        {
            OpenPuzzleDialogAsync(puzzleId).Forget();
        }

        private async UniTask FetchAndApplyCatalogPreviewsAsync(CancellationToken ct)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;

                try
                {
                    var sprite = await _preview.GetPreviewAsync(item.PreviewPath, ct);
                    _view?.SetTilePreview(item.Id, sprite);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private async UniTask OpenPuzzleDialogAsync(string puzzleId)
        {
            if (_view == null || _lifetimeCts == null) return;

            var selectionToken = BeginSelectionRequest();
            var selectedItem = ResolveSelectedPuzzleItem(puzzleId);
            if (selectedItem == null)
            {
                _view.SetGridInteractable(true);
                return;
            }

            try
            {
                using (await _selectionLock.LockAsync(selectionToken))
                {
                    _view?.SetGridInteractable(false);

                    var (texture, loadError) = await TryLoadSelectedPreviewAsync(selectedItem, selectionToken);
                    selectionToken.ThrowIfCancellationRequested();

                    var args = new PuzzleStartArgs(
                        selectedItem.Id,
                        selectedItem.PreviewPath,
                        texture ?? PreviewPlaceholderTexture.GetOrCreate(),
                        initialLoadError: loadError);

                    await _puzzleStartPresenter.OpenDialogAsync(args);
                }
            }
            catch (OperationCanceledException)
            {
                // Latest-wins: selection was superseded by newer request.
            }
            finally
            {
                if (!selectionToken.IsCancellationRequested)
                {
                    _view?.SetGridInteractable(true);
                }
            }
        }

        private async UniTask<(Texture2D texture, string loadError)> TryLoadSelectedPreviewAsync(PuzzleCatalogItem selectedItem, CancellationToken ct)
        {
            try
            {
                var sprite = await _preview.GetPreviewAsync(selectedItem.PreviewPath, ct);
                _view?.SetTilePreview(selectedItem.Id, sprite);
                return (sprite != null ? sprite.texture : null, "");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return (null, "Failed to load preview.");
            }
        }

        private void OnBalanceChanged(int coins)
        {
            _view?.SetCoins(coins);
        }

        private PuzzleCatalogItem ResolveSelectedPuzzleItem(string puzzleId)
        {
            if (!string.IsNullOrWhiteSpace(puzzleId) && _itemsById.TryGetValue(puzzleId, out var selected))
            {
                return selected;
            }

            return null;
        }

        private void RebuildItemsIndex()
        {
            _itemsById.Clear();

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;
                _itemsById[item.Id] = item;
            }
        }

        private CancellationToken BeginSelectionRequest()
        {
            CancelSelectionRequest();
            _selectionCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
            return _selectionCts.Token;
        }

        private void CancelSelectionRequest()
        {
            var cts = _selectionCts;
            _selectionCts = null;
            if (cts == null) return;
            cts.Cancel();
            cts.Dispose();
        }

        private void CancelAsync()
        {
            CancelSelectionRequest();

            if (_lifetimeCts != null)
            {
                _lifetimeCts.Cancel();
                _lifetimeCts.Dispose();
                _lifetimeCts = null;
            }

        }

    }
}

