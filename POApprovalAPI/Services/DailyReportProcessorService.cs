using POApprovalAPI.Interfaces;

namespace POApprovalAPI.Services
{
    public class DailyReportProcessorService
    {
        private readonly DailyReportService _dailyReportService;
        private readonly HtmlParserService _htmlParserService;
        private readonly MessageFormatterService _messageFormatterService;
        private readonly ManagerService _managerService;
        private readonly IWhatsAppService _whatsAppService;

        public DailyReportProcessorService(
            DailyReportService dailyReportService,
            HtmlParserService htmlParserService,
            MessageFormatterService messageFormatterService,
            ManagerService managerService,
            IWhatsAppService whatsAppService)
        {
            _dailyReportService = dailyReportService;
            _htmlParserService = htmlParserService;
            _messageFormatterService = messageFormatterService;
            _managerService = managerService;
            _whatsAppService = whatsAppService;
        }

        public async Task ProcessTodayReportsAsync()
        {
            var reports = await _dailyReportService.GetTodaysReports();

           foreach (var report in reports)
{
    var parsedReport = _htmlParserService.Parse(report);

    var message = _messageFormatterService.Format(parsedReport);

    var mobileNumber = "919978222000";

    await _whatsAppService.SendMessageAsync(mobileNumber, message);
}
        }
    }
}