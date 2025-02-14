using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VinWallet.Domain.Models;
using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Response.UserResponse;

namespace VinWallet.Repository.Mapper.UserMapper
{
    public class UserMapper : Profile
    {
        public UserMapper()
        {
            CreateMap<CreateUserRequest, User>();
            CreateMap<User, UserResponse>();
        }
    }
}
