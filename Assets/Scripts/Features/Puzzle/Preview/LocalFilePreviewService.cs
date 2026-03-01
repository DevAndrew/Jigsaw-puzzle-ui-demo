using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Preview
{
    public sealed class LocalFilePreviewService : IPuzzlePreviewService
    {
        [Serializable]
        public struct PreviewEntry
        {
            public string PuzzleId;
            public string AssetPath;
        }

        public sealed class Config
        {
            public PreviewEntry[] Entries;
            public string DefaultPuzzleId;
        }

        private readonly PreviewEntry[] _entries;
        private readonly string _defaultPuzzleId;

        private static Texture2D s_placeholder;

        public LocalFilePreviewService(Config config)
        {
            _entries = config.Entries ?? Array.Empty<PreviewEntry>();
            _defaultPuzzleId = config.DefaultPuzzleId ?? "";
        }

        public async UniTask<Texture2D> GetPreviewAsync(string puzzleId, CancellationToken ct)
        {
            if (_entries.Length == 0)
            {
                return GetPlaceholder();
            }

            var id = string.IsNullOrWhiteSpace(puzzleId) ? _defaultPuzzleId : puzzleId;
            var assetPath = TryGetAssetPath(id) ?? TryGetAssetPath(_defaultPuzzleId);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return GetPlaceholder();
            }

            var resourcesPath = NormalizeResourcesPath(assetPath);
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                return GetPlaceholder();
            }

            var req = Resources.LoadAsync<Texture2D>(resourcesPath);
            await req.ToUniTask(cancellationToken: ct);
            var texture = req.asset as Texture2D;
            return texture ?? GetPlaceholder();
        }

        private static string NormalizeResourcesPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return "";
            }

            var normalized = assetPath.Trim().Replace('\\', '/');

            const string resourcesPrefix = "Assets/Resources/";
            var resourcesPrefixIndex = normalized.IndexOf(resourcesPrefix, StringComparison.OrdinalIgnoreCase);
            if (resourcesPrefixIndex >= 0)
            {
                normalized = normalized.Substring(resourcesPrefixIndex + resourcesPrefix.Length);
            }

            const string shortResourcesPrefix = "Resources/";
            if (normalized.StartsWith(shortResourcesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(shortResourcesPrefix.Length);
            }

            var slashIndex = normalized.LastIndexOf('/');
            var dotIndex = normalized.LastIndexOf('.');
            if (dotIndex > slashIndex)
            {
                normalized = normalized.Substring(0, dotIndex);
            }

            return normalized;
        }

        private string TryGetAssetPath(string puzzleId)
        {
            if (string.IsNullOrWhiteSpace(puzzleId)) return null;

            for (var i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (e.PuzzleId == puzzleId)
                {
                    return e.AssetPath;
                }
            }

            return null;
        }

        private static Texture2D GetPlaceholder()
        {
            if (s_placeholder != null) return s_placeholder;

            s_placeholder = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            s_placeholder.SetPixels(new[]
            {
                new Color(0.2f, 0.2f, 0.2f, 1f),
                new Color(0.25f, 0.25f, 0.25f, 1f),
                new Color(0.25f, 0.25f, 0.25f, 1f),
                new Color(0.2f, 0.2f, 0.2f, 1f),
            });
            s_placeholder.Apply();
            return s_placeholder;
        }
    }
}

