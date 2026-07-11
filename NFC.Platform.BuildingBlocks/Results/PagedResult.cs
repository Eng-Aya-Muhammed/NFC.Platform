using System;
using System.Collections.Generic;
using System.Linq;

namespace NFC.Platform.BuildingBlocks.Results
{
    /// <summary>
    /// Represents a paginated list of items.
    /// </summary>
    /// <typeparam name="T">The type of the items in the paginated list.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Gets the items on the current page.
        /// </summary>
        public List<T> Items { get; init; } = [];

        /// <summary>
        /// Gets the current page number (1-indexed).
        /// </summary>
        public int PageNumber { get; init; }

        /// <summary>
        /// Gets the page size (number of items per page).
        /// </summary>
        public int PageSize { get; init; }

        /// <summary>
        /// Gets the total number of items across all pages.
        /// </summary>
        public long TotalCount { get; init; }

        /// <summary>
        /// Gets the total number of pages based on total count and page size.
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
        /// </summary>
        protected PagedResult() { }

        /// <summary>
        /// Creates a new <see cref="PagedResult{T}"/> instance.
        /// </summary>
        /// <param name="items">The items on the current page.</param>
        /// <param name="totalCount">The total count of items.</param>
        /// <param name="pageNumber">The current page number.</param>
        /// <param name="pageSize">The size of the page.</param>
        /// <returns>A new <see cref="PagedResult{T}"/>.</returns>
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
