using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Domain.Common;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.Infrastructure.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IGenericRepository{T}"/> for database CRUD operations.
    /// </summary>
    /// <typeparam name="T">The entity type inheriting from <see cref="BaseEntity"/>.</typeparam>
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        /// <inheritdoc />
        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <inheritdoc />
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        /// <inheritdoc />
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        /// <inheritdoc />
        public void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        /// <inheritdoc />
        public void Remove(T entity)
        {
            // Soft Delete implementation: Set IsDeleted to true and trigger entity update state.
            entity.IsDeleted = true;
            Update(entity);
        }

        /// <inheritdoc />
        public void HardRemove(T entity)
        {
            _dbSet.Remove(entity);
        }

        /// <inheritdoc />
        public void HardRemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate != null 
                ? await _dbSet.CountAsync(predicate) 
                : await _dbSet.CountAsync();
        }

        /// <inheritdoc />
        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }
    }
}
