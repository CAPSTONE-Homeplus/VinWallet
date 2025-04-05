using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Constants
{
    public static class LaundryApiEndPointConstant
    {
        public const string baseUrl = "https://vinlaundry.onrender.com";
        //public const string baseUrl = "https://localhost:7106";
        public const string RootEndPoint = baseUrl + "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndPoint + ApiVersion;
        public static class Order
        {
            public const string OrderEndpoints = ApiEndpoint + "/orders";
            public const string OrderEndpoint = OrderEndpoints + "/{id}";
        }
    }
}
