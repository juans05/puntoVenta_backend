namespace Domain.Models
{
    public static class SD
    {
        public static string ApiBase { get; set; }

        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
