namespace JigsawPrototype.Features.Home.Catalog
{
    public sealed class PuzzleCatalogItem
    {
        public string Id { get; }
        public string PreviewPath { get; }
        public int Order { get; }

        public PuzzleCatalogItem(string id, string previewPath, int order)
        {
            Id = id ?? "";
            PreviewPath = previewPath ?? "";
            Order = order;
        }
    }
}

