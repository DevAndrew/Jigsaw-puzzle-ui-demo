using System;
using System.Collections.Generic;
using JigsawPrototype.Features.Home.Catalog;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace JigsawPrototype.UI.Screens
{
    public sealed class HomeScreenView : MonoBehaviour
    {

        [Header("Top Bar")]
        [SerializeField] private TMP_Text _coinsText;

        [Header("Puzzle Tiles")]
        [SerializeField] private RectTransform _gridRoot;
        [SerializeField] private PuzzleTileView _tilePrefab;
        [SerializeField] private Color _tilePlaceholderColor = new Color(1f, 1f, 1f, 0f);

        public event Action<string> PuzzleSelected;

        private readonly Dictionary<string, PuzzleTileView> _tilesById = new();
        private readonly List<PuzzleTileView> _activeTiles = new();
        private readonly Queue<PuzzleTileView> _pool = new();

        private void OnDisable()
        {
            SetGridInteractable(true);
        }

        public void SetCoins(int coins)
        {
            _coinsText.text = coins.ToString();
        }

        public void RenderCatalog(IReadOnlyList<PuzzleCatalogItem> items)
        {
            if (_gridRoot == null || _tilePrefab == null)
            {
                Debug.LogError("HomeScreenView: assign GridRoot and TilePrefab in inspector.");
                return;
            }

            ReleaseActiveTiles();

            if (items == null || items.Count == 0)
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                var puzzleId = item.Id;
                if (string.IsNullOrWhiteSpace(puzzleId)) continue;

                var tile = GetOrCreateTile();
                tile.name = $"PuzzleTile_{puzzleId}";
                tile.Bind(puzzleId, HandleTileSelected, _tilePlaceholderColor);
                _activeTiles.Add(tile);
                _tilesById[puzzleId] = tile;
            }
        }

        public void SetTilePreview(string puzzleId, Texture2D texture)
        {
            if (!_tilesById.TryGetValue(puzzleId, out var tile) || tile == null)
            {
                return;
            }

            tile.SetPreview(texture, _tilePlaceholderColor);
        }

        public void SetGridInteractable(bool interactable)
        {
            for (var i = 0; i < _activeTiles.Count; i++)
            {
                var tile = _activeTiles[i];
                if (tile != null)
                {
                    tile.SetInteractable(interactable);
                }
            }
        }

        private PuzzleTileView GetOrCreateTile()
        {
            while (_pool.Count > 0)
            {
                var pooled = _pool.Dequeue();
                if (pooled == null) continue;
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            return Instantiate(_tilePrefab, _gridRoot);
        }

        private void ReleaseActiveTiles()
        {
            for (var i = _activeTiles.Count - 1; i >= 0; i--)
            {
                var tile = _activeTiles[i];
                if (tile == null) continue;
                tile.gameObject.SetActive(false);
                _pool.Enqueue(tile);
            }

            _activeTiles.Clear();
            _tilesById.Clear();
        }

        private void HandleTileSelected(string puzzleId)
        {
            PuzzleSelected?.Invoke(puzzleId);
        }
    }
}

