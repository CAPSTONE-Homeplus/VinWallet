using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Constants
{
    public static class ExceptionConstant
    {
        public static class BadRequest
        {
            public const string Default = "Bad request";
            public const string Validation = "Validation failed";
        }

        public static class Auth
        {
            public const string Unauthorized = "Unauthorized access";
            public const string InvalidCredentials = "Invalid credentials";
        }

        public static class NotFound
        {
            public const string Default = "Resource not found";
        }

        public static class Database
        {
            public const string Error = "Database error occurred";
            public const string SqlError = "A SQL error occurred during database operation.";
            public const string InsertFailed = "Failed to insert data into the database.";
        }

        public static class Operation
        {
            public const string Canceled = "Operation was canceled";
            public const string Invalid = "Invalid operation";
        }

        public static class Server
        {
            public const string InternalError = "Internal server error";
        }

    }
}
