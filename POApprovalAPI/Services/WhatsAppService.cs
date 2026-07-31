using POApprovalAPI.Interfaces;

namespace POApprovalAPI.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        public async Task<bool> SendMessageAsync(string mobileNumber, string message)
        {
            await Task.CompletedTask;
            return true;
        }
    }
}

