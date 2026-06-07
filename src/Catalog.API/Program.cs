using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        //Them traceId từ Activity hiện tại(OpenTelemetry/ W3C Trace Context)
        ctx.ProblemDetails.Extensions["traceId"] =
            System.Diagnostics.Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;

        //them instance Default URI
        ctx.ProblemDetails.Instance ??= $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
    };
});

//Bật jwt authentication(đọc config Identity:Url + Identity:Audience)
builder.AddDefaultAuthentication();

//Định nghĩa policy AdminOnly
builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireRole("admin"));

var withApiVersioning = builder.Services.AddApiVersioning(options =>
{
    // Include "api-supported-versions" and "api-deprecated-versions" headers in all responses
    options.ReportApiVersions = true;
});

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStatusCodePages();

//Add: middleware auth (Dat truoc MapCatalogApi)
app.UseAuthentication();
app.UseAuthorization();

// DEBUG endpoint: xem Catalog đang nhìn thấy claim nào trong token
app.MapGet(
        "/debug/claims",
        (HttpContext ctx) =>
        {
            return Results.Ok(
                new
                {
                    IsAuthenticated = ctx.User.Identity?.IsAuthenticated,
                    AuthenticationType = ctx.User.Identity?.AuthenticationType,
                    Claims = ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToArray(),
                }
            );
        }
    )
    .RequireAuthorization();

app.MapCatalogApi();

app.UseDefaultOpenApi();
app.Run();
