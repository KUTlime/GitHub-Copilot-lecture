var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL database; every module owns its own schema (catalog, orders, shipping).
var database = builder.AddPostgres("postgres")
    .AddDatabase("agentshop");

builder.AddProject<Projects.AgentShop_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

await builder.Build().RunAsync();
