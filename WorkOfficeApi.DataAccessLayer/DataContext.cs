using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Reflection;
using WorkOfficeApi.DataAccessLayer.Entities.Common;
using WorkOfficeApi.DataAccessLayer.Extensions;

namespace WorkOfficeApi.DataAccessLayer;

public sealed class DataContext : DbContext, IDataContext, IReadOnlyDataContext
{
	private readonly string connectionString;

	private IDbConnection connection;
	private CancellationTokenSource source;

	private bool disposed;

	public DataContext(DbContextOptions<DataContext> options) : base(options)
	{
		connectionString = Database.GetConnectionString();

		connection = null;
		source = null;

		disposed = false;
	}

	/// <summary>
	/// gets the open connection to the database
	/// </summary>
	private IDbConnection Connection
	{
		get
		{
			ThrowIfDisposed();

			//if the connection is null I create a new instance
			connection ??= new SqlConnection(connectionString);

			try
			{
				//if the connection is closed I open it
				if (connection.State is ConnectionState.Closed)
				{
					connection.Open();
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

			return connection;
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

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		ThrowIfDisposed();

		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		modelBuilder.ApplyQueryFilter(this);
		modelBuilder.ApplyTrimStringConverter();

		base.OnModelCreating(modelBuilder);
	}

	private IEnumerable<EntityEntry> Entries()
	{
		var entries = ChangeTracker.Entries()
			.Where(e => e.Entity.GetType().IsSubclassOf(typeof(BaseEntity)))
			.ToList();

		return entries.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
	}

	private void ApplyQueryFilter<TEntity>(ModelBuilder builder) where TEntity : DeletableEntity
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
			if (connection is not null)
			{
				if (connection.State is ConnectionState.Open)
				{
					connection.Close();
				}

				connection.Dispose();
				connection = null;
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
			Type currentType = typeof(DataContext);
			throw new ObjectDisposedException(currentType.FullName);
		}
	}
}