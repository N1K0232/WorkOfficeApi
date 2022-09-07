using System.Data;

namespace WorkOfficeApi.DataAccessLayer;

public interface IDapperContext
{
    /// <summary>
    /// gets a list of entities from the database such as views
    /// </summary>
    /// <typeparam name="TEntity">the return type object</typeparam>
    /// <param name="sql">the query to execute</param>
    /// <param name="param">the parameter (default value: <see langword="null"/>)</param>
    /// <param name="transaction">the transaction (default value: <see langword="null"/>)</param>
    /// <param name="commandType">the commandType (default value: <see langword="null"/>)</param>
    /// <returns>the task with the list result</returns>
    Task<IEnumerable<TEntity>> GetAsync<TEntity>(string sql,
        object param = null,
        IDbTransaction transaction = null,
        CommandType? commandType = null) where TEntity : class;

    /// <summary>
    /// executes an action in the database
    /// </summary>
    /// <param name="sql">the query</param>
    /// <param name="param">the parameter (default value: <see langword="null"/>)</param>
    /// <param name="transaction">the transaction (default value: <see langword="null"/>)</param>
    /// <param name="commandType">the commandType (default value: <see langword="null"/>)</param>
    /// <returns>the task with the executed action</returns>
    Task ExecuteAsync(string sql,
        object param = null,
        IDbTransaction transaction = null,
        CommandType? commandType = null);
}