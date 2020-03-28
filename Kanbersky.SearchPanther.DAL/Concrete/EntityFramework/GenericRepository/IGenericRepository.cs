using Kanbersky.SearchPanther.Core.Entity;
using Kanbersky.SearchPanther.Entities.Abstract;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Kanbersky.SearchPanther.DAL.Concrete.EntityFramework.GenericRepository
{
    public interface IGenericRepository<T> where T : BaseEntity, IEntity, new()
    {
        Task<T> Get(Expression<Func<T, bool>> expression);

        Task<List<T>> GetList(Expression<Func<T, bool>> expression = null);

        Task<T> Add(T entity);

        Task<T> Update(T entity);

        void Delete(int id);

        Task<int> SaveChangesAsync();

        IEnumerable<T> GetQueryable();
    }
}
