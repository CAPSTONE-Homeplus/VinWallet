﻿using VinWallet.Repository.Payload.Request;
using VinWallet.Repository.Payload.Response;

namespace VinWallet.API.Service.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest loginRequest);
    }
}
