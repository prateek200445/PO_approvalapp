namespace POApprovalAPI.Models;

public class ChatRequest
{
    public string Message { get; set; } = "";
    public int TopK { get; set; } = 3;
}

public class ChatResponse
{
    public string Answer { get; set; } = "";
    public string Sql { get; set; } = "";
    public List<RetrievedTableDto> TablesUsed { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public int RowCount { get; set; }
    public string? Warning { get; set; }
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
