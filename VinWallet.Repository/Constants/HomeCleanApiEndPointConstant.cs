using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Constants
{
    public static class HomeCleanApiEndPointConstant
    {
        static HomeCleanApiEndPointConstant()
        {
        }
        public const string baseUrl = "https://homeclean.onrender.com";
        public const string RootEndPoint = baseUrl + "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndPoint + ApiVersion;


        public static class Room
        {
            public const string RoomsEndpoint = ApiEndpoint + "/rooms";
            public const string RoomEndpoint = RoomsEndpoint + "/{id}";
            public const string RoomByCodeEndpoint = RoomsEndpoint + "/by-code/{room-code}";

        }
    }
}
