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

        private static Texture2D s_placeholder;

        public LocalFilePreviewService(Config config)
        {
            _defaultPreviewPath = config?.DefaultPreviewPath ?? "";
        }

        public async UniTask<Texture2D> GetPreviewAsync(string previewPath, CancellationToken ct)
        {
            var assetPath = string.IsNullOrWhiteSpace(previewPath) ? _defaultPreviewPath : previewPath;
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

