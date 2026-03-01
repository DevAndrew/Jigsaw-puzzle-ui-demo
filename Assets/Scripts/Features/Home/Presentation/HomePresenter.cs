using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JigsawPrototype.Core.Services.Currency;
using JigsawPrototype.Features.Home.Catalog;
using JigsawPrototype.Features.Puzzle.Preview;
using JigsawPrototype.Features.Puzzle.Presentation.Dialogs;
using UnityEngine;

namespace JigsawPrototype.UI.Screens
{
    public sealed class HomePresenter
    {
        private readonly ICurrencyService _currency;
        private readonly IPuzzleCatalogService _catalog;
        private readonly IPuzzlePreviewService _preview;
        private readonly IPuzzlePreviewCache _previewCache;
        private readonly PuzzleStartPresenter _puzzleStartPresenter;

        private HomeScreenView _view;
        private CancellationTokenSource _lifetimeCts;
        private CancellationTokenSource _selectionCts;
        private int _selectionVersion;
        private IReadOnlyList<PuzzleCatalogItem> _items = Array.Empty<PuzzleCatalogItem>();

        private static Texture2D s_errorPlaceholder;

        public HomePresenter(
            ICurrencyService currency,
            IPuzzleCatalogService catalog,
            IPuzzlePreviewService preview,
            IPuzzlePreviewCache previewCache,
            PuzzleStartPresenter puzzleStartPresenter)
        {
            _currency = currency;
            _catalog = catalog;
            _preview = preview;
            _previewCache = previewCache;
            _puzzleStartPresenter = puzzleStartPresenter;
        }

        public void Bind(HomeScreenView view)
        {
            _view = view;
            _lifetimeCts = new CancellationTokenSource();
            _items = _catalog?.GetItems() ?? Array.Empty<PuzzleCatalogItem>();
            _view.SetCoins(_currency.Balance);
            _view.RenderCatalog(_items);
            _view.PuzzleSelected += OnPuzzleSelected;
            _currency.BalanceChanged += OnBalanceChanged;

            WarmupCatalogPreviewsAsync(_lifetimeCts.Token).Forget();
        }

        public void Unbind()
        {
            if (_view == null) return;
            CancelAsync();
            _view.PuzzleSelected -= OnPuzzleSelected;
            _currency.BalanceChanged -= OnBalanceChanged;
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
                var puzzleId = _items[i].Id;
                if (string.IsNullOrWhiteSpace(puzzleId)) continue;

                try
                {
                    var texture = await GetOrLoadPreviewAsync(puzzleId, ct);
                    _view?.SetTilePreview(puzzleId, texture);
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
            var selectedPuzzleId = ResolveSelectedPuzzleId(puzzleId);

            _view.SetGridInteractable(false);

            Texture2D texture = null;
            var loadError = "";
            try
            {
                texture = await GetOrLoadPreviewAsync(selectedPuzzleId, selectionToken);
                _view.SetTilePreview(selectedPuzzleId, texture);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                loadError = "Failed to load preview.";
                texture = GetErrorPlaceholder();
            }

            if (!IsSelectionCurrent(requestVersion, selectionToken))
            {
                return;
            }

            try
            {
                var args = new PuzzleStartArgs(
                    selectedPuzzleId,
                    texture ?? GetErrorPlaceholder(),
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

        private async UniTask<Texture2D> GetOrLoadPreviewAsync(string puzzleId, CancellationToken ct)
        {
            if (_previewCache != null && _previewCache.TryGet(puzzleId, out var cached))
            {
                return cached;
            }

            var texture = await _preview.GetPreviewAsync(puzzleId, ct);
            _previewCache?.Put(puzzleId, texture);
            return texture;
        }

        private string ResolveSelectedPuzzleId(string puzzleId)
        {
            if (!string.IsNullOrWhiteSpace(puzzleId))
            {
                return puzzleId;
            }

            if (!string.IsNullOrWhiteSpace(_catalog?.DefaultPuzzleId))
            {
                return _catalog.DefaultPuzzleId;
            }

            return _items.Count > 0 ? _items[0].Id : "";
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

            _previewCache?.Clear();
        }

        private static Texture2D GetErrorPlaceholder()
        {
            if (s_errorPlaceholder != null) return s_errorPlaceholder;

            s_errorPlaceholder = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            s_errorPlaceholder.SetPixels(new[]
            {
                new Color(0.2f, 0.2f, 0.2f, 1f),
                new Color(0.25f, 0.25f, 0.25f, 1f),
                new Color(0.25f, 0.25f, 0.25f, 1f),
                new Color(0.2f, 0.2f, 0.2f, 1f),
            });
            s_errorPlaceholder.Apply();
            return s_errorPlaceholder;
        }
    }
}

