using System;
using System.Collections.Generic;

namespace JigsawPrototype.Features.Home.Catalog
{
    public sealed class StaticPuzzleCatalogService : IPuzzleCatalogService
    {
        private readonly List<PuzzleCatalogItem> _items;

        public string DefaultPuzzleId { get; }

        public StaticPuzzleCatalogService(IEnumerable<PuzzleCatalogItem> items, string defaultPuzzleId)
        {
            _items = new List<PuzzleCatalogItem>();
            if (items != null)
            {
                _items.AddRange(items);
            }

            _items.Sort((a, b) => a.Order.CompareTo(b.Order));
            DefaultPuzzleId = defaultPuzzleId ?? "";
        }

        public IReadOnlyList<PuzzleCatalogItem> GetItems()
        {
            return _items;
        }
    }
}

