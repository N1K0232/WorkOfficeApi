using Microsoft.AspNetCore.Mvc;
using WorkOfficeApi.Abstractions.Controllers;
using WorkOfficeApi.BusinessLayer.Services.Interfaces;
using WorkOfficeApi.Shared.Requests;

namespace WorkOfficeApi.Controllers;

public sealed class WorkersController : ApiController
{
	private readonly IWorkerService workerService;

	public WorkersController(IWorkerService workerService)
	{
		this.workerService = workerService;
	}

	[HttpDelete("Delete")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(Guid workerId)
	{
		await workerService.DeleteAsync(workerId);
		return Ok("worker successfully deleted");
	}

	[HttpGet("Get/{workerId:guid}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Get(Guid workerId)
	{
		var worker = await workerService.GetAsync(workerId);
		if (worker is not null)
		{
			return Ok(worker);
		}

		return NotFound("no worker found");
	}

	[HttpGet("Get/{pageIndex}/{itemsPerPage}/{orderBy}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Get(int pageIndex = 0, int itemsPerPage = 50, string orderBy = "FirstName")
	{
		var workers = await workerService.GetAsync(pageIndex, itemsPerPage, orderBy);
		if (workers is not null)
		{
			return Ok(workers);
		}

		return NotFound("no worker found");
	}

	[HttpPost("Save")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Save([FromBody] SaveWorkerRequest request)
	{
		var savedWorker = await workerService.SaveAsync(request);
		return Ok(savedWorker);
	}
}