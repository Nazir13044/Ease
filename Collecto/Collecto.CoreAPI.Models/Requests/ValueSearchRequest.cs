namespace Collecto.CoreAPI.Models.Requests
{
    public class ValueSearchRequest
    {
        public string Criteria { get; set; }
    }

    public class ValueAndPageSearchRequest : ValueSearchRequest
    {
        public int Skip { get; set; }
        public int PageSize { get; set; }
    }

    public class ValueAndStatusSearchRequest : ValueSearchRequest
    {
        public short Status { get; set; }
    }

    public class ValueStatusAndPageSearchRequest : ValueAndPageSearchRequest
    {
        public short Status { get; set; }
    }

    public class ValueStatusAndPageAndSortSearchRequest : ValueStatusAndPageSearchRequest
    {
        public string SortField { get; set; }
        public string SortOrder { get; set; }
    }

    public class ValueAndPageAndSortSearchRequest : ValueAndPageSearchRequest
    {
        public string SortField { get; set; }
        public string SortOrder { get; set; }
    }
}
