using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Application.Interfaces.Repositories
{
    /// <summary>
    /// Generic repository contract to perform CRUD operations on domain entities inheriting from <see cref="BaseEntity"/>.
    /// All operations utilize Async/Await where appropriate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface IGenericRepository<T> where T : BaseEntity
    {
        /// <summary>
        /// Retrieves an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entity.</param>
        /// <returns>A task representing the async lookup, yielding the entity if found, otherwise null.</returns>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// Retrieves all entities.
        /// </summary>
        /// <returns>A task representing the async lookup, yielding an read-only list of all entities.</returns>
        Task<IReadOnlyList<T>> GetAllAsync();

        /// <summary>
        /// Finds entities matching the specified predicate expression.
        /// </summary>
        /// <param name="predicate">The filter condition.</param>
        /// <returns>A task representing the async lookup, yielding matching entities.</returns>
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds a new entity to the data set.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>A task representing the async addition.</returns>
        Task AddAsync(T entity);

        /// <summary>
        /// Adds a collection of entities to the data set.
        /// </summary>
        /// <param name="entities">The entities to add.</param>
        /// <returns>A task representing the async addition.</returns>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Marks an existing entity as modified.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        void Update(T entity);

        /// <summary>
        /// Performs soft delete on the entity by setting its IsDeleted flag to true and updating it.
        /// </summary>
        /// <param name="entity">The entity to soft delete.</param>
        void Remove(T entity);

        /// <summary>
        /// Physically deletes an entity from the database.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        void HardRemove(T entity);

        /// <summary>
        /// Physically deletes a collection of entities from the database.
        /// </summary>
        /// <param name="entities">The entities to delete.</param>
        void HardRemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// Counts the number of entities matching an optional filter.
        /// </summary>
        /// <param name="predicate">An optional filter condition.</param>
        /// <returns>A task representing the async count operation, yielding the matching count.</returns>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        /// <summary>
        /// Exposes the underlying dataset as an IQueryable query builder for custom service filtering.
        /// </summary>
        /// <returns>An IQueryable instance of the dataset.</returns>
        IQueryable<T> GetQueryable();
    }
}
