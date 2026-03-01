using JigsawPrototype.Core.UI;

namespace JigsawPrototype.Features.Puzzle.Presentation.Screens
{
    public sealed class PuzzleStartedPresenter
    {
        private readonly ScreenStack _screens;
        private PuzzleStartedScreenView _view;

        public PuzzleStartedPresenter(ScreenStack screens)
        {
            _screens = screens;
        }

        public void Bind(PuzzleStartedScreenView view)
        {
            _view = view;
            _view.BackRequested += OnBack;
        }

        public void Unbind()
        {
            if (_view == null) return;
            _view.BackRequested -= OnBack;
            _view = null;
        }

        public void SetPieces(int pieces)
        {
            _view?.SetTitle($"Puzzle started! Pieces: {pieces}");
        }

        private void OnBack()
        {
            _screens.Replace(ScreenId.Home);
        }
    }
}

