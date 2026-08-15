using System.Diagnostics;
using System.Text;
using System.Text.Json;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class SchemaRetrievalService
{
    private readonly ILogger<SchemaRetrievalService> _logger;
    private readonly string _chatbotDir;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SchemaRetrievalService(IWebHostEnvironment env, ILogger<SchemaRetrievalService> logger)
    {
        _logger = logger;
        _chatbotDir = Path.Combine(env.ContentRootPath, "Chatbot");
    }

    public async Task<IReadOnlyList<RetrievedSchemaChunk>> RetrieveAsync(
        string question,
        int topK = 5,
        CancellationToken ct = default)
    {
        topK = Math.Clamp(topK, 1, 8);
        var script = Path.Combine(_chatbotDir, "retrieve.py");
        if (!File.Exists(script))
            throw new InvalidOperationException($"Missing retrieve script at {script}");

        var python = ResolvePython();
        _logger.LogDebug("Schema RAG using Python at {Python}", python);

        var psi = new ProcessStartInfo
        {
            FileName = python,
            WorkingDirectory = _chatbotDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.ArgumentList.Add(script);
        psi.ArgumentList.Add(question);
        psi.ArgumentList.Add("--k");
        psi.ArgumentList.Add(topK.ToString());

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        if (!process.Start())
            throw new InvalidOperationException("Failed to start Python for schema retrieval.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);

        var output = stdout.ToString().Trim();
        if (process.ExitCode != 0)
        {
            _logger.LogError("retrieve.py failed: {Stderr}", stderr.ToString());
            throw new InvalidOperationException(
                $"Schema retrieval failed (exit {process.ExitCode}): {stderr}");
        }

        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException("Schema retrieval returned empty output.");

        using var doc = JsonDocument.Parse(output);
        if (doc.RootElement.TryGetProperty("error", out var err))
            throw new InvalidOperationException(err.GetString() ?? "retrieve error");

        var results = new List<RetrievedSchemaChunk>();
        foreach (var item in doc.RootElement.GetProperty("results").EnumerateArray())
        {
            results.Add(new RetrievedSchemaChunk
            {
                Id = item.GetProperty("id").GetString() ?? "",
                ObjectName = item.GetProperty("objectName").GetString() ?? "",
                ObjectType = item.TryGetProperty("objectType", out var ot) ? ot.GetString() : null,
                Domain = item.TryGetProperty("domain", out var d) ? d.GetString() : null,
                Score = item.GetProperty("score").GetDouble(),
                EmbeddingText = item.TryGetProperty("embeddingText", out var et)
                    ? et.GetString() ?? ""
                    : ""
            });
        }

        return results;
    }

    private static string? _cachedPython;

    private static string ResolvePython()
    {
        if (!string.IsNullOrEmpty(_cachedPython))
            return _cachedPython;

        foreach (var candidate in EnumeratePythonCandidates())
        {
            if (!TryValidatePython(candidate, out var resolved))
                continue;

            _cachedPython = resolved;
            return resolved;
        }

        throw new InvalidOperationException(
            "Python was not found. Install Python 3.12+ and fastembed, or set CHATBOT_PYTHON to the full path of python.exe.");
    }

    private static IEnumerable<string> EnumeratePythonCandidates()
    {
        foreach (var key in new[] { "CHATBOT_PYTHON", "PYTHON_PATH", "PYTHON_EXECUTABLE" })
        {
            var fromEnv = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                yield return fromEnv.Trim();
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        foreach (var versionDir in new[] { "Python313", "Python312", "Python311", "Python310" })
        {
            yield return Path.Combine(localAppData, "Programs", "Python", versionDir, "python.exe");
        }

        foreach (var name in new[] { "python", "py" })
        {
            foreach (var onPath in FindExecutablesOnPath(name))
                yield return onPath;
        }
    }

    private static IEnumerable<string> FindExecutablesOnPath(string command)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            yield break;

        var fileName = OperatingSystem.IsWindows() ? $"{command}.exe" : command;
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = dir.Trim();
            if (trimmed.Length == 0)
                continue;

            var fullPath = Path.Combine(trimmed, fileName);
            if (File.Exists(fullPath))
                yield return fullPath;
        }
    }

    private static bool IsWindowsAppsStub(string path) =>
        path.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase);

    private static bool TryValidatePython(string candidate, out string resolved)
    {
        resolved = candidate.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(resolved))
            return false;

        if (IsWindowsAppsStub(resolved))
            return false;

        // Allow bare command names only when not a WindowsApps stub.
        if (!resolved.Contains(Path.DirectorySeparatorChar)
            && !resolved.Contains('/')
            && !resolved.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            if (IsWindowsAppsStub(resolved))
                return false;
        }
        else if (!File.Exists(resolved))
        {
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = resolved,
                Arguments = "-c \"import numpy, fastembed\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null)
                return false;

            p.WaitForExit(15000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
