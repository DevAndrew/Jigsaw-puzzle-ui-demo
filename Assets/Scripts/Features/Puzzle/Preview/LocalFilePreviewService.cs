using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Preview
{
    public sealed class LocalFilePreviewService : IPuzzlePreviewService
    {
        private readonly Dictionary<string, Sprite> _cache = new(StringComparer.Ordinal);

        public LocalFilePreviewService()
        {
        }

        public async UniTask<Sprite> GetPreviewAsync(string previewPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(previewPath))
                return null;

            var resourcesPath = NormalizeResourcesPath(previewPath);
            if (string.IsNullOrWhiteSpace(resourcesPath))
                return null;

            if (_cache.TryGetValue(resourcesPath, out var cached) && cached != null)
                return cached;

            var req = Resources.LoadAsync<Sprite>(resourcesPath);
            await req.ToUniTask(cancellationToken: ct);
            var sprite = req.asset as Sprite;
            if (sprite != null)
                _cache[resourcesPath] = sprite;
            return sprite;
        }

        private static string NormalizeResourcesPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return "";

            var normalized = assetPath.Trim().Replace('\\', '/');

            const string resourcesPrefix = "Assets/Resources/";
            var resourcesPrefixIndex = normalized.IndexOf(resourcesPrefix, StringComparison.OrdinalIgnoreCase);
            if (resourcesPrefixIndex >= 0)
                normalized = normalized.Substring(resourcesPrefixIndex + resourcesPrefix.Length);

            const string shortResourcesPrefix = "Resources/";
            if (normalized.StartsWith(shortResourcesPrefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(shortResourcesPrefix.Length);

            var slashIndex = normalized.LastIndexOf('/');
            var dotIndex = normalized.LastIndexOf('.');
            if (dotIndex > slashIndex)
                normalized = normalized.Substring(0, dotIndex);

            return normalized;
        }
    }
}
