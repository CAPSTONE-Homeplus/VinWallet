using AutoMapper;
using VinWallet.Domain.Models;
using VinWallet.Repository.Payload.Request.TransactionRequest;
using VinWallet.Repository.Payload.Response.TransactionResponse;

namespace VinWallet.Repository.Mapper.TransactionMapper
{
    public class TransactionMapper : Profile
    {
        public TransactionMapper()
        {
            CreateMap<CreateTransactionRequest, Transaction>();
            CreateMap<Transaction, TransactionResponse>();
        }
    }
}
