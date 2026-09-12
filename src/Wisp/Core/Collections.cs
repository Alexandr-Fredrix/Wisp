namespace Wisp.Core
{
    public sealed class CollectionGroup
    {
        public string Id = "", Title = "", Summary = "";
        public CollectionItem[] Items = new CollectionItem[0];
    }

    public sealed class CollectionItem
    {
        public string Id = "", Title = "", Location = "", Effect = "", Icon = "";
        public int CharmId;
        public CollectionImage[] Maps = new CollectionImage[0];
    }

    public sealed class CollectionImage
    {
        public string Label = "", Url = "";
        public bool Wide;
    }
}
