using AgentShop.Api.IntegrationTests.Infrastructure;
using Xunit.Sdk;
using Xunit.v3;

// One PostgreSQL container and one API host for the whole test run.
[assembly: AssemblyFixture(typeof(AgentShopApiFactory))]

// All tests share one database that is reset before each test, so they must not run in parallel.
[assembly: Parallelization(Mode = ParallelMode.None)]
