using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private int _selectionVersion;
        private IReadOnlyList<PuzzleCatalogItem> _items = Array.Empty<PuzzleCatalogItem>();
        private readonly Dictionary<string, PuzzleCatalogItem> _itemsById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> _previewSpritesById = new(StringComparer.Ordinal);

        public HomePresenter(
            ICurrencyService currency,
            IPuzzleCatalogService catalog,
            IPuzzlePreviewService preview,
            PuzzleStartPresenter puzzleStartPresenter)
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
            _previewSpritesById.Clear();
            _view.SetCoins(_currency.Balance);
            _view.RenderCatalog(_items);
            _view.PuzzleSelected += OnPuzzleSelected;
            _currency.BalanceChanged += OnBalanceChanged;

            WarmupCatalogPreviewsAsync(_lifetimeCts.Token).Forget();
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

        private async UniTask WarmupCatalogPreviewsAsync(CancellationToken ct)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;

                try
                {
                    var sprite = await GetOrLoadPreviewSpriteAsync(item, ct);
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

            var requestVersion = BeginSelectionRequest();
            var selectionToken = _selectionCts.Token;
            var selectedItem = ResolveSelectedPuzzleItem(puzzleId);
            if (selectedItem == null)
            {
                _view.SetGridInteractable(true);
                return;
            }

            var selectedPuzzleId = selectedItem.Id;
            var selectedPreviewPath = selectedItem.PreviewPath;

            _view.SetGridInteractable(false);

            Texture2D texture = null;
            var loadError = "";
            try
            {
                var sprite = await GetOrLoadPreviewSpriteAsync(selectedItem, selectionToken);
                _view.SetTilePreview(selectedPuzzleId, sprite);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                texture = await GetOrLoadPreviewAsync(selectedItem, selectionToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                loadError = "Failed to load preview.";
                texture = PreviewPlaceholderTexture.GetOrCreate();
            }

            if (!IsSelectionCurrent(requestVersion, selectionToken))
            {
                return;
            }

            try
            {
                var args = new PuzzleStartArgs(
                    selectedPuzzleId,
                    selectedPreviewPath,
                    texture ?? PreviewPlaceholderTexture.GetOrCreate(),
                    initialLoadError: loadError);

                await _puzzleStartPresenter.OpenDialogAsync(args);
            }
            finally
            {
                if (IsSelectionCurrent(requestVersion, selectionToken))
                {
                    _view?.SetGridInteractable(true);
                }
            }
        }

        private void OnBalanceChanged(int coins)
        {
            _view?.SetCoins(coins);
        }

        private async UniTask<Texture2D> GetOrLoadPreviewAsync(PuzzleCatalogItem item, CancellationToken ct)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
            {
                return PreviewPlaceholderTexture.GetOrCreate();
            }

            var texture = await _preview.GetPreviewAsync(item.PreviewPath, ct);
            return texture;
        }

        private async UniTask<Sprite> GetOrLoadPreviewSpriteAsync(PuzzleCatalogItem item, CancellationToken ct)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
            {
                return null;
            }

            if (_previewSpritesById.TryGetValue(item.Id, out var cached) && cached != null)
            {
                return cached;
            }

            var request = Resources.LoadAsync<Sprite>(item.PreviewPath);
            await request.ToUniTask(cancellationToken: ct);
            var sprite = request.asset as Sprite;
            _previewSpritesById[item.Id] = sprite;
            return sprite;
        }

        private PuzzleCatalogItem ResolveSelectedPuzzleItem(string puzzleId)
        {
            if (!string.IsNullOrWhiteSpace(puzzleId) && _itemsById.TryGetValue(puzzleId, out var selected))
            {
                return selected;
            }

            if (!string.IsNullOrWhiteSpace(_catalog?.DefaultPuzzleId) &&
                _itemsById.TryGetValue(_catalog.DefaultPuzzleId, out var defaultItem))
            {
                return defaultItem;
            }

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item != null && !string.IsNullOrWhiteSpace(item.Id))
                {
                    return item;
                }
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

        private int BeginSelectionRequest()
        {
            CancelSelectionRequest();
            _selectionCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
            _selectionVersion++;
            return _selectionVersion;
        }

        private bool IsSelectionCurrent(int version, CancellationToken token)
        {
            return _selectionCts != null && !token.IsCancellationRequested && _selectionVersion == version;
        }

        private void CancelSelectionRequest()
        {
            if (_selectionCts == null) return;
            _selectionCts.Cancel();
            _selectionCts.Dispose();
            _selectionCts = null;
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

