using System.Collections.Generic;
using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Preview
{
    public sealed class InMemoryPuzzlePreviewCache : IPuzzlePreviewCache
    {
        private readonly int _capacity;
        private readonly Dictionary<string, Texture2D> _items = new();
        private readonly Queue<string> _order = new();

        public InMemoryPuzzlePreviewCache(int capacity)
        {
            _capacity = Mathf.Max(1, capacity);
        }

        public bool TryGet(string puzzleId, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrWhiteSpace(puzzleId))
            {
                return false;
            }

            return _items.TryGetValue(puzzleId, out texture) && texture != null;
        }

        public void Put(string puzzleId, Texture2D texture)
        {
            if (string.IsNullOrWhiteSpace(puzzleId) || texture == null)
            {
                return;
            }

            if (_items.ContainsKey(puzzleId))
            {
                _items[puzzleId] = texture;
                return;
            }

            while (_items.Count >= _capacity && _order.Count > 0)
            {
                var oldestId = _order.Dequeue();
                _items.Remove(oldestId);
            }

            _items[puzzleId] = texture;
            _order.Enqueue(puzzleId);
        }

        public void Clear()
        {
            _items.Clear();
            _order.Clear();
        }
    }
}

