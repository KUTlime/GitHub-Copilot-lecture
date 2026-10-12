using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AgentShop.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Minimal approval (snapshot) testing for JSON responses. See docs/adr/0008.
/// <list type="bullet">
/// <item>Approved snapshots live in tests/AgentShop.Api.IntegrationTests/Snapshots/*.approved.json and are committed.</item>
/// <item>On a mismatch a *.received.json file is written next to it (git-ignored) and the test fails.</item>
/// <item>Volatile values are normalized: GUIDs become Guid_1, Guid_2, ... and "traceId" is removed.</item>
/// <item>Accept an intended change by re-running with AGENTSHOP_ACCEPT_SNAPSHOTS=1 (ignored on CI). Review the diff first.</item>
/// </list>
/// </summary>
internal static partial class JsonSnapshot
{
    private const string AcceptVariable = "AGENTSHOP_ACCEPT_SNAPSHOTS";
    private static readonly string[] VolatileMembers = ["traceId"];

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly string SnapshotDirectory = Path.Combine(
        typeof(JsonSnapshot).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "ProjectDirectory").Value!,
        "Snapshots");

    public static async Task MatchAsync(string json)
    {
        var test = TestContext.Current;
        var className = test.TestClass?.TestClassName.Split('.')[^1] ?? throw new InvalidOperationException("Snapshots require a running test.");
        var name = $"{className}.{test.TestMethod!.MethodName}";
        var approvedPath = Path.Combine(SnapshotDirectory, $"{name}.approved.json");
        var receivedPath = Path.Combine(SnapshotDirectory, $"{name}.received.json");
        var cancellationToken = test.CancellationToken;

        var received = Normalize(json);
        var approved = File.Exists(approvedPath)
            ? (await File.ReadAllTextAsync(approvedPath, cancellationToken)).ReplaceLineEndings("\n")
            : null;

        if (string.Equals(received, approved, StringComparison.Ordinal))
        {
            File.Delete(receivedPath);
            return;
        }

        Directory.CreateDirectory(SnapshotDirectory);
        var isCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
        if (!isCi && Environment.GetEnvironmentVariable(AcceptVariable) == "1")
        {
            await File.WriteAllTextAsync(approvedPath, received, cancellationToken);
            File.Delete(receivedPath);
            return;
        }

        await File.WriteAllTextAsync(receivedPath, received, cancellationToken);
        Assert.Fail(
            $"Snapshot '{name}' {(approved is null ? "does not exist yet" : "does not match")}.{Environment.NewLine}" +
            $"Compare: {approvedPath}{Environment.NewLine}    with: {receivedPath}{Environment.NewLine}" +
            $"If (and only if) the change is intended, re-run with {AcceptVariable}=1 to accept it.");
    }

    private static string Normalize(string json)
    {
        var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("Snapshot content is not JSON.");
        ScrubInPlace(node, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        return node.ToJsonString(Indented).ReplaceLineEndings("\n") + "\n";
    }

    private static void ScrubInPlace(JsonNode node, Dictionary<string, string> guids)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var member in VolatileMembers)
                {
                    obj.Remove(member);
                }

                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (obj[key] is JsonValue value && value.TryGetValue<string>(out var text))
                    {
                        obj[key] = ScrubGuids(text, guids);
                    }
                    else if (obj[key] is { } child)
                    {
                        ScrubInPlace(child, guids);
                    }
                }

                break;

            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (array[i] is JsonValue value && value.TryGetValue<string>(out var text))
                    {
                        array[i] = ScrubGuids(text, guids);
                    }
                    else if (array[i] is { } child)
                    {
                        ScrubInPlace(child, guids);
                    }
                }

                break;
        }
    }

    private static string ScrubGuids(string text, Dictionary<string, string> guids) =>
        GuidPattern().Replace(text, match =>
        {
            if (!guids.TryGetValue(match.Value, out var placeholder))
            {
                placeholder = $"Guid_{guids.Count + 1}";
                guids[match.Value] = placeholder;
            }

            return placeholder;
        });

    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex GuidPattern();
}
