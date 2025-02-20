using AutoMapper;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Request.PaymentMethodRequest;
using VinWallet.Repository.Payload.Response.PaymentMethodResponse;

namespace VinWallet.API.Service.Implements
{
    public class PaymentMethodService : BaseService<PaymentMethodService>, IPaymentMethodService
    {
        public PaymentMethodService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<PaymentMethodService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public Task<PaymentMethodResponse> CreateNew(PaymentMethodRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Delete(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<IPaginate<PaymentMethodResponse>> GetAll(string? search, string? orderBy, int page, int size)
        {
            search = search?.Trim().ToLower();

            Func<IQueryable<PaymentMethod>, IOrderedQueryable<PaymentMethod>> orderByFunc = x => x.OrderByDescending(y => y.Id);

            if (!string.IsNullOrEmpty(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "createdat":
                        orderByFunc = x => x.OrderBy(y => y.CreatedAt);
                        break;
                    case "updatedat":
                        orderByFunc = x => x.OrderBy(y => y.UpdatedAt);
                        break;
                    case "status":
                        orderByFunc = x => x.OrderBy(y => y.Status);
                        break;
                    default:
                        orderByFunc = x => x.OrderByDescending(y => y.Id);
                        break;
                }
            }
            var paymentMethod = await _unitOfWork.GetRepository<PaymentMethod>().GetPagingListAsync(
                selector: x => new PaymentMethodResponse(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt, x.Status),
                predicate: string.IsNullOrEmpty(search) ? x => true : x => x.Name.ToLower().Contains(search),
                orderBy: orderByFunc,
                 page: page,
                size: size);

            return paymentMethod;
        }

        public Task<PaymentMethodResponse> GetById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentMethodResponse> Update(Guid id, PaymentMethodRequest request)
        {
            throw new NotImplementedException();
        }
    }

}
