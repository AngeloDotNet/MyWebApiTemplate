using ErrorResponseFormat = AppEngine.Validation.ErrorResponseFormat;

namespace Template.WebApi;

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

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<ILogEventEnricher, HttpContextEnricher>();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ClientTimeProvider>();

        builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("SqlConnection")!;
            options.UseSqlServer(connectionString, opt =>
            {
                opt.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

                // Set the compatibility level to the highest supported by your SQL Server version.
                opt.UseCompatibilityLevel(170); // SQL Server 2025
                //opt.UseCompatibilityLevel(160); // SQL Server 2022
                //opt.UseCompatibilityLevel(150); // SQL Server 2019
                //opt.UseCompatibilityLevel(140); // SQL Server 2017
            });

            options.LogTo(Console.WriteLine, LogLevel.Information);

            // Enable detailed errors and sensitive data logging in development environment for better debugging.
            if (builder.Environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }

            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            options.UseExceptionProcessor();

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        var appSettings = builder.Services.ConfigureAndGet<AppSettings>(builder.Configuration, nameof(AppSettings)) ?? new();
        var swaggerSettings = builder.Services.ConfigureAndGet<SwaggerSettings>(builder.Configuration, nameof(SwaggerSettings)) ?? new();

        builder.Services.AddHttpJsonOptions();
        //builder.Services.AddSimpleAuthentication(builder.Configuration, appSettings.JWTSectionName);

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

        builder.Services.AddMemoryCache();
        // If you want to use in-memory caching, uncomment the following line.
        // AddDistributedMemoryCache is used in a test endpoint, if it is removed and there are
        // no endpoints that require the use of distributed cache you can comment out the line below
        builder.Services.AddDistributedMemoryCache();

        // If you want to use Redis for caching, uncomment the following lines.
        //builder.Services.AddStackExchangeRedisCache(options =>
        //{
        //    // Basic Redis configuration using a connection string. Replace with your Redis server endpoint and port.
        //    options.Configuration = "localhost:6379";

        //    // Prefix for all cache keys to avoid conflicts with other applications using the same Redis server.
        //    options.InstanceName = "NETWebApiTemplate:";

        //    // For a more advanced Redis configuration (comment out the configuration above, as it is overridden),
        //    // you can use ConfigurationOptions like this:
        //    //var redisSettings = builder.Services.ConfigureAndGet<RediSettings>(builder.Configuration, nameof(RediSettings)) ?? new();

        //    //options.InstanceName = redisSettings.InstanceName;
        //    //options.ConfigurationOptions = new ConfigurationOptions
        //    //{
        //    //    EndPoints = { redisSettings.EndPoints },
        //    //    Password = redisSettings.Password,
        //    //    ClientName = redisSettings.ClientName,
        //    //    KeepAlive = redisSettings.KeepAlive,
        //    //    Ssl = redisSettings.SslforAzure,
        //    //    AbortOnConnectFail = redisSettings.AbortOnConnectFail
        //    //};
        //});

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

        if (swaggerSettings.IsEnabled)
        {
            builder.Services.AddOpenApi(options =>
            {
                // Remove Servers list in OpenAPI.
                options.RemoveServerList();

                // Enable OpenAPI integration for simple authentication.
                //options.AddSimpleAuthentication(builder.Configuration, appSettings.JWTSectionName); 

                // Add Accept-Language header to all endpoints.
                options.AddAcceptLanguageHeader();

                // Add a default (error) response to all endpoints.
                options.AddDefaultProblemDetailsResponse();

                // Enable OpenAPI integration for custom parameters.
                options.AddOperationParameters();

                // Respect the ignored JsonNumberHandling attribute.
                //options.WriteNumberAsString(); 

                // Describe all query string parameters in Camel Case.
                //options.DescribeAllParametersInCamelCase();

                // Add time examples for TimeSpan and TimeOnly fields.
                //options.AddTimeExamples(); 
            });
        }

        builder.Services.AddDefaultProblemDetails();
        builder.Services.AddDefaultExceptionHandler();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true)
                    .AllowCredentials().WithExposedHeaders(HeaderNames.ContentDisposition);
            });
        });

        var app = builder.Build();
        //await ConfigureDatabaseAsync(app.Services);

        app.UseForwardedHeaders(new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            KnownProxies = { }
        });
        app.UseHttpsRedirection();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (swaggerSettings.IsEnabled)
        {
            if (swaggerSettings.RequireAuthentication)
            {
                app.UseMiddleware<SwaggerBasicAuthenticationMiddleware>();
            }

            //app.MapOpenApi().AllowAnonymous(); // Allow anonymous access to the OpenAPI document for tools like Swagger UI.
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", $"{app.Environment.ApplicationName} v1");
            });
        }

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

        app.MapEndpoints(); // Automatically map endpoints from all controllers in the assembly.
        //app.MapEndpointsFromAssemblyContaining<Program>(); // Automatically map endpoints from all controllers in the assembly.
        app.Run();

        //static async Task ConfigureDatabaseAsync(IServiceProvider serviceProvider)
        //{
        //    await using var scope = serviceProvider.CreateAsyncScope();
        //    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        //    await dbContext.Database.MigrateAsync();
        //}
    }
}