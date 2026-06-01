namespace MinimalApi.Template.Api.Endpoints;

public class TestEndpoints : IEndpointRouteHandlerBuilder
{
	public static void MapEndpoints(IEndpointRouteBuilder endpoints)
	{
		var versionedApi = endpoints.NewVersionedApi().ReportApiVersions();
		var versionNeutralApi = versionedApi.MapGroup("test2").IsApiVersionNeutral();

		//var testGroup = endpoints
		//	.MapGroup("/test")
		//	.WithTags("Test Endpoints");

		//var testGroup = versionedApi
		var testGroup = versionNeutralApi
			.MapGroup("test")
			.WithTags("Test Endpoints");

		testGroup.MapGet("/hello", Result<HelloResponse> (HttpContext httpContext) =>
		{
			var response = new HelloResponse("Hello, world!");

			return response;

		})
		.Produces<HelloResponse>(StatusCodes.Status200OK)
		.WithName("GetHello")
		.WithDescription("Returns a greeting message.")
		.WithSummary("Provides a simple greeting message.");

		testGroup.MapGet("/datetime", async Task<Result<DateTimeResponse>> ([FromServices] IDistributedCache cache,
			ILogger<TestEndpoints> logger, ITimeZoneService timeZoneService, HttpContext httpContext, CancellationToken cancellationToken) =>
		{
			var timeZoneInfo = timeZoneService.GetTimeZone();

			if (timeZoneInfo is null)
			{
				var timeZoneId = timeZoneService.GetTimeZoneHeaderValue();

				if (timeZoneId is not null)
				{
					// If timeZoneInfo is null, but timeZoneId has a value, it means that the time zone specified in the header is invalid.
					return Result.Fail(FailureReasons.ClientError, "Unable to find the time zone", $"The time zone '{timeZoneId}' is invalid or is not available on the system");
				}
			}

			var cacheOptions = new DistributedCacheEntryOptions()
				.SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
				.SetSlidingExpiration(TimeSpan.FromMinutes(2));

			var cacheTask = await cache.GetOrSetAsync($"datetime-{timeZoneInfo}", async () =>
			{
				logger.LogInformation("Cache miss for 'datetime'. Fetching new datetime value");
				await Task.Delay(1000, cancellationToken);

				return GetCacheTaskValue(timeZoneInfo).ToString();
			}, cacheOptions, cancellationToken);

			if (cacheTask is null)
			{
				logger.LogError("Failed to retrieve 'datetime' from cache.");
				return Result.Fail(FailureReasons.Forbidden);
			}

			var response = new DateTimeResponse(cacheTask.ToString());

			return response;
		})
		.Produces<DateTimeResponse>(StatusCodes.Status200OK)
		.ProducesProblem(StatusCodes.Status403Forbidden)
		.WithName("GetDateTime")
		.WithDescription("Gets the current date and time.")
		.WithSummary("Retrieves the current date and time, utilizing caching for efficiency.");

		testGroup.MapGet("/datetime-nocache", async Task<Result<DateTimeResponse>> (TestEndpointContext<TestEndpoints> ctx, CancellationToken cancellationToken) =>
		{
			var timeZoneInfo = ctx.TimeZoneService.GetTimeZone();

			if (timeZoneInfo is null)
			{
				var timeZoneId = ctx.TimeZoneService.GetTimeZoneHeaderValue();

				if (timeZoneId is not null)
				{
					// If timeZoneInfo is null, but timeZoneId has a value, it means that the time zone specified in the header is invalid.
					return Result.Fail(FailureReasons.ClientError, "Unable to find the time zone", $"The time zone '{timeZoneId}' is invalid or is not available on the system");
				}
			}

			ctx.Logger.LogInformation("Cache miss for 'datetime'. Fetching new datetime value");
			await Task.Delay(1000, cancellationToken);

			var noCacheTask = GetCacheTaskValue(timeZoneInfo).ToString();

			if (noCacheTask is null)
			{
				ctx.Logger.LogError("Failed to retrieve 'datetime' from cache.");
				return Result.Fail(FailureReasons.Forbidden);
			}

			var response = new DateTimeResponse(noCacheTask.ToString());

			return response;
		})
		.Produces<DateTimeResponse>(StatusCodes.Status200OK)
		.ProducesProblem(StatusCodes.Status403Forbidden)
		.WithName("GetDateTime-NoCache")
		.WithDescription("Gets the current date and time.")
		.WithSummary("Retrieves the current date and time, utilizing caching for efficiency.");
	}

	private record class HelloResponse(string Message);
	private record class DateTimeResponse(string DateTime);

	private static DateTime GetCacheTaskValue(TimeZoneInfo? timeZoneInfo)
	{
		var dateTime = DateTime.Now;

		if (timeZoneInfo is null)
		{
			return dateTime;
		}
		else
		{
			return dateTime.Kind == DateTimeKind.Unspecified
				? TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), timeZoneInfo)
				: TimeZoneInfo.ConvertTime(dateTime, timeZoneInfo);
		}
	}
}