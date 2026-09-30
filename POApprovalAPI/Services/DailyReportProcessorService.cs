namespace POApprovalAPI.Services
{
    public class DailyReportProcessorService
    {
        private readonly DailyReportDigestService _digest;

        public DailyReportProcessorService(DailyReportDigestService digest)
        {
            _digest = digest;
        }

        public Task<DailyReportDigestStatusDto> ProcessTodayReportsAsync() =>
            _digest.SendAsync(_digest.LocalNow.Date, force: false, triggeredBy: "processor");
    }
}
