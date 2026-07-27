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
            PaginationRequest? request,
            CancellationToken cancellationToken = default)
        {
            request ??= new PaginationRequest();

            var totalCount = await query.LongCountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<T>.Create(items, totalCount, request.PageNumber, request.PageSize);
        }

        public static async Task<PagedResult<TResult>> ToPagedResultAsync<T, TResult>(
            this IQueryable<T> query,
            PaginationRequest? request,
            Func<T, TResult> selector,
            CancellationToken cancellationToken = default)
        {
            request ??= new PaginationRequest();

            var totalCount = await query.LongCountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var projected = items.Select(selector).ToList();

            return PagedResult<TResult>.Create(projected, totalCount, request.PageNumber, request.PageSize);
        }

        public static Task<PagedResult<T>> ToPagedResultAsync<T>(
            this System.Collections.Generic.IEnumerable<T> source,
            PaginationRequest? request,
            CancellationToken cancellationToken = default)
        {
            request ??= new PaginationRequest();

            var list = source as System.Collections.Generic.IList<T> ?? source.ToList();
            var totalCount = (long)list.Count;

            var items = list
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Task.FromResult(PagedResult<T>.Create(items, totalCount, request.PageNumber, request.PageSize));
        }
    }
}
