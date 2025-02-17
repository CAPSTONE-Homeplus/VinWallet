using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Constants
{
    public static class ApiEndPointConstant
    {
        static ApiEndPointConstant()
        {
        }

        public const string RootEndPoint = "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndPoint + ApiVersion;

        public static class Authentication
        {
            public const string AuthenticationEndpoint = ApiEndpoint + "/auth";
            public const string Login = AuthenticationEndpoint + "/login";
            public const string RefreshToken = AuthenticationEndpoint + "/refresh-token";
        }

        public static class User
        {
            public const string UsersEndpoint = ApiEndpoint + "/users";
            public const string UserEndpoint = UsersEndpoint + "/{id}";
            public const string WalletsOfUserEndpoint = UserEndpoint + "/wallets";
        }

        public static class Wallet
        {
            public const string WalletsEndpoint = ApiEndpoint + "/wallets";
            public const string WalletEndpoint = WalletsEndpoint + "/{id}";
        }

        public static class Transaction
        {
            public const string TransactionsEndpoint = ApiEndpoint + "/transactions";
            public const string TransactionEndpoint = TransactionsEndpoint + "/{id}";
        }

    }
}
