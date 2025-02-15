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
        }

        public static class RoomMessage
        {
            public const string RoomNotFound = "Không tìm thấy phòng";
            public const string RoomCodeAlreadyExists = "Mã phòng đã tồn tại";
        }

        public static class DataBase
        {
            public const string DatabaseError = "Fail to commit database";

        }
    }
}
