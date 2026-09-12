using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using POApprovalAPI.Services;

var contentRoot = @"C:\Users\gdmit\OneDrive\Desktop\PO_Code\POApprovalAPI";
var mode = args.Length > 0 ? args[0] : "--recompute";

var svc = new FibcBuyersService(
    new ConfigurationBuilder().AddInMemoryCollection().Build(),
    new SimpleHostEnv { ContentRootPath = contentRoot },
    NullLogger<FibcBuyersService>.Instance);

if (mode.Equals("--refresh-contacts", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(JsonSerializer.Serialize(svc.RefreshBuyerContacts(), new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}

if (mode.Equals("--recompute", StringComparison.OrdinalIgnoreCase) || !File.Exists(mode))
{
    Console.WriteLine(JsonSerializer.Serialize(svc.RecomputeAll(), new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}

await using var fs = File.OpenRead(mode);
var result = await svc.ImportFileAsync(fs, Path.GetFileName(mode), CancellationToken.None);
Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
return 0;

sealed class SimpleHostEnv : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "ImportFibcExim";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
