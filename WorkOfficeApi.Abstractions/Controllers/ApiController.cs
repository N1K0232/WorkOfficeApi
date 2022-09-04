using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WorkOfficeApi.Abstractions.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public abstract class ApiController : ControllerBase
{
	protected ApiController()
	{
	}
}