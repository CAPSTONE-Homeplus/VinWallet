using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Constants
{
    public static class VinLaundryApiEndPointConstant
    {
        static VinLaundryApiEndPointConstant()
        {
        }
        public const string baseUrl = "https://vinlaundry.onrender.com";
        public const string RootEndPoint = baseUrl + "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndPoint + ApiVersion;

        public static class Order
        {
            public const string OrdersEndpoint = ApiEndpoint + "/orders";
            public const string OrderEndpoint = OrdersEndpoint + "/{id}";
            public const string OrderEndpointForPayment = OrderEndpoint + "/payment";
        }
    }
}
