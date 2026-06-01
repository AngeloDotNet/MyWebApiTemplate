namespace MinimalApi.Template.Api;

public class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		//builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

		builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
		{
			loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
			loggerConfiguration.ReadFrom.Services(services);
		});

		var appSettings = builder.Services.ConfigureAndGet<AppSettings>(builder.Configuration, nameof(AppSettings)) ?? new();
		var toolDocumentation = appSettings.ApiDocumentationTool;

		var swaggerSettings = new SwaggerSettings();
		var scalarSettings = new ScalarSettings();

		if (toolDocumentation == ApiDocumentationTool.SwaggerUI)
		{
			swaggerSettings = builder.Services.ConfigureAndGet<SwaggerSettings>(builder.Configuration, nameof(SwaggerSettings)) ?? new();
		}
		else if (toolDocumentation == ApiDocumentationTool.Scalar)
		{
			scalarSettings = new ScalarSettings()
			{
				Title = $"{builder.Environment.ApplicationName} API Reference",

				// Optional, default is true
				//DarkMode = true,

				// Optional, default is true
				//ShowSidebar = true,

				// Optional, default is Never.
				//ShowDeveloperToolsVisibility = DeveloperToolsVisibility.Never,

				// Optional, default is Mars
				Theme = ScalarTheme.BluePlanet,

				// Optional, default is CSharp
				//Target = ScalarTarget.CSharp,

				// Optional, default is HttpClient
				//Client = ScalarClient.HttpClient
			};
		}

		builder.Services.AddHttpContextAccessor();
		builder.Services.AddScoped<TestEndpointContext<TestEndpoints>>();

		builder.Services.AddSingleton<ILogEventEnricher, HttpContextEnricher>();
		builder.Services.AddSingleton(TimeProvider.System);

		builder.Services.AddSingleton<ClientTimeProvider>();
		builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

		builder.Services.AddDbContext<ApplicationDbContext>(options =>
		{
			var connectionString = builder.Configuration.GetConnectionString("SqlConnection")!;
			options.UseSqlServer(connectionString, opt =>
			{
				opt.CommandTimeout(60); // Set the command timeout to 60 seconds.
				opt.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);

				opt.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
				opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

				// Set the compatibility level to the highest supported by your SQL Server version.
				// Docs: https://learn.microsoft.com/it-it/sql/t-sql/statements/alter-database-transact-sql-compatibility-level?view=sql-server-ver17

				//opt.UseCompatibilityLevel(140); // SQL Server 2017
				//opt.UseCompatibilityLevel(150); // SQL Server 2019
				//opt.UseCompatibilityLevel(160); // SQL Server 2022
				opt.UseCompatibilityLevel(170); // SQL Server 2025
			});

			// Enable detailed errors and sensitive data logging in development environment for better debugging.
			if (builder.Environment.IsDevelopment())
			{
				options.EnableDetailedErrors();
				options.EnableSensitiveDataLogging();
			}

			options.LogTo(Console.WriteLine, LogLevel.Information);
			options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

			options.UseExceptionProcessor();
			options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
		});

		builder.Services.AddOperationResult(options =>
		{
			options.ErrorResponseFormat = ErrorResposeFormatOR.List;

			// Adds a custom mapping between CustomFailureReasons.NotAvailable (1001) and the 501 HTTP Status Code.
			// Adding new mappings or editing the existing ones allows to define what HTTP Status Codes the API must return
			// for the Operation Results of our Business Logic methods.

			// Rif: https://github.com/marcominerva/OperationResults/blob/master/samples/OperationResults.Sample.BusinessLayer/CustomFailureReasons.cs#L3
			options.StatusCodesMapping.Add(CustomFailureReasons.NotAvailable, StatusCodes.Status501NotImplemented);

			// If you just want to directly use HTTP status codes as failure reasons, set the following property to false.
			// In this way, the code you use with Result.Fail() will be used as response status code with no further mapping.
			//options.MapStatusCodes = false;
		});

		builder.Services.AddHttpJsonOptions();
		//builder.Services.AddSimpleAuthentication(builder.Configuration, "JwtSettings");

		// Add supported cultures for request localization. The cultures should be defined in the appsettings.json file under the SupportedCultures property.
		builder.Services.AddRequestLocalization(appSettings.SupportedCultures.Distinct().ToArray());
		builder.Services.AddResponseCompression();

		// If you want to support all specific cultures, you can use the following code instead of the above line:
		//builder.Services.AddRequestLocalization(options =>
		//{
		//    var supportedCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
		//    options.SupportedCultures = supportedCultures;
		//    options.SupportedUICultures = supportedCultures;
		//    options.DefaultRequestCulture = new RequestCulture("en-US");
		//});

		// Disable FluentValidation's built-in localization to use our custom error messages.
		// Rif: https://docs.fluentvalidation.net/en/latest/localization.html#disabling-localization
		ValidatorOptions.Global.LanguageManager.Enabled = false;

		builder.Services.AddValidatorsFromAssemblyContaining<Program>();
		builder.Services.ConfigureValidation(options =>
		{
			options.ErrorResponseFormat = ErrorResponseFormat.List;

			// The default message is "One or more validation errors occurred", if you want to show the number of errors, uncomment the line below.
			//options.ValidationErrorTitleMessageFactory = (context, errors) => $"There was {errors.Values.Sum(v => v.Length)} validation error(s) occurred";
		});

		builder.Services.AddOpenApiOperationParameters(options =>
		{
			options.Parameters.Add(new()
			{
				Name = TimeZoneService.HeaderKey,
				In = ParameterLocation.Header,
				Required = false,
				Schema = OpenApiSchemaHelper.CreateStringSchema()
			});

			//    options.Parameters.Add(new()
			//    {
			//        Name = "x-tenant",
			//        In = ParameterLocation.Header,
			//        Required = true,
			//        Schema = OpenApiSchemaHelper.CreateStringSchema()
			//    });

			//    options.Parameters.Add(new()
			//    {
			//        Name = "x-environment",
			//        In = ParameterLocation.Header,
			//        Schema = OpenApiSchemaHelper.CreateSchema<Environment>(Environment.Production)
			//    });
		});

		var apiOptionSettings = new OpenApiOptionSettings();
		var apiPolicyOptions = new List<ApiPoliciesSettings>();

		builder.Services.AddVersioningApi(appSettings.ApiVersions, builder.Configuration, apiOptionSettings, apiPolicyOptions);
		builder.Services.AddDefaultProblemDetails();

		builder.Services.AddDefaultExceptionHandler();
		builder.Services.AddCors(options =>
		{
			options.AddDefaultPolicy(builder =>
			{
				builder.AllowAnyHeader()
					//.AllowAnyMethod()
					// TODO: Restrict the allowed origins in production.
					.WithMethods("GET", "POST", "PUT", "DELETE")
					.SetIsOriginAllowed(_ => true)
					.AllowCredentials().WithExposedHeaders(HeaderNames.ContentDisposition);
			});
		});

		var app = builder.Build();

		await ConfigureDatabaseAsync(app.Services);
		app.UseForwardedHeaders(new()
		{
			ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
			KnownProxies = { }
		});

		app.UseHttpsRedirection();
		app.UseExceptionHandler();

		app.MapOpenApi()
			// Allow anonymous access to the OpenAPI document for tools like Swagger UI.
			//.AllowAnonymous()

			// Generate a separate OpenAPI document for each API version.
			// The version is determined by the ApiVersion attribute on the controllers or by the default version defined in the API versioning configuration.
			// The Asp.Versioning.OpenApi Nuget package is currently in Release Candidate (RC 1)
			.WithDocumentPerVersion();

		// Map the OpenAPI document to the selected API documentation tool (Swagger UI or Scalar) based on the configuration in appsettings.json.
		//AppEngine.Tools.DependencyInjection.ServiceCollectionExtensions.MapDocumentationTool(appSettings, toolDocumentation, swaggerSettings, scalarSettings, app);
		//ServiceCollectionExtensionAET.MapDocumentationTool(appSettings, toolDocumentation, swaggerSettings, scalarSettings, app);
		app.UseSwaggerUI(options =>
		{
			var descriptions = app.DescribeApiVersions();

			foreach (var description in descriptions)
			{
				//options.SwaggerEndpoint($"/openapi/{description.GroupName}.json", description.GroupName);
				options.SwaggerEndpoint($"/openapi/{description.GroupName}.json", $"{app.Environment.ApplicationName} {description.GroupName}");
			}

			options.RoutePrefix = string.Empty; // Serve the Swagger UI at the app's root (e.g., https://localhost:5001/)
		});

		// Enable serving default files like index.html from wwwroot folder
		//app.UseDefaultFiles();

		//app.UseDefaultFiles(); // Enable serving default files like index.html from wwwroot folder
		//app.UseStaticFiles(); // Enable serving static files from wwwroot folder

		app.UseRouting();
		app.UseCors();

		app.UseRequestLocalization();
		//app.UseAuthentication();

		app.UseSerilogRequestLogging(options =>
		{
			options.IncludeQueryInRequestPath = true;
		});

		//app.UseAuthorization();
		app.UseResponseCompression();

		app.UseResponseCaching();
		//app.MapEndpointsFromAssemblyContaining<Program>(); // Automatically map endpoints from all controllers in the assembly.

		app.MapEndpoints(); // Automatically map endpoints from all controllers in the assembly.
		app.Run();

		static async Task ConfigureDatabaseAsync(IServiceProvider serviceProvider)
		{
			await using var scope = serviceProvider.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			await dbContext.Database.MigrateAsync();
		}
	}
}