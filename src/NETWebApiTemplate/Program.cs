using AppEngine.Routing;
using AppEngine.Tools;
using AppEngine.Tools.TimeZoneService;
using AppEngine.Tools.TimeZoneService.Interfaces;
using EntityFramework.Exceptions.SqlServer;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Net.Http.Headers;
using NETWebApiTemplate.BusinessLayer.Settings;
using NETWebApiTemplate.DataAccessLayer;
using NETWebApiTemplate.Logging;
using NETWebApiTemplate.Swagger;
using Serilog;
using Serilog.Core;

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

            // Ignore the warning about pending model changes, as we will handle migrations manually.
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

            options.UseExceptionProcessor();
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        builder.Services.AddHttpJsonOptions();
        builder.Services.AddSimpleAuthentication(builder.Configuration);

        var appSettings = builder.Services.ConfigureAndGet<AppSettings>(builder.Configuration, nameof(AppSettings)) ?? new();
        var swaggerSettings = builder.Services.ConfigureAndGet<SwaggerSettings>(builder.Configuration, nameof(SwaggerSettings)) ?? new();

        // Configure request localization with the supported cultures from app settings.
        builder.Services.AddRequestLocalization(appSettings.SupportedCultures.Distinct().ToArray());

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
        //builder.Services.AddValidatorsFromAssemblyContaining<MyCustomValidator>();

        builder.Services.AddOpenApiOperationParameters(options =>
        {
            options.Parameters.Add(new()
            {
                Name = TimeZoneService.HeaderKey,
                In = ParameterLocation.Header,
                Required = false,
                Schema = OpenApiSchemaHelper.CreateStringSchema()
            });
        });

        if (swaggerSettings.IsEnabled)
        {
            builder.Services.AddOpenApi(options =>
            {
                options.RemoveServerList();
                options.AddSimpleAuthentication(builder.Configuration);

                options.AddAcceptLanguageHeader();
                options.AddDefaultProblemDetailsResponse();

                options.AddOperationParameters();
            });
        }

        builder.Services.AddDefaultProblemDetails();
        builder.Services.AddDefaultExceptionHandler();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyHeader()
                    .AllowAnyMethod().SetIsOriginAllowed(_ => true)
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
            app.UseMiddleware<SwaggerBasicAuthenticationMiddleware>();

            app.MapOpenApi();
            //app.MapOpenApi().AllowAnonymous(); // Allow anonymous access to the OpenAPI document for tools like Swagger UI.
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", $"{app.Environment.ApplicationName} v1");
            });
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseCors();

        app.UseRequestLocalization();
        //app.UseAuthentication();

        app.UseSerilogRequestLogging(options =>
        {
            options.IncludeQueryInRequestPath = true;
        });
        //app.UseAuthorization();

        app.MapGet("/hello", () => "Hello, world!");

        app.MapEndpoints();
        app.Run();

        //static async Task ConfigureDatabaseAsync(IServiceProvider serviceProvider)
        //{
        //    await using var scope = serviceProvider.CreateAsyncScope();
        //    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        //    await dbContext.Database.MigrateAsync();
        //}
    }
}