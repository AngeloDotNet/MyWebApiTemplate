using AppEngine.Routing;
using AppEngine.Tools.TimeZoneService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MinimalApi.Template.BusinessLayer.Context;

public class TestEndpointContext<TEndpoints>(ILogger<TEndpoints> logger, ITimeZoneService timeZoneService, IHttpContextAccessor accessor)
	where TEndpoints : IEndpointRouteHandlerBuilder
{
	public ILogger<TEndpoints> Logger { get; } = logger;
	public ITimeZoneService TimeZoneService { get; } = timeZoneService;
	public HttpContext HttpContext { get; } = accessor.HttpContext!;
}