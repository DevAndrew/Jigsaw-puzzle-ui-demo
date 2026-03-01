using System.Collections.Generic;

namespace JigsawPrototype.Features.Home.Catalog
{
    public interface IPuzzleCatalogService
    {
        IReadOnlyList<PuzzleCatalogItem> GetItems();
        string DefaultPuzzleId { get; }
    }
}

