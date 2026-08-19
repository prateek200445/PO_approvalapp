namespace POApprovalAPI.Planning.Execution;

internal static class ExecutionProductionHelper
{
    internal static int ParseLineFromTeam(string? teamNo)
    {
        if (string.IsNullOrWhiteSpace(teamNo))
            return 0;

        var trimmed = teamNo.Trim();
        if (int.TryParse(trimmed, out var direct))
            return direct;

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) ? parsed : 0;
    }

    internal static string NormalizeShift(string? shift) =>
        string.IsNullOrWhiteSpace(shift) ? "" : shift.Trim().ToUpperInvariant();

    internal static string LineShiftKey(int lineNo, string shift) =>
        $"{lineNo}|{NormalizeShift(shift)}";

    internal static string LineShiftDateKey(int lineNo, string shift, DateTime date) =>
        $"{lineNo}|{NormalizeShift(shift)}|{date:yyyy-MM-dd}";
}
