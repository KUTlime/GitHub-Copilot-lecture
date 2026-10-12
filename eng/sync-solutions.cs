#:package Microsoft.VisualStudio.SolutionPersistence
// Generates AgentShop.sln from AgentShop.slnx (the single source of truth).
//   dotnet run eng/sync-solutions.cs             -> regenerate AgentShop.sln
//   dotnet run eng/sync-solutions.cs -- --check  -> exit code 1 when AgentShop.sln is out of sync
// Run from the repository root. The library requires absolute paths.
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

var root = Directory.GetCurrentDirectory();
var slnx = Path.Combine(root, "AgentShop.slnx");
var sln = Path.Combine(root, "AgentShop.sln");

if (!File.Exists(slnx))
{
    await Console.Error.WriteLineAsync("AgentShop.slnx not found. Run this script from the repository root.");
    return 1;
}

var model = await SolutionSerializers.SlnXml.OpenAsync(slnx, CancellationToken.None);

if (args.Contains("--check", StringComparer.Ordinal))
{
    if (!File.Exists(sln))
    {
        await Console.Error.WriteLineAsync("AgentShop.sln is missing. Run: dotnet run eng/sync-solutions.cs");
        return 1;
    }

    var existing = await SolutionSerializers.SlnFileV12.OpenAsync(sln, CancellationToken.None);
    if (!string.Equals(Signature(existing), Signature(model), StringComparison.Ordinal))
    {
        await Console.Error.WriteLineAsync("AgentShop.sln is out of sync with AgentShop.slnx. Never edit AgentShop.sln by hand; run: dotnet run eng/sync-solutions.cs");
        return 1;
    }

    Console.WriteLine("Solutions in sync.");
    return 0;
}

await SolutionSerializers.SlnFileV12.SaveAsync(sln, model, CancellationToken.None);
Console.WriteLine("Generated AgentShop.sln from AgentShop.slnx.");
return 0;

static string Signature(SolutionModel model)
{
    var projects = model.SolutionProjects
        .Select(p => $"{p.Parent?.Path}|{Normalize(p.FilePath)}")
        .Order(StringComparer.Ordinal);
    var folders = model.SolutionFolders
        .Select(f => $"{f.Path}:{string.Join(',', (f.Files ?? []).Select(Normalize).Order(StringComparer.Ordinal))}")
        .Order(StringComparer.Ordinal);
    return string.Join('\n', projects.Concat(folders));
}

static string Normalize(string path) => path.Replace('\\', '/');
