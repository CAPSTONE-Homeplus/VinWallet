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
        //public const string baseUrl = "https://localhost:7106";
        public const string RootEndPoint = baseUrl + "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndPoint + ApiVersion;


        public static class Room
        {
            public const string RoomsEndpoint = ApiEndpoint + "/rooms";
            public const string RoomEndpoint = RoomsEndpoint + "/{id}";
            public const string RoomByCodeEndpoint = RoomsEndpoint + "/by-code/{room-code}";

        }

        public static class Building
        {
            public const string BuildingsEndpoint = ApiEndpoint + "/buildings";
            public const string BuildingEndpoint = BuildingsEndpoint + "/{id}";

            public const string BuildingByCodeEndpoint = BuildingsEndpoint + "/by-code/{code}";
        }

        public static class House
        {
            public const string HousesEndpoint = ApiEndpoint + "/houses";
            public const string HouseEndpoint = HousesEndpoint + "/{id}";
            public const string HouseByCodeEndpoint = HousesEndpoint + "/by-code/{code}";
        }

        public static class Order
        {
            public const string OrdersEndpoint = ApiEndpoint + "/orders";
            public const string OrderEndpoint = OrdersEndpoint + "/{id}";
        }
    }
}
