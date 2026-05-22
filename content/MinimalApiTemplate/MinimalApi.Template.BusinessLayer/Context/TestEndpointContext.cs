namespace MinimalApi.Template.BusinessLayer.Context;

public class TestEndpointContext<TEndpoints>(ILogger<TEndpoints> logger, ITimeZoneService timeZoneService, IHttpContextAccessor accessor)
	where TEndpoints : IEndpointRouteHandlerBuilder
{
	public ILogger<TEndpoints> Logger { get; } = logger;
	public ITimeZoneService TimeZoneService { get; } = timeZoneService;
	public HttpContext HttpContext { get; } = accessor.HttpContext!;
}