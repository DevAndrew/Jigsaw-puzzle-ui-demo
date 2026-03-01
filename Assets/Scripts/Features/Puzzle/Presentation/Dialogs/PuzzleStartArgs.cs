using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Presentation.Dialogs
{
    public sealed class PuzzleStartArgs
    {
        public string PuzzleId { get; }
        public Texture2D PrefetchedPreview { get; }
        public PiecesPreset? InitialPiecesPreset { get; }
        public string InitialLoadError { get; }

        public PuzzleStartArgs(
            string puzzleId,
            Texture2D prefetchedPreview,
            PiecesPreset? initialPiecesPreset = null,
            string initialLoadError = null)
        {
            PuzzleId = puzzleId ?? "";
            PrefetchedPreview = prefetchedPreview;
            InitialPiecesPreset = initialPiecesPreset;
            InitialLoadError = initialLoadError ?? "";
        }
    }
}

