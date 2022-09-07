using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Data;
using System.Reflection;
using WorkOfficeApi.DataAccessLayer.Entities.Common;

namespace WorkOfficeApi.DataAccessLayer;

public sealed class DataContext : DbContext,
	IDataContext,
	IReadOnlyDataContext,
	IDapperContext
{
	private static readonly MethodInfo queryFilterMethod;
	private readonly ValueConverter<string, string> trimStringConverter;

	private readonly string connectionString;

	private SqlConnection activeConnection;
	private CancellationTokenSource source;

	private bool disposed;

	static DataContext()
	{
		Type type = GetCurrentType();
		BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
		IEnumerable<MethodInfo> methods = type.GetMethods(bindingFlags);
		string methodName = nameof(SetQueryFilter);

		queryFilterMethod = methods.Single(t => t.IsGenericMethod && t.Name == methodName);
	}

	/// <summary>
	/// creates a new instance of the <see cref="DataContext"/> class
	/// </summary>
	/// <param name="options">the options for this context</param>
	public DataContext(DbContextOptions<DataContext> options) : base(options)
	{
		trimStringConverter = new ValueConverter<string, string>(v => v.Trim(), v => v.Trim());

		connectionString = Database.GetConnectionString();

		activeConnection = null;
		source = null;

		disposed = false;
	}

	/// <summary>
	/// gets the open connection to the database
	/// </summary>
	private IDbConnection InnerConnection
	{
		get
		{
			ThrowIfDisposed();

			//if the connection is null I create a new instance
			activeConnection ??= new SqlConnection(connectionString);

			try
			{
				//if the connection is closed I open it
				if (activeConnection.State is ConnectionState.Closed)
				{
					activeConnection.Open();
				}
			}
			catch (SqlException ex)
			{
				throw ex;
			}
			catch (InvalidOperationException ex)
			{
				throw ex;
			}

			return activeConnection;
		}
	}

	/// <summary>
	/// gets the <see cref="CancellationToken"/> for executing
	/// asynchronous operations
	/// </summary>
	private CancellationToken CancellationToken
	{
		get
		{
			ThrowIfDisposed();

			CancellationToken cancellationToken;
			source ??= new CancellationTokenSource();

			try
			{
				cancellationToken = source.Token;
			}
			catch (Exception)
			{
				source.Cancel();
				cancellationToken = source.Token;
			}

			return cancellationToken;
		}
	}

	/// <summary>
	/// deletes the specified entity in the database
	/// </summary>
	/// <typeparam name="TEntity">the entity type</typeparam>
	/// <param name="entity">the entity that will be deleted</param>
	/// <exception cref="ArgumentNullException">the entity is null</exception>
	/// <exception cref="ObjectDisposedException">the <see cref="DataContext"/> was disposed</exception>
	public void Delete<TEntity>(TEntity entity) where TEntity : BaseEntity
	{
		ThrowIfDisposed();

		if (entity is null)
		{
			throw new ArgumentNullException(nameof(entity));
		}

		Set<TEntity>().Remove(entity);
	}

	/// <summary>
	/// deletes a list of entity from the database
	/// </summary>
	/// <typeparam name="TEntity">the type of the entity</typeparam>
	/// <param name="entities">the list of entities</param>
	/// <exception cref="ArgumentNullException">the list is null or doesn't contains elements</exception>
	/// <exception cref="ObjectDisposedException">the <see cref="DataContext"/> was disposed</exception>
	public void Delete<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
	{
		ThrowIfDisposed();

		if (entities is null)
		{
			throw new ArgumentNullException(nameof(entities));
		}

		Set<TEntity>().RemoveRange(entities);
	}

	/// <summary>
	/// updates the specified entity in the database
	/// </summary>
	/// <typeparam name="TEntity">the entity type</typeparam>
	/// <param name="entity">the entity that will be edited</param>
	/// <exception cref="ArgumentNullException">the entity is null</exception>
	/// <exception cref="ObjectDisposedException">the <see cref="DataContext"/> was disposed</exception>
	public void Edit<TEntity>(TEntity entity) where TEntity : BaseEntity
	{
		ThrowIfDisposed();

		if (entity is null)
		{
			throw new ArgumentException("entity can't be null", nameof(entity));
		}

		Set<TEntity>().Update(entity);
	}

	/// <summary>
	/// gets the entity giving its id
	/// </summary>
	/// <typeparam name="TEntity">the entity type</typeparam>
	/// <param name="keyValues">the primary keys</param>
	/// <returns>the entity</returns>
	/// <exception cref="ObjectDisposedException">the <see cref="DataContext"/> was disposed</exception>
	public Task<TEntity> GetAsync<TEntity>(params object[] keyValues) where TEntity : BaseEntity
	{
		ThrowIfDisposed();

		CancellationToken token = CancellationToken;

		var set = Set<TEntity>();
		return set.FindAsync(keyValues, token).AsTask();
	}

	/// <summary>
	/// gets the query for the specified table
	/// </summary>
	/// <typeparam name="TEntity">the entity type</typeparam>
	/// <param name="ignoreQueryFilters">true if I want to retrieve all the entities even if they have a specified query filter. otherwise false</param>
	/// <param name="trackingChanges">true if EntityFramework ChangeTracker should tracking the entity for eventual updates. otherwise false/></param>
	/// <returns>the query for the specified table</returns>
	/// <exception cref="ObjectDisposedException">the <see cref="DataContext"/> was disposed</exception>
	public IQueryable<TEntity> GetData<TEntity>(bool ignoreQueryFilters = false, bool trackingChanges = false) where TEntity : BaseEntity
	{
		ThrowIfDisposed();

		var set = Set<TEntity>().AsQueryable();

		if (ignoreQueryFilters)
		{
			set = set.IgnoreQueryFilters();
		}

		return trackingChanges ?
			set.AsTracking() :
			set.AsNoTrackingWithIdentityResolution();
	}

	/// <summary>
	/// adds the specified entity in the database
	/// </summary>
	/// <typeparam name="TEntity">the entity typ</typeparam>
	/// <param name="entity">the entity that will be added</param>
	/// /// <exception cref="ArgumentNullException">the entity is null</exception>
	/// <exception cref="ObjectDisposedException">the <see cref="DataContext"/> was disposed</exception>
	public void Insert<TEntity>(TEntity entity) where TEntity : BaseEntity
	{
		ThrowIfDisposed();

		if (entity is null)
		{
			throw new ArgumentException("entity can't be null", nameof(entity));
		}

		Set<TEntity>().Add(entity);
	}

	/// <summary>
	/// saves the changes made in the database
	/// </summary>
	/// <returns>the task of the current action</returns>
	public Task SaveAsync()
	{
		ThrowIfDisposed();

		CancellationToken token = CancellationToken;
		token.ThrowIfCancellationRequested();

		try
		{
			var entries = Entries();

			foreach (var entry in entries)
			{
				BaseEntity baseEntity = entry.Entity as BaseEntity;

				//checks if the entity is added in the database
				if (entry.State is EntityState.Added)
				{
					//if the entity is a DeletableEntity object
					//entity frameworks assignes these values to IsDeleted and DeletedDate
					//columns
					if (baseEntity is DeletableEntity deletableEntity)
					{
						deletableEntity.IsDeleted = false;
						deletableEntity.DeletedDate = null;
					}

					baseEntity.CreationDate = DateTime.UtcNow;
					baseEntity.UpdatedDate = null;
				}

				//if the state is modified EntityFramework sets the Date of update
				if (entry.State is EntityState.Modified)
				{
					baseEntity.UpdatedDate = DateTime.UtcNow;

					//when the entity is updated
					//if it was deleted, the IsDeleted property is set to false
					//and the DeletedDate is set to null
					if (baseEntity is DeletableEntity deletableEntity)
					{
						deletableEntity.IsDeleted = false;
						deletableEntity.DeletedDate = null;
					}
				}

				if (entry.State is EntityState.Deleted)
				{
					//if the entity state is deleted and the object derives from
					//DeletableEntity I set the state to modified and I modify the
					//IsDeleted and DeletedDate values
					if (baseEntity is DeletableEntity deletableEntity)
					{
						entry.State = EntityState.Modified;
						deletableEntity.IsDeleted = true;
						deletableEntity.DeletedDate = DateTime.UtcNow;
					}
				}
			}

			return SaveChangesAsync(token);
		}
		catch (DbUpdateConcurrencyException ex)
		{
			throw ex;
		}
		catch (DbUpdateException ex)
		{
			throw ex;
		}
		catch (OperationCanceledException ex)
		{
			throw ex;
		}
	}

	/// <summary>
	/// commits changes in the database if there are different
	/// changes between the tables
	/// </summary>
	/// <param name="action">the action that will be performed</param>
	/// <returns>the task of the current action</returns>
	public Task ExecuteTransactionAsync(Func<Task> action)
	{
		ThrowIfDisposed();

		if (action is null)
		{
			throw new ArgumentNullException(nameof(action), "cannot perform action");
		}

		CancellationToken token = CancellationToken;
		token.ThrowIfCancellationRequested();

		DatabaseFacade database = Database;
		IExecutionStrategy strategy = database.CreateExecutionStrategy();

		return strategy.ExecuteAsync(async () =>
		{
			using var transaction = await database.BeginTransactionAsync(token).ConfigureAwait(false);
			await action.Invoke().ConfigureAwait(false);
			await transaction.CommitAsync(token).ConfigureAwait(false);
		});
	}

	/// <summary>
	/// gets a list of entities from the database such as views
	/// </summary>
	/// <typeparam name="TEntity">the return type object</typeparam>
	/// <param name="sql">the query to execute</param>
	/// <param name="param">the parameter (default value: <see langword="null"/>)</param>
	/// <param name="transaction">the transaction (default value: <see langword="null"/>)</param>
	/// <param name="commandType">the commandType (default value: <see langword="null"/>)</param>
	/// <returns>the task with the list result</returns>
	public Task<IEnumerable<TEntity>> GetAsync<TEntity>(string sql,
		object param = null,
		IDbTransaction transaction = null,
		CommandType? commandType = null) where TEntity : class
	{
		ThrowIfDisposed();
		return InnerConnection.QueryAsync<TEntity>(sql, param, transaction, commandType: commandType);
	}

	/// <summary>
	/// executes an action in the database
	/// </summary>
	/// <param name="sql">the query</param>
	/// <param name="param">the parameter (default value: <see langword="null"/>)</param>
	/// <param name="transaction">the transaction (default value: <see langword="null"/>)</param>
	/// <param name="commandType">the commandType (default value: <see langword="null"/>)</param>
	/// <returns>the task with the executed action</returns>
	public Task ExecuteAsync(string sql,
		object param = null,
		IDbTransaction transaction = null,
		CommandType? commandType = null)
	{
		ThrowIfDisposed();
		return InnerConnection.ExecuteAsync(sql, param, transaction, commandType: commandType);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		base.ConfigureConventions(configurationBuilder);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		ApplyTrimStringConverter(modelBuilder);
		ApplyQueryFilter(modelBuilder);

		base.OnModelCreating(modelBuilder);
	}

	/// <summary>
	/// applies the converter to all the string properties in the database
	/// </summary>
	/// <param name="modelBuilder"></param>
	private void ApplyTrimStringConverter(ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(string))
				{
					modelBuilder.Entity(entityType.Name)
						.Property(property.Name)
						.HasConversion(trimStringConverter);
				}
			}
		}
	}

	/// <summary>
	/// Applies the query filter to all the <see cref="DeletableEntity"/> entities
	/// </summary>
	/// <param name="modelBuilder"></param>
	private void ApplyQueryFilter(ModelBuilder modelBuilder)
	{
		DataContext dataContext = this;

		var entities = modelBuilder.Model
			.GetEntityTypes()
			.Where(t => typeof(DeletableEntity).IsAssignableFrom(t.ClrType))
			.ToList();

		foreach (var type in entities.Select(t => t.ClrType))
		{
			var methods = SetGlobalQueryMethods(type);

			foreach (var method in methods)
			{
				var genericMethod = method.MakeGenericMethod(type);
				genericMethod.Invoke(dataContext, new object[] { modelBuilder });
			}
		}
	}

	/// <summary>
	/// creates a list of methods if the param type derives from <see cref="DeletableEntity"/> object/>
	/// </summary>
	/// <param name="type">the entity type</param>
	/// <returns>the list of methods</returns>
	private static IEnumerable<MethodInfo> SetGlobalQueryMethods(Type type)
	{
		var result = new List<MethodInfo>();

		if (typeof(DeletableEntity).IsAssignableFrom(type))
		{
			result.Add(queryFilterMethod);
		}

		return result;
	}

	/// <summary>
	/// gets all the <see cref="BaseEntity"/> entries tracked by the <see cref="ChangeTracker"/>
	/// that are added, modified or deleted
	/// </summary>
	/// <returns>the entities</returns>
	private IEnumerable<EntityEntry> Entries()
	{
		var entries = ChangeTracker.Entries()
			.Where(e => e.Entity.GetType().IsSubclassOf(typeof(BaseEntity)))
			.ToList();

		return entries.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
	}

	/// <summary>
	/// sets the query filter to all the entities that derives from <see cref="DeletableEntity"/> class
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	/// <param name="builder"></param>
	private void SetQueryFilter<TEntity>(ModelBuilder builder) where TEntity : DeletableEntity
	{
		ThrowIfDisposed();
		builder.Entity<TEntity>().HasQueryFilter(x => !x.IsDeleted && x.DeletedDate == null);
	}

	public override void Dispose()
	{
		base.Dispose();

		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (disposing && !disposed)
		{
			if (activeConnection is not null)
			{
				if (activeConnection.State is ConnectionState.Open)
				{
					activeConnection.Close();
				}

				activeConnection.Dispose();
				activeConnection = null;
			}

			if (source is not null)
			{
				source.Dispose();
				source = null;
			}

			disposed = true;
		}
	}

	private void ThrowIfDisposed()
	{
		if (disposed)
		{
			Type currentType = GetCurrentType();
			throw new ObjectDisposedException(currentType.FullName);
		}
	}

	private static Type GetCurrentType()
	{
		return typeof(DataContext);
	}
}