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

        var psi = new ProcessStartInfo
        {
            FileName = ResolvePython(),
            WorkingDirectory = _chatbotDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
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

    private static string ResolvePython()
    {
        var candidates = new[] { "python", "py" };
        foreach (var c in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = c,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) continue;
                p.WaitForExit(3000);
                if (p.ExitCode == 0) return c;
            }
            catch
            {
                // try next
            }
        }

        throw new InvalidOperationException(
            "Python was not found on PATH. Install Python and fastembed to enable schema RAG.");
    }
}
