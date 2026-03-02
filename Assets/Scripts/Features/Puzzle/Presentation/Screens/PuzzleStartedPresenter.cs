using JigsawPrototype.Core.UI;
using Cysharp.Threading.Tasks;

namespace JigsawPrototype.Features.Puzzle.Presentation.Screens
{
    public sealed class PuzzleStartedPresenter
    {
        private readonly ScreenStack _screens;
        private readonly DialogHost _dialogHost;
        private PuzzleStartedScreenView _view;

        public PuzzleStartedPresenter(ScreenStack screens, DialogHost dialogHost)
        {
            _screens = screens;
            _dialogHost = dialogHost;
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
            ReturnHomeAsync().Forget();
        }

        private async UniTask ReturnHomeAsync()
        {
            await _dialogHost.HideAllAsync();
            _screens.Replace(ScreenId.Home);
        }
    }
}

