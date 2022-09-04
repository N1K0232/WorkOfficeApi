using WorkOfficeApi.DataAccessLayer.Entities.Common;

namespace WorkOfficeApi.DataAccessLayer;

public interface IDataContext : IReadOnlyDataContext
{
    void Delete<TEntity>(TEntity entity) where TEntity : BaseEntity;

    void Delete<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity;

    void Edit<TEntity>(TEntity entity) where TEntity : BaseEntity;

    void Insert<TEntity>(TEntity entity) where TEntity : BaseEntity;

    Task SaveAsync();

    Task ExecuteTransactionAsync(Func<Task> action);
}