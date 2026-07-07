using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            PaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            var totalCount = await query.LongCountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<T>.Create(items, totalCount, request.PageNumber, request.PageSize);
        }

        public static async Task<PagedResult<TResult>> ToPagedResultAsync<T, TResult>(
            this IQueryable<T> query,
            PaginationRequest request,
            Func<T, TResult> selector,
            CancellationToken cancellationToken = default)
        {
            var totalCount = await query.LongCountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var projected = items.Select(selector).ToList();

            return PagedResult<TResult>.Create(projected, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
