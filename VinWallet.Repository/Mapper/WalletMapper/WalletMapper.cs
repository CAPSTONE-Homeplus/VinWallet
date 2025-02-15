using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VinWallet.Domain.Models;
using VinWallet.Repository.Payload.Request.WalletRequest;
using VinWallet.Repository.Payload.Response.WalletResponse;

namespace VinWallet.Repository.Mapper.WalletMapper
{
    public class WalletMapper : Profile
    {
        public WalletMapper()
        {
            CreateMap<CreateWalletRequest, Wallet>();
            CreateMap<Wallet, WalletResponse>();
        }
    }
}
