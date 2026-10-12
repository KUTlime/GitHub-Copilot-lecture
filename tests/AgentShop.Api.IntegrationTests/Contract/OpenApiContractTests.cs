using System.Net;
using AgentShop.Api.IntegrationTests.Infrastructure;

namespace AgentShop.Api.IntegrationTests.Contract;

/// <summary>
/// Guardrail for the public HTTP contract: any change to routes, DTOs or status codes shows up as a diff
/// of OpenApiContractTests.OpenApiDocument_MatchesApprovedContract.verified.json in the pull request.
/// Accept a new snapshot only when the contract change was explicitly requested.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OpenApiContractTests(AgentShopApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task OpenApiDocument_MatchesApprovedContract()
    {
        var response = await Client.GetAsync("/openapi/v1.json", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await JsonSnapshot.MatchAsync(await response.Content.ReadAsStringAsync(CancellationToken));
    }
}
