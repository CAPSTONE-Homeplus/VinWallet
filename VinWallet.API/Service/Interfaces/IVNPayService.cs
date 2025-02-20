using VinWallet.Repository.Payload.Response.VnPayDto;

namespace VinWallet.API.Service.Interfaces
{
    public interface IVNPayService
    {
        string GeneratePaymentUrl(string amount, string infor);
        Task<VnPayPaymentConfirmDto> ProcessPaymentConfirmation(string queryString);
    }
}
