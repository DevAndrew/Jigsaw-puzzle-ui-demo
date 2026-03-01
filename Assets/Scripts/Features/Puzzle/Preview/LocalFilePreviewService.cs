using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Preview
{
    public sealed class LocalFilePreviewService : IPuzzlePreviewService
    {
        public sealed class Config
        {
            public string DefaultPreviewPath;
        }

        private readonly string _defaultPreviewPath;

        public LocalFilePreviewService(Config config)
        {
            _defaultPreviewPath = config?.DefaultPreviewPath ?? "";
        }

        public async UniTask<Texture2D> GetPreviewAsync(string previewPath, CancellationToken ct)
        {
            var assetPath = string.IsNullOrWhiteSpace(previewPath) ? _defaultPreviewPath : previewPath;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return PreviewPlaceholderTexture.GetOrCreate();
            }

            var resourcesPath = NormalizeResourcesPath(assetPath);
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                return PreviewPlaceholderTexture.GetOrCreate();
            }

            var req = Resources.LoadAsync<Texture2D>(resourcesPath);
            await req.ToUniTask(cancellationToken: ct);
            var texture = req.asset as Texture2D;
            return texture ?? PreviewPlaceholderTexture.GetOrCreate();
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
    }
}

