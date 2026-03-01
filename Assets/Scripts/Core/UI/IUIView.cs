using System;
using Cysharp.Threading.Tasks;

namespace JigsawPrototype.Core.UI
{
    /// <summary>
    /// Minimal UI contract for dialogs/popups (and optionally screens).
    /// Keeps presenters independent from GameObject.SetActive and animation details.
    /// </summary>
    public interface IUIView
    {
        bool IsVisible { get; }

        event Action Shown;
        event Action Hidden;

        UniTask ShowAsync();
        UniTask HideAsync();

        /// <summary>
        /// Whether the view should accept user input (raycasts / interactables).
        /// </summary>
        void SetInteractable(bool value);
    }
}

