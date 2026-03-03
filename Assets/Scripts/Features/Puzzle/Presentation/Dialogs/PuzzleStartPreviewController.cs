using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JigsawPrototype.Features.Puzzle.Preview;
using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Presentation.Dialogs
{
    public sealed class PuzzleStartPreviewController
    {
        private readonly IPuzzlePreviewService _previewService;

        private CancellationTokenSource _loadCts;
        private string _selectedPuzzleId = "";
        private string _selectedPreviewPath = "";
        private string _prefetchedPuzzleId;
        private Texture2D _prefetchedPreview;
        private string _pendingLoadError;

        public PuzzleStartPreviewController(IPuzzlePreviewService previewService)
        {
            _previewService = previewService;
        }

        public void SetSelection(PuzzleStartArgs args)
        {
            _selectedPuzzleId = args.PuzzleId;
            _selectedPreviewPath = args.PreviewPath;
            _prefetchedPuzzleId = _selectedPuzzleId;
            _prefetchedPreview = args.PrefetchedPreview;
            _pendingLoadError = args.InitialLoadError ?? "";
        }

        public PuzzleStartArgs CreateArgs(PiecesPreset piecesPreset)
        {
            return new PuzzleStartArgs(
                _selectedPuzzleId,
                _selectedPreviewPath,
                _prefetchedPreview,
                piecesPreset,
                _pendingLoadError);
        }

        public bool TryGetCachedForSelected(out Texture2D texture)
        {
            if (!string.IsNullOrWhiteSpace(_prefetchedPuzzleId) &&
                _prefetchedPreview != null &&
                _prefetchedPuzzleId == _selectedPuzzleId)
            {
                texture = _prefetchedPreview;
                return true;
            }

            texture = null;
            return false;
        }

        public bool TryConsumeCachedForSelected(out Texture2D texture, out string pendingError)
        {
            if (TryGetCachedForSelected(out texture))
            {
                pendingError = _pendingLoadError;
                _pendingLoadError = "";
                return true;
            }

            pendingError = "";
            return false;
        }

        public async UniTask<PuzzleStartPreviewLoadResult> LoadSelectedAsync()
        {
            CancelLoad();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            try
            {
                var sprite = await _previewService.GetPreviewAsync(_selectedPreviewPath, token);
                token.ThrowIfCancellationRequested();

                var texture = sprite != null ? sprite.texture : PreviewPlaceholderTexture.GetOrCreate();
                _prefetchedPuzzleId = _selectedPuzzleId;
                _prefetchedPreview = texture;
                _pendingLoadError = "";
                return new PuzzleStartPreviewLoadResult(texture, "");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                var fallbackTexture = PreviewPlaceholderTexture.GetOrCreate();
                _prefetchedPuzzleId = _selectedPuzzleId;
                _prefetchedPreview = fallbackTexture;
                _pendingLoadError = "Failed to load preview.";
                return new PuzzleStartPreviewLoadResult(fallbackTexture, _pendingLoadError);
            }
        }

        public void CancelLoad()
        {
            if (_loadCts == null) return;
            _loadCts.Cancel();
            _loadCts.Dispose();
            _loadCts = null;
        }
    }

    public readonly struct PuzzleStartPreviewLoadResult
    {
        public Texture2D Texture { get; }
        public string Error { get; }

        public PuzzleStartPreviewLoadResult(Texture2D texture, string error)
        {
            Texture = texture;
            Error = error ?? "";
        }
    }
}
