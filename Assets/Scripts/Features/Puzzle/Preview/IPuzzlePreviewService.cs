using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Preview
{
    public interface IPuzzlePreviewService
    {
        UniTask<Sprite> GetPreviewAsync(string previewPath, CancellationToken ct);
    }
}
