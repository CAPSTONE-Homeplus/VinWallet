using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;
using VinWallet.Domain.Errors;
using VinWallet.Repository.Constants;

namespace VinWallet.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly bool _isDevelopment;

        public ExceptionHandlingMiddleware(RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            bool isDevelopment = false)
        {
            _next = next;
            _logger = logger;
            _isDevelopment = isDevelopment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = context.Response;
            var errorObj = new ErrorObj
            {
                TraceId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorObj.Code = (int)HttpStatusCode.BadRequest;
                    errorObj.Message = ExceptionConstant.BadRequest.Validation;
                    errorObj.Description = validationEx.Message;
                    _logger.LogWarning(validationEx, "Validation error occurred");
                    break;

                case BadHttpRequestException badRequestEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorObj.Code = (int)HttpStatusCode.BadRequest;
                    errorObj.Message = ExceptionConstant.BadRequest.Default;
                    errorObj.Description = badRequestEx.Message;
                    _logger.LogWarning(badRequestEx, "Bad request error occurred");
                    break;

                case UnauthorizedAccessException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorObj.Code = (int)HttpStatusCode.Unauthorized;
                    errorObj.Message = ExceptionConstant.Auth.Unauthorized;
                    errorObj.Description = unauthorizedEx.Message;
                    _logger.LogWarning(unauthorizedEx, "Unauthorized access attempt");
                    break;

                case AuthenticationException authEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorObj.Code = (int)HttpStatusCode.Unauthorized;
                    errorObj.Message = ExceptionConstant.Auth.InvalidCredentials;
                    errorObj.Description = authEx.Message;
                    _logger.LogWarning(authEx, "Authentication failed");
                    break;

                case KeyNotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorObj.Code = (int)HttpStatusCode.NotFound;
                    errorObj.Message = ExceptionConstant.NotFound.Default;
                    errorObj.Description = notFoundEx.Message;
                    _logger.LogWarning(notFoundEx, "Resource not found");
                    break;


                case DbUpdateException dbUpdateEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorObj.Code = (int)HttpStatusCode.BadRequest;
                    errorObj.Message = ExceptionConstant.Database.InsertFailed;
                    errorObj.Description = dbUpdateEx.InnerException?.Message ?? dbUpdateEx.Message;
                    _logger.LogError(dbUpdateEx, "Database update operation failed");
                    break;


                case OperationCanceledException canceledEx:
                    response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    errorObj.Code = (int)HttpStatusCode.ServiceUnavailable;
                    errorObj.Message = ExceptionConstant.Operation.Canceled;
                    errorObj.Description = canceledEx.Message;
                    _logger.LogWarning(canceledEx, "Operation was canceled");
                    break;

                case InvalidOperationException invalidOpEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorObj.Code = (int)HttpStatusCode.BadRequest;
                    errorObj.Message = ExceptionConstant.Operation.Invalid;
                    errorObj.Description = invalidOpEx.Message;
                    _logger.LogWarning(invalidOpEx, "Invalid operation attempted");
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorObj.Code = (int)HttpStatusCode.InternalServerError;
                    errorObj.Message = ExceptionConstant.Server.InternalError;
                    errorObj.Description = _isDevelopment ? exception.Message : "An unexpected error occurred";
                    _logger.LogError(exception, "An unhandled exception occurred");
                    break;
            }

            var result = JsonSerializer.Serialize(errorObj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await context.Response.WriteAsync(result);
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(
            this IApplicationBuilder app, bool isDevelopment = false)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>(isDevelopment);
        }
    }
}