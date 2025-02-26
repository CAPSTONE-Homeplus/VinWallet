using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Constants
{
    public class MessageConstant
    {
        public static class LoginMessage
        {
            public const string InvalidUsernameOrPassword = "Tên đăng nhập hoặc mật khẩu không chính xác";
            public const string InactivatedAccount = "Tài khoản đang bị vô hiệu hoá";
        }

        public static class UserMessage
        {
            public const string EmptyUserId = "User id bị trống";
            public const string UserNotFound = "Không tìm thấy người dùng";
            public const string UsernameAlreadyExists = "Username đã tồn tại";
            public const string UserInactivated = "Người dùng đã bị vô hiệu hoá";
            public const string UserActivated = "Người dùng đã được kích hoạt";
            public const string CreateUserFailed = "Tạo người dùng thất bại";
            public const string NotAllowAction = "Bạn không được truy cập phần thông tin này";
        }

        public static class WalletMessage
        {
            public const string EmptyWalletId = "Wallet id bị trống";
            public const string WalletNotFound = "Không tìm thấy ví";
            public const string CreateWalletFailed = "Tạo ví thất bại";
            public const string EmptyAmount = "Số tiền không được để trống";
            public const string MinAmount = "Số tiền nạp tối thiểu là 10,000 VND";
            public const string NotEnoughBalance = "Số dư không đủ";
            public const string InviteMemberFailed = "Mời thành viên vào ví thất bại";
        }

        public static class TransactionMessage
        {
            public const string EmptyTransactionId = "Transaction id bị trống";
            public const string TransactionNotFound = "Không tìm thấy giao dịch";
            public const string CreateTransactionFailed = "Tạo giao dịch thất bại";
        }


        public static class RoomMessage
        {
            public const string RoomNotFound = "Không tìm thấy phòng";
            public const string RoomCodeAlreadyExists = "Mã phòng đã tồn tại";
        }

        public static class BuildingMessage
        {
            public const string BuildingNotFound = "Không tìm thấy tòa nhà";
            public const string BuildingCodeAlreadyExists = "Mã tòa nhà đã tồn tại";
        }
        public static class HouseMessage
        {
            public const string HouseNotFound = "Không tìm thấy nhà";
            public const string HouseCodeAlreadyExists = "Mã nhà đã tồn tại";
            public const string HouseNotInBuilding = "Nhà không nằm trong tòa";
        }

        public static class Order
        {
            public const string EmptyOrderId = "OrderId bị trống";
            public const string OrderCodeExist = "Code của Order đã tồn tại";
            public const string OrderNotFound = "Không tìm thấy Order";
            public const string CreateFailedOrder = "Tạo mới Order thất bại";
        }

        public static class DataBase
        {
            public const string DatabaseError = "Fail to commit database";

        }
    }
}
