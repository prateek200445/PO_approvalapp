namespace POApprovalAPI.Services;

public interface IChatCompletionService
{
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default);
}
