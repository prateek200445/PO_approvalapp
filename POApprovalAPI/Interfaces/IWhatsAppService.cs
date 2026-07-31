namespace POApprovalAPI.Interfaces
{
    public interface IWhatsAppService
    {
        Task<bool> SendMessageAsync(string mobileNumber, string message);
    }
}