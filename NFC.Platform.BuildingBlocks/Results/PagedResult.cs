using System;
using System.Collections.Generic;
using System.Linq;

namespace NFC.Platform.BuildingBlocks.Results
{
    public class PagedResult<T>
    {
        public List<T> Items { get; init; } = [];

        public int PageNumber { get; init; }

        public int PageSize { get; init; }

        public long TotalCount { get; init; }

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        protected PagedResult() { }

        public static PagedResult<T> Create(IEnumerable<T> items, long totalCount, int pageNumber, int pageSize)
        {
            return new PagedResult<T>
            {
                Items = items?.ToList() ?? [],
                TotalCount = totalCount,
                PageNumber = pageNumber < 1 ? 1 : pageNumber,
                PageSize = pageSize < 1 ? 10 : pageSize
            };
        }
    }
}
