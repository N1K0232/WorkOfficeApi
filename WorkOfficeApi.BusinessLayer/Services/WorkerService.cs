using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using WorkOfficeApi.BusinessLayer.Services.Interfaces;
using WorkOfficeApi.DataAccessLayer;
using WorkOfficeApi.Shared.Common;
using WorkOfficeApi.Shared.Models;
using WorkOfficeApi.Shared.Requests;
using Entities = WorkOfficeApi.DataAccessLayer.Entities;

namespace WorkOfficeApi.BusinessLayer.Services;

public sealed class WorkerService : IWorkerService
{
	private readonly IDataContext dataContext;
	private readonly IMapper mapper;

	public WorkerService(IDataContext dataContext, IMapper mapper)
	{
		this.dataContext = dataContext;
		this.mapper = mapper;
	}


	public async Task DeleteAsync(Guid workerId)
	{
		var worker = await dataContext.GetAsync<Entities.Worker>(workerId);
		dataContext.Delete(worker);
		await dataContext.SaveAsync();
	}

	public async Task<Worker> GetAsync(Guid workerId)
	{
		var dbWorker = await dataContext.GetAsync<Entities.Worker>(workerId);
		var worker = mapper.Map<Worker>(dbWorker);
		return worker;
	}

	public async Task<ListResult<Worker>> GetAsync(int pageIndex, int itemsPerPage, string orderBy)
	{
		var query = dataContext.GetData<Entities.Worker>();

		var workers = await query.OrderBy(orderBy)
			.Skip(pageIndex * itemsPerPage)
			.Take(itemsPerPage + 1)
			.ProjectTo<Worker>(mapper.ConfigurationProvider)
			.ToListAsync();

		var totalCount = await query.CountAsync();
		var hasNextPage = workers.Count > itemsPerPage;

		return new ListResult<Worker>(workers.Take(itemsPerPage), totalCount, hasNextPage);
	}

	public async Task<Worker> SaveAsync(SaveWorkerRequest request)
	{
		var dbWorker = request.Id != null ? await dataContext.GetData<Entities.Worker>(trackingChanges: true)
			.FirstOrDefaultAsync(w => w.Id == request.Id) : null;

		if (dbWorker == null)
		{
			dbWorker = mapper.Map<Entities.Worker>(request);
			dataContext.Insert(dbWorker);
		}
		else
		{
			mapper.Map(request, dbWorker);
			dataContext.Edit(dbWorker);
		}

		await dataContext.SaveAsync();

		var savedWorker = mapper.Map<Worker>(dbWorker);
		return savedWorker;
	}
}