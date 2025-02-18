namespace VinWallet.API.Service.Interfaces
{
    public interface IVNPayService
    {
        string GeneratePaymentUrl(string amount, string infor);
        Task<bool> ProcessPaymentConfirmation(string queryString);
    }
}
