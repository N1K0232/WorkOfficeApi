using WorkOfficeApi.DataAccessLayer.Entities.Common;

namespace WorkOfficeApi.DataAccessLayer;

public interface IReadOnlyDataContext : IDisposable
{
    /// <summary>
    /// gets the query for the specified table
    /// </summary>
    /// <typeparam name="TEntity">the entity type</typeparam>
    /// <param name="ignoreQueryFilters">true if I want to retrieve all the entities even if they have a specified query filter. otherwise false</param>
    /// <param name="trackingChanges">true if EntityFramework ChangeTracker should tracking the entity for eventual updates. otherwise false/></param>
    /// <returns>the query for the specified table</returns>
    IQueryable<TEntity> GetData<TEntity>(bool ignoreQueryFilters = false, bool trackingChanges = false) where TEntity : BaseEntity;

    /// <summary>
    /// gets the entity giving its id
    /// </summary>
    /// <typeparam name="TEntity">the entity type</typeparam>
    /// <param name="keyValues">the primary keys</param>
    /// <returns>the entity</returns>
    Task<TEntity> GetAsync<TEntity>(params object[] keyValues) where TEntity : BaseEntity;
}