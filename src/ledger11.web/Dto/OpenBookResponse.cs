using ledger11.model.Data;
using System.Collections.Generic;

namespace ledger11.web.Dto
{
    public class OpenBookResponse
    {
        public Dictionary<string, string?> Settings { get; set; } = new ();
        public List<Category> Categories { get; set; } = new ();
        public PaginatedResult<Transaction> Transactions { get; set; } = new ();
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new ();
        public int TotalCount { get; set; }
    }
}
