using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;
using WorkOfficeApi.DataAccessLayer.Entities.Common;
using WorkOfficeApi.DataAccessLayer.Extensions;

namespace WorkOfficeApi.DataAccessLayer;

public sealed class DataContext : DbContext, IDataContext
{
	public DataContext(DbContextOptions<DataContext> options) : base(options)
	{
	}

	public void Delete<TEntity>(TEntity entity) where TEntity : BaseEntity
	{
		if (entity is null)
		{
			throw new ArgumentNullException(nameof(entity));
		}

		Set<TEntity>().Remove(entity);
	}

	public void Delete<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
	{
		if (entities is null)
		{
			throw new ArgumentNullException(nameof(entities));
		}

		Set<TEntity>().RemoveRange(entities);
	}

	public void Edit<TEntity>(TEntity entity) where TEntity : BaseEntity
	{
		if (entity is null)
		{
			throw new ArgumentException("entity can't be null", nameof(entity));
		}

		Set<TEntity>().Update(entity);
	}

	public Task<TEntity> GetAsync<TEntity>(params object[] keyValues) where TEntity : BaseEntity
	{
		using var source = new CancellationTokenSource();

		var set = Set<TEntity>();
		return set.FindAsync(keyValues, source.Token).AsTask();
	}

	public IQueryable<TEntity> GetData<TEntity>(bool ignoreQueryFilters = false, bool trackingChanges = false) where TEntity : BaseEntity
	{
		var set = Set<TEntity>().AsQueryable();

		if (ignoreQueryFilters)
		{
			set = set.IgnoreQueryFilters();
		}

		return trackingChanges ?
			set.AsTracking() : set.AsNoTrackingWithIdentityResolution();
	}

	public void Insert<TEntity>(TEntity entity) where TEntity : BaseEntity
	{
		if (entity is null)
		{
			throw new ArgumentException("entity can't be null", nameof(entity));
		}

		Set<TEntity>().Add(entity);
	}

	public Task SaveAsync()
	{
		using var source = new CancellationTokenSource();
		return SaveAsync(source.Token);
	}

	public Task ExecuteTransactionAsync(Func<Task> action)
	{
		using var source = new CancellationTokenSource();
		return ExecuteTransactionAsync(action, source.Token);
	}

	private Task SaveAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

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

			return SaveChangesAsync(cancellationToken);
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

	private Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
	{
		if (action is null)
		{
			throw new ArgumentNullException(nameof(action), "cannot perform action");
		}

		cancellationToken.ThrowIfCancellationRequested();

		var database = Database;
		var strategy = database.CreateExecutionStrategy();

		return strategy.ExecuteAsync(async () =>
		{
			using var transaction = await database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
			await action.Invoke().ConfigureAwait(false);
			await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
		});
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		OnModelBuilderInternal(modelBuilder);
		base.OnModelCreating(modelBuilder);
	}

	private void OnModelBuilderInternal(ModelBuilder builder)
	{
		builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		builder.ApplyQueryFilter(this);
		builder.ApplyTrimStringConverter();
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
		builder.Entity<TEntity>().HasQueryFilter(x => !x.IsDeleted && x.DeletedDate == null);
	}
}