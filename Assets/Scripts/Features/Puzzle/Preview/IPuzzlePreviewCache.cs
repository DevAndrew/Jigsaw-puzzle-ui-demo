using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Preview
{
    public interface IPuzzlePreviewCache
    {
        bool TryGet(string puzzleId, out Texture2D texture);
        void Put(string puzzleId, Texture2D texture);
        void Clear();
    }
}

