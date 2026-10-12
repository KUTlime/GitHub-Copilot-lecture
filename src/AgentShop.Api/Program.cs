using AgentShop.Api.OpenApi;
using AgentShop.Modules.Catalog;
using AgentShop.Modules.Orders;
using AgentShop.Modules.Shipping;
using AgentShop.SharedKernel;
using FluentValidation;
using Scalar.AspNetCore;

// Validation messages are part of the API contract: always English, independent of the server culture.
ValidatorOptions.Global.LanguageManager.Enabled = false;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.CreateSchemaReferenceId = OpenApiSchemaNames.Create);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IIntegrationEventPublisher, InProcessIntegrationEventPublisher>();

builder.AddCatalogModule();
builder.AddOrdersModule();
builder.AddShippingModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Development and integration tests only. Production migrations are a deployment step (docs/adr/0005).
    await app.Services.MigrateCatalogModuleAsync(app.Lifetime.ApplicationStopping);
    await app.Services.MigrateOrdersModuleAsync(app.Lifetime.ApplicationStopping);
    await app.Services.MigrateShippingModuleAsync(app.Lifetime.ApplicationStopping);

    app.MapScalarApiReference();
}

app.MapOpenApi();

app.MapCatalogModule();
app.MapOrdersModule();
app.MapShippingModule();

app.MapDefaultEndpoints();

await app.RunAsync();

/// <summary>Entry point; public partial so integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
