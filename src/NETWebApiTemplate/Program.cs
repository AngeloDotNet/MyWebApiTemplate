using AppEngine.Extensions;

namespace Template.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpJsonOptions();

        builder.Services.AddOpenApi();

        var app = builder.Build();
        app.UseHttpsRedirection();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", $"{app.Environment.ApplicationName} v1"));
        }

        app.MapGet("/hello", () => "Hello, world!");

        app.UseRouting();
        app.Run();
    }
}