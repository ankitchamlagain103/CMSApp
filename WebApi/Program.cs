using Application;
using Infrastructure;
using Infrastructure.Persistence.DataSeeder;
using WebApi.Extensions;
using WebApi.Filters;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options => options.Filters.Add<AuthorizedAction>());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "v1";
    config.Title = "CMSApp API";
    config.Version = "v1";
    config.AddSecurity("JWT", Enumerable.Empty<string>(), new NSwag.OpenApiSecurityScheme
    {
        Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
        Name = "Authorization",
        In = NSwag.OpenApiSecurityApiKeyLocation.Header,
        Description = "Paste a JWT as: Bearer {token}"
    });
    config.OperationProcessors.Add(new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("JWT"));
});

var app = builder.Build();

try
{
    await IdentitySeeder.SeedAsync(app.Services);
    await MenuSeeder.SeedAsync(app.Services);
    await AppConfigSeeder.SeedAsync(app.Services);
    await ConfigCatalogSeeder.SeedAsync(app.Services);
    await DocumentTemplateSeeder.SeedAsync(app.Services);
    // Placeholder example fiscal year + tax slabs -- verify/replace before relying on for real payroll.
    await PayrollSeeder.SeedAsync(app.Services);
    // Development/demo data (school structure, teachers, students) -- remove for production.
    await SampleDataSeeder.SeedAsync(app.Services);
}
catch (Exception seedException)
{
    var seedLogger = app.Services.GetRequiredService<ILogger<Program>>();
    seedLogger.LogWarning(seedException, "Data seeding was skipped -- this is expected if the database hasn't been migrated yet.");
}

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "CMSApp API";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

app.UseHttpsRedirection();

// CORS is locked to the origins configured in App:AllowedOrigins. An empty list means no
// cross-origin access at all (correct while no frontend exists) -- never fall back to
// any-origin, especially not combined with AllowCredentials.
var allowedOrigins = builder.Configuration.GetSection("App:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (allowedOrigins.Length > 0)
{
    app.UseCors(policy => policy
        .WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
        .AllowAnyHeader()
        .AllowCredentials());
}

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
