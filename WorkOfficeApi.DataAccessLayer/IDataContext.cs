using WorkOfficeApi.DataAccessLayer.Entities.Common;

namespace WorkOfficeApi.DataAccessLayer;

public interface IDataContext : IReadOnlyDataContext, IDisposable
{
    /// <summary>
    /// deletes the specified entity in the database
    /// </summary>
    /// <typeparam name="TEntity">the entity type</typeparam>
    /// <param name="entity">the entity that will be deleted</param>
    void Delete<TEntity>(TEntity entity) where TEntity : BaseEntity;

    /// <summary>
    /// deletes a list of entity from the database
    /// </summary>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    /// <param name="entities">the list of entities</param>
    void Delete<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity;

    /// <summary>
    /// updates the specified entity in the database
    /// </summary>
    /// <typeparam name="TEntity">the entity type</typeparam>
    /// <param name="entity">the entity that will be edited</param>
    void Edit<TEntity>(TEntity entity) where TEntity : BaseEntity;

    /// <summary>
    /// adds the specified entity in the database
    /// </summary>
    /// <typeparam name="TEntity">the entity typ</typeparam>
    /// <param name="entity">the entity that will be added</param>
    void Insert<TEntity>(TEntity entity) where TEntity : BaseEntity;

    /// <summary>
    /// saves the changes made in the database
    /// </summary>
    /// <returns>the task of the current action</returns>
    Task SaveAsync();

    /// <summary>
    /// commits changes in the database if there are different
    /// changes between the tables
    /// </summary>
    /// <param name="action">the action that will be performed</param>
    /// <returns>the task of the current action</returns>
    Task ExecuteTransactionAsync(Func<Task> action);
}