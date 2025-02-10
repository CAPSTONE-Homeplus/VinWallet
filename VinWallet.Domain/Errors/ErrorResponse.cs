using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Domain.Errors
{
    public class ErrorResponse : Exception
    {

        public ErrorResponse()
        {
        }
        public ErrorResponse(ErrorObj internalServerError, ErrorObj notFound, ErrorObj badRequest)
        {
            InternalServerError = internalServerError;
            NotFound = notFound;
            BadRequest = badRequest;
        }

        public ErrorObj InternalServerError { get; set; } = new ErrorObj(code: (int)HttpStatusCode.InternalServerError, message: "Oops! Something went wrong.", description: "An internal server error occurred.");

        public ErrorObj NotFound { get; set; } = new ErrorObj(code: (int)HttpStatusCode.NotFound, message: "Resource not found.", description: "The requested resource could not be found.");

        public ErrorObj BadRequest { get; set; } = new ErrorObj(code: (int)HttpStatusCode.BadRequest, message: "Bad request.", description: "The request was malformed or invalid.");

        public ErrorObj Unauthorized { get; set; } = new ErrorObj(code: (int)HttpStatusCode.Unauthorized, message: "Unauthorized access.", description: "You are not authorized to access this resource.");

        public ErrorObj Error;

    }

}
