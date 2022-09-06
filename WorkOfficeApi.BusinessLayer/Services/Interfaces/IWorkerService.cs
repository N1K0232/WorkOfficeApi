using WorkOfficeApi.Shared.Common;
using WorkOfficeApi.Shared.Models;
using WorkOfficeApi.Shared.Requests;

namespace WorkOfficeApi.BusinessLayer.Services.Interfaces;

public interface IWorkerService : IDisposable
{
    Task DeleteAsync(Guid workerId);

    Task<Worker> GetAsync(Guid workerId);

    Task<ListResult<Worker>> GetAsync(int pageIndex, int itemsPerPage, string orderBy);

    Task<Worker> SaveAsync(SaveWorkerRequest request);
}