using WorkOfficeApi.DataAccessLayer.Entities.Common;

namespace WorkOfficeApi.DataAccessLayer;

public interface IReadOnlyDataContext
{
    IQueryable<TEntity> GetData<TEntity>(bool ignoreQueryFilters = false, bool trackingChanges = false) where TEntity : BaseEntity;

    Task<TEntity> GetAsync<TEntity>(params object[] keyValues) where TEntity : BaseEntity;
}