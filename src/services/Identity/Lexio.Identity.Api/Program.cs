using Lexio.BuildingBlocks.Auth;
using Lexio.BuildingBlocks.Observability;
using Lexio.BuildingBlocks.Web;
using Lexio.Identity.Api.Configuration;
using Lexio.Identity.Api.Endpoints;
using Lexio.Identity.Application;
using Lexio.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLexioObservability("Lexio.Identity");
builder.Services.AddLexioWeb();
builder.Services.AddLexioAuth(builder.Configuration);
builder.Services.AddLexioAuthorization();
builder.Services.AddLexioCors(builder.Configuration);
builder.Services.AddLexioRateLimits();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseLexioWeb();
app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapOpenApi();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapRoleEndpoints();
app.MapControllers();

// S6966: use RunAsync for proper async shutdown handling
await app.RunAsync();

// Expose Program for integration test WebApplicationFactory
// S1118: partial class is required by ASP.NET test infrastructure
#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118
