using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    public sealed class UiRootView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform screensRoot;
        [SerializeField] private Transform dialogsRoot;

        [Header("Hosts")]
        [SerializeField] private DialogHost dialogHost;

        private bool _wired;

        public Transform ScreensRoot
        {
            get
            {
                EnsureWired();
                return screensRoot;
            }
        }

        public Transform DialogsRoot
        {
            get
            {
                EnsureWired();
                return dialogsRoot;
            }
        }

        public DialogHost DialogHost
        {
            get
            {
                EnsureWired();
                return dialogHost;
            }
        }

        private void Awake()
        {
            EnsureWired();
        }

        private void EnsureWired()
        {
            if (_wired) return;

            // Convenience: allow leaving references empty in prefab as long as expected objects exist in children.
            dialogHost ??= GetComponentInChildren<DialogHost>(includeInactive: true);
            screensRoot ??= transform.Find("Canvas/Screens");
            dialogsRoot ??= transform.Find("Canvas/Dialogs");

            // Fallback: tolerate different nesting (e.g. if shell is reauthored).
            if (screensRoot == null || dialogsRoot == null)
            {
                var all = GetComponentsInChildren<Transform>(includeInactive: true);
                for (var i = 0; i < all.Length; i++)
                {
                    var t = all[i];
                    if (t == null) continue;
                    if (screensRoot == null && t.name == "Screens") screensRoot = t;
                    if (dialogsRoot == null && t.name == "Dialogs") dialogsRoot = t;
                    if (screensRoot != null && dialogsRoot != null) break;
                }
            }

            _wired = true;
        }
    }
}

