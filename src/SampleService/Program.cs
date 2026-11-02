using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/version", () =>
{
    var version = app.GetType().Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "dev";
    return Results.Ok(new { version });
});

app.Run();

// Exposed so the test project can host the app in memory.
public partial class Program { }
