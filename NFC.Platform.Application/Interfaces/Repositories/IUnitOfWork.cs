using System;
using System.Threading;
using System.Threading.Tasks;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Application.Interfaces.Repositories
{
    /// <summary>
    /// Unit of Work contract coordinating database save operations and transactions.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets a generic repository instance for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type inheriting from <see cref="BaseEntity"/>.</typeparam>
        /// <returns>An instance of <see cref="IGenericRepository{T}"/>.</returns>
        IGenericRepository<T> Repository<T>() where T : BaseEntity;

        /// <summary>
        /// Asynchronously saves all changes made inside this Unit of Work to the database.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A task representing the async save operation, yielding the count of affected rows.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a database transaction asynchronously.
        /// </summary>
        /// <returns>A task representing the async transaction start.</returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commits the active database transaction asynchronously.
        /// </summary>
        /// <returns>A task representing the async transaction commit.</returns>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rolls back the active database transaction asynchronously.
        /// </summary>
        /// <returns>A task representing the async transaction rollback.</returns>
        Task RollbackTransactionAsync();
    }
}
