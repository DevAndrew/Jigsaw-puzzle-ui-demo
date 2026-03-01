using System.Collections.Generic;
using JigsawPrototype.Core.Services.Ads;
using JigsawPrototype.Core.Services.Currency;
using JigsawPrototype.Core.UI;
using JigsawPrototype.Features.Home.Catalog;
using JigsawPrototype.Features.Puzzle.Preview;
using JigsawPrototype.Features.Puzzle.Presentation.Dialogs;
using JigsawPrototype.Features.Puzzle.Presentation.Screens;
using JigsawPrototype.Features.Store.Presentation;
using JigsawPrototype.UI.Screens;
using UnityEngine;

namespace JigsawPrototype.App
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private UiRootView _uiRootPrefab;

        private UiRootView _ui;

        private InMemoryCurrencyService _currency;
        private SimulatedAdsService _ads;
        private LocalFilePreviewService _preview;
        private StaticPuzzleCatalogService _catalog;
        private InMemoryPuzzlePreviewCache _previewCache;

        private HomePresenter _homePresenter;
        private PuzzleStartPresenter _puzzleStartPresenter;
        private StorePresenter _storePresenter;
        private PuzzleStartedPresenter _startedPresenter;

        private ScreenStack _screens;
        private DialogHost _dialogHost;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_uiRootPrefab == null)
            {
                Debug.LogError("GameBootstrapper: assign UiRootPrefab in inspector.");
                enabled = false;
                return;
            }

            _ui = Instantiate(_uiRootPrefab);
            DontDestroyOnLoad(_ui.gameObject);

            if (_ui.ScreensRoot == null || _ui.DialogsRoot == null)
            {
                Debug.LogError("UiRoot is missing ScreensRoot and/or DialogsRoot. Expected hierarchy: Canvas/Screens and Canvas/Dialogs.");
                enabled = false;
                return;
            }

            // Services (demo config)
            _currency = new InMemoryCurrencyService(new InMemoryCurrencyService.Config { InitialBalance = 2400 });
            _ads = new SimulatedAdsService(new SimulatedAdsService.Config { SimulatedDelayMs = 1200 });
            const string defaultPuzzleId = "demo_1";
            var previewEntries = new[]
            {
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_1", AssetPath = "PuzzleImages/demo_img_1" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_2", AssetPath = "PuzzleImages/demo_img_2" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_3", AssetPath = "PuzzleImages/demo_img_3" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_4", AssetPath = "PuzzleImages/demo_img_4" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_5", AssetPath = "PuzzleImages/demo_img_5" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_6", AssetPath = "PuzzleImages/demo_img_6" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_7", AssetPath = "PuzzleImages/demo_img_7" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_8", AssetPath = "PuzzleImages/demo_img_8" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_9", AssetPath = "PuzzleImages/demo_img_9" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_10", AssetPath = "PuzzleImages/demo_img_10" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_11", AssetPath = "PuzzleImages/demo_img_11" },
                new LocalFilePreviewService.PreviewEntry { PuzzleId = "demo_12", AssetPath = "PuzzleImages/demo_img_12" },
            };
            _preview = new LocalFilePreviewService(new LocalFilePreviewService.Config
            {
                DefaultPuzzleId = defaultPuzzleId,
                Entries = previewEntries
            });
            var catalogItems = new List<PuzzleCatalogItem>(previewEntries.Length);
            for (var i = 0; i < previewEntries.Length; i++)
            {
                var entry = previewEntries[i];
                catalogItems.Add(new PuzzleCatalogItem(entry.PuzzleId, entry.AssetPath, i));
            }
            _catalog = new StaticPuzzleCatalogService(catalogItems, defaultPuzzleId);
            _previewCache = new InMemoryPuzzlePreviewCache(capacity: 64);

            // Views (prefab-authored, instantiated under UiRootShell)
            var homePrefab = LoadUiPrefab("UI/Screens/HomeScreen");
            var storePrefab = LoadUiPrefab("UI/Screens/StoreScreen");
            var startedPrefab = LoadUiPrefab("UI/Screens/PuzzleStartedScreen");
            var puzzleStartDialogPrefab = LoadUiPrefab("UI/Dialogs/PuzzleStartDialog");

            if (homePrefab == null || storePrefab == null || startedPrefab == null || puzzleStartDialogPrefab == null)
            {
                enabled = false;
                return;
            }

            var homeGo = Instantiate(homePrefab, _ui.ScreensRoot);
            var storeGo = Instantiate(storePrefab, _ui.ScreensRoot);
            var startedGo = Instantiate(startedPrefab, _ui.ScreensRoot);
            var puzzleStartDialogGo = Instantiate(puzzleStartDialogPrefab, _ui.DialogsRoot);

            var homeView = RequireComponent<HomeScreenView>(homeGo, "HomeScreenView");
            var storeView = RequireComponent<StoreScreenView>(storeGo, "StoreScreenView");
            var startedView = RequireComponent<PuzzleStartedScreenView>(startedGo, "PuzzleStartedScreenView");
            var puzzleStartDialogView = RequireComponent<PuzzleStartDialogView>(puzzleStartDialogGo, "PuzzleStartDialogView");

            // Navigation
            _screens = new ScreenStack(new Dictionary<ScreenId, GameObject>
            {
                { ScreenId.Home, homeView.gameObject },
                { ScreenId.Store, storeView.gameObject },
                { ScreenId.PuzzleStarted, startedView.gameObject }
            });

            _dialogHost = _ui.DialogHost;
            if (_dialogHost == null)
            {
                Debug.LogError("DialogHost not found in UiRoot. Add DialogHost component under UiRoot and reference it in UiRootView.");
                enabled = false;
                return;
            }

            // Presenters
            _startedPresenter = new PuzzleStartedPresenter(_screens);
            _puzzleStartPresenter = new PuzzleStartPresenter(_currency, _ads, _preview, _screens, puzzleStartDialogView, _startedPresenter, _dialogHost, defaultPuzzleId);
            _homePresenter = new HomePresenter(_currency, _catalog, _preview, _previewCache, _puzzleStartPresenter);
            _storePresenter = new StorePresenter(_currency, _screens, _puzzleStartPresenter);

            // Bind views
            _homePresenter.Bind(homeView);
            _puzzleStartPresenter.Bind(puzzleStartDialogView);
            _storePresenter.Bind(storeView);
            _startedPresenter.Bind(startedView);

            // Initial state
            _screens.Replace(ScreenId.Home);
            puzzleStartDialogView.HideImmediate();
            _startedPresenter.SetPieces((int)puzzleStartDialogView.InitialPiecesPreset);
        }

        private void OnDestroy()
        {
            _homePresenter?.Unbind();
            _puzzleStartPresenter?.Unbind();
            _storePresenter?.Unbind();
            _startedPresenter?.Unbind();
        }

        private static GameObject LoadUiPrefab(string resourcesPath)
        {
            var prefab = Resources.Load<GameObject>(resourcesPath);
            if (prefab == null)
            {
                Debug.LogError($"UI prefab not found at Resources path '{resourcesPath}'.");
            }
            return prefab;
        }

        private static T RequireComponent<T>(GameObject go, string typeName) where T : Component
        {
            if (go == null) return null;
            var c = go.GetComponent<T>();
            if (c == null)
            {
                Debug.LogError($"Expected component '{typeName}' on '{go.name}'.");
            }
            return c;
        }
    }
}

