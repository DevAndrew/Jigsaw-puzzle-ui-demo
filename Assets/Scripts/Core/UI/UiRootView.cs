using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    public sealed class UiRootView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform _screensRoot;
        [SerializeField] private Transform _dialogsRoot;

        [Header("Hosts")]
        [SerializeField] private DialogHost _dialogHost;

        public Transform ScreensRoot
        {
            get
            {
                return _screensRoot;
            }
        }

        public Transform DialogsRoot
        {
            get
            {
                return _dialogsRoot;
            }
        }

        public DialogHost DialogHost
        {
            get
            {
                return _dialogHost;
            }
        }
    }
}

