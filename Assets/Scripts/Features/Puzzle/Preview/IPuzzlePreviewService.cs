using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Preview
{
    public interface IPuzzlePreviewService
    {
        UniTask<Texture2D> GetPreviewAsync(string previewPath, CancellationToken ct);
    }
}

