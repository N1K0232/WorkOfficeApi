using AutoMapper;
using WorkOfficeApi.Shared.Models;
using WorkOfficeApi.Shared.Requests;
using Entities = WorkOfficeApi.DataAccessLayer.Entities;

namespace WorkOfficeApi.BusinessLayer.MapperProfiles;

internal sealed class WorkerMapperProfile : Profile
{
	public WorkerMapperProfile()
	{
		CreateMap<Entities.Worker, Worker>();
		CreateMap<SaveWorkerRequest, Entities.Worker>();
	}
}