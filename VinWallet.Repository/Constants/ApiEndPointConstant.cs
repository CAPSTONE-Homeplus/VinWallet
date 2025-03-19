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
            public const string AdminLogin = AuthenticationEndpoint + "/admin/login";
            public const string RefreshToken = AuthenticationEndpoint + "/refresh-token";
        }

        public static class User
        {
            public const string UsersEndpoint = ApiEndpoint + "/users";
            public const string UserEndpoint = UsersEndpoint + "/{id}";
            public const string WalletsOfUserEndpoint = UserEndpoint + "/wallets";
            public const string TransactionsOfUserEndpoint = UserEndpoint + "/transactions";
            public const string TransactionsOfUserEndpointByWalletId = UserEndpoint + "/transactions/{walletId}";
            public const string CreateShareWallet = UserEndpoint + "/share-wallet";
            public const string GetUserByPhoneNumber = UsersEndpoint + "/phone-number/{phoneNumber}";
            public const string CheckUserInfo = UsersEndpoint + "/check-info";
        }

        public static class Wallet
        {
            public const string WalletsEndpoint = ApiEndpoint + "/wallets";
            public const string WalletEndpoint = WalletsEndpoint + "/{id}";
            public const string InviteMemberEndpoint = WalletsEndpoint + "/invite-member";
            public const string GetUserInSharedWallet = WalletsEndpoint + "/{id}/users-in-sharewallet";
            public const string DeleteUserWallet = WalletEndpoint + "/{userId}";
            public const string GetTransactionByWalletId = WalletEndpoint + "/transactions";
            public const string ChangeOwner = WalletEndpoint + "/change-owner/{userId}";
            public const string GetWalletContributionStatistics = WalletEndpoint + "/contribution-statistics";
        }

        public static class Transaction
        {
            public const string TransactionsEndpoint = ApiEndpoint + "/transactions";
            public const string TransactionEndpoint = TransactionsEndpoint + "/{id}";
        }

        public static class VNPay
        {
            public const string VNPayEndpoint = ApiEndpoint + "/vnpay";
            public const string Payment = VNPayEndpoint + "/payment/{amount}/{infor}";
            public const string PaymentConfirm = VNPayEndpoint + "/paymentconfirm";
        }

        public static class PaymentMethod
        {
            public const string PaymentMethodsEndpoint = ApiEndpoint + "/payment-methods";
            public const string PaymentMethodEndpoint = PaymentMethodsEndpoint + "/{id}";
        }
    }
}
