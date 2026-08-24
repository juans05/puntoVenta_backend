namespace Domain.Common
{
    public class DataCollection<T>
    {
        public bool HasItems
        {
            get
            {
                return Items != null && Items.Any();
            }
        }

        public int Total { get; set; }

        public int Page { get; set; }

        public int Pages { get; set; }

        public List<T>? Items { get; set; }

    }
}
