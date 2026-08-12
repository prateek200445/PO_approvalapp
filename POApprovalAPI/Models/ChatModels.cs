namespace POApprovalAPI.Models;

public class ChatRequest
{
    public string Message { get; set; } = "";
    public int TopK { get; set; } = 3;
}

public class ChatExportRequest
{
    public string Sql { get; set; } = "";
}

public class ChatResponse
{
    public string Answer { get; set; } = "";
    public string Sql { get; set; } = "";
    public List<RetrievedTableDto> TablesUsed { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    /// <summary>Rows returned in this chat payload (capped at MaxReturnRows).</summary>
    public int RowCount { get; set; }
    /// <summary>Full matching row count when a companion COUNT query succeeded.</summary>
    public int? TotalCount { get; set; }
    /// <summary>True when chat rows are a sample of a larger result set.</summary>
    public bool Truncated { get; set; }
    public string? Warning { get; set; }
}

public class ChatExportResult
{
    public byte[] CsvBytes { get; set; } = [];
    public string FileName { get; set; } = "export.csv";
    public int RowCount { get; set; }
    public int? TotalCount { get; set; }
    public bool Truncated { get; set; }
}

public class RetrievedTableDto
{
    public string ObjectName { get; set; } = "";
    public string Domain { get; set; } = "";
    public double Score { get; set; }
}

public class RetrievedSchemaChunk
{
    public string Id { get; set; } = "";
    public string ObjectName { get; set; } = "";
    public string? ObjectType { get; set; }
    public string? Domain { get; set; }
    public double Score { get; set; }
    public string EmbeddingText { get; set; } = "";
}
