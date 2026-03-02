using System.Collections.Generic;
using JigsawPrototype.Core.Services.Ads;
using JigsawPrototype.Core.Services.Currency;
using JigsawPrototype.Core.UI;
using JigsawPrototype.Features.Home.Catalog;
using JigsawPrototype.Features.Puzzle.Preview;
using JigsawPrototype.Features.Puzzle.Presentation.Dialogs;
using JigsawPrototype.Features.Puzzle.Presentation.Screens;
using JigsawPrototype.Features.Home.Presentation;
using JigsawPrototype.Features.Store.Presentation;
using UnityEngine;

namespace JigsawPrototype.App
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private UiRootView _uiRootPrefab;
        [SerializeField] private GameObject _homeScreenPrefab;
        [SerializeField] private GameObject _storeScreenPrefab;
        [SerializeField] private GameObject _puzzleStartedScreenPrefab;
        [SerializeField] private GameObject _puzzleStartDialogPrefab;

        private UiRootView _ui;

        private InMemoryCurrencyService _currency;
        private SimulatedAdsService _ads;
        private LocalFilePreviewService _preview;
        private StaticPuzzleCatalogService _catalog;

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
                Debug.LogError("UiRoot is missing one or more required roots. Expected: Canvas/Screens and Canvas/Dialogs.");
                enabled = false;
                return;
            }

            // Services (demo config)
            _currency = new InMemoryCurrencyService(new InMemoryCurrencyService.Config
            {
                InitialBalance = AppConstants.Economy.InitialBalance
            });
            _ads = new SimulatedAdsService(new SimulatedAdsService.Config
            {
                SimulatedDelayMs = AppConstants.Ads.SimulatedDelayMs
            });
            var defaultPuzzleId = AppConstants.Catalog.DefaultPuzzleId;
            var catalogItems = new List<PuzzleCatalogItem>
            {
                new PuzzleCatalogItem("demo_1", "PuzzleImages/demo_img_1", 0),
                new PuzzleCatalogItem("demo_2", "PuzzleImages/demo_img_2", 1),
                new PuzzleCatalogItem("demo_3", "PuzzleImages/demo_img_3", 2),
                new PuzzleCatalogItem("demo_4", "PuzzleImages/demo_img_4", 3),
                new PuzzleCatalogItem("demo_5", "PuzzleImages/demo_img_5", 4),
                new PuzzleCatalogItem("demo_6", "PuzzleImages/demo_img_6", 5),
                new PuzzleCatalogItem("demo_7", "PuzzleImages/demo_img_7", 6),
                new PuzzleCatalogItem("demo_8", "PuzzleImages/demo_img_8", 7),
                new PuzzleCatalogItem("demo_9", "PuzzleImages/demo_img_9", 8),
                new PuzzleCatalogItem("demo_10", "PuzzleImages/demo_img_10", 9),
                new PuzzleCatalogItem("demo_11", "PuzzleImages/demo_img_11", 10),
                new PuzzleCatalogItem("demo_12", "PuzzleImages/demo_img_12", 11),
            };

            var defaultPreviewPath = "";
            for (var i = 0; i < catalogItems.Count; i++)
            {
                if (catalogItems[i].Id == defaultPuzzleId)
                {
                    defaultPreviewPath = catalogItems[i].PreviewPath;
                    break;
                }
            }

            _preview = new LocalFilePreviewService(new LocalFilePreviewService.Config
            {
                DefaultPreviewPath = defaultPreviewPath
            });
            _catalog = new StaticPuzzleCatalogService(catalogItems, defaultPuzzleId);

            // Views (prefab-authored, instantiated under UiRootShell)
            if (_homeScreenPrefab == null || _storeScreenPrefab == null || _puzzleStartedScreenPrefab == null || _puzzleStartDialogPrefab == null)
            {
                Debug.LogError("GameBootstrapper: assign all UI prefab references in inspector.");
                enabled = false;
                return;
            }

            var homeGo = Instantiate(_homeScreenPrefab, _ui.ScreensRoot);
            var storeGo = Instantiate(_storeScreenPrefab, _ui.ScreensRoot);
            var startedGo = Instantiate(_puzzleStartedScreenPrefab, _ui.ScreensRoot);
            var puzzleStartDialogGo = Instantiate(_puzzleStartDialogPrefab, _ui.DialogsRoot);

            var homeView = RequireComponent<HomeScreenView>(homeGo);
            var storeView = RequireComponent<StoreScreenView>(storeGo);
            var startedView = RequireComponent<PuzzleStartedScreenView>(startedGo);
            var puzzleStartDialogView = RequireComponent<PuzzleStartDialogView>(puzzleStartDialogGo);
            if (homeView == null || storeView == null || startedView == null || puzzleStartDialogView == null)
            {
                enabled = false;
                return;
            }

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
            _puzzleStartPresenter = new PuzzleStartPresenter(_currency, _ads, _preview, _screens, puzzleStartDialogView, _startedPresenter, _dialogHost, defaultPuzzleId, defaultPreviewPath);
            _homePresenter = new HomePresenter(_currency, _catalog, _preview, _puzzleStartPresenter);
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

        private static T RequireComponent<T>(GameObject go) where T : Component
        {
            if (go == null)
            {
                Debug.LogError($"Expected component '{typeof(T).Name}', but target GameObject is null.");
                return null;
            }

            if (!go.TryGetComponent<T>(out var component) || component == null)
            {
                Debug.LogError($"Expected component '{typeof(T).Name}' on '{go.name}'.");
                return null;
            }

            return component;
        }
    }
}

