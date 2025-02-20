using VinWallet.Domain.Paginate;
using VinWallet.Repository.Payload.Request.PaymentMethodRequest;
using VinWallet.Repository.Payload.Response.PaymentMethodResponse;

namespace VinWallet.API.Service.Interfaces
{
    public interface IPaymentMethodService
    {
        public Task<PaymentMethodResponse> CreateNew(PaymentMethodRequest request);

        public Task<IPaginate<PaymentMethodResponse>> GetAll(string? search, string? orderBy, int page, int size);


        public Task<PaymentMethodResponse> GetById(Guid id);

        public Task<PaymentMethodResponse> Update(Guid id, PaymentMethodRequest request);

        public Task<bool> Delete(Guid id);
    }
}
