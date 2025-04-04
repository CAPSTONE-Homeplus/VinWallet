﻿
using Grpc.Net.Client;
using Hangfire.SqlServer;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoomProto;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using VinWallet.API.Service.Implements;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.VnPay;
using VinWallet.Domain.Models;
using VinWallet.Repository.Generic.Implements;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.API.Service;
using VinWallet.API.Service.RabbitMQ;
using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using Microsoft.AspNetCore.SignalR;
using VinWallet.API.Hubs;
using HomeClean.API.Service.Implements.RabbitMQ;

namespace VinWallet.API.Extensions;

public static class DependencyServices
{
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork<VinWalletContext>, UnitOfWork<VinWalletContext>>();
        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddDbContext<VinWalletContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }


    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddTransient<IBackgroundJobClient, BackgroundJobClient>();
        services.AddSingleton<IUserIdProvider, UserProvider>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ISignalRHubService, SignalRHubService>();
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();


        services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQ"));
        services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
            return new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost
            };
        });

        services.AddSingleton<RabbitMQPublisher>();
        services.AddHostedService<RabbitMQConsumer>();

        services.AddHangfire(x => x.UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection")));
        services.AddHangfireServer();

        //services.AddHostedService<BackgroundJobService>();
        services.Configure<VNPaySettings>(configuration.GetSection("VNPaySettings"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<VNPaySettings>>().Value);
        services.AddScoped<IVNPayService, VNPayService>();

        services.AddSignalR();
        services.AddGrpcClient<RoomGrpcService.RoomGrpcServiceClient>(x =>
        {
            x.Address = new Uri("https://localhost:7106");
            //x.Address = new Uri("https://homeclean.onrender.com");
        });
        services.AddSingleton<IUserIdProvider, UserProvider>();

        return services;
    }

    public static IServiceCollection AddConfigSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo() { Title = "Vin Wallet", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
     {
         {
             new OpenApiSecurityScheme
             {
                 Reference = new OpenApiReference
                 {
                     Type = ReferenceType.SecurityScheme,
                     Id = "Bearer"
                 }
             },
             new string[] { }
         }
     });
            options.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
        });
        return services;
    }

    public static IServiceCollection AddJwtValidation(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidIssuer = "Issuer",
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("SuperStrongSecretKeyForJwtToken123!")
                ),
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role,
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/vinWalletHub"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new
                    {
                        code = StatusCodes.Status401Unauthorized,
                        message = "Invalid token"
                    });
                    return context.Response.WriteAsync(result);
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();

                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(new
                        {
                            code = StatusCodes.Status401Unauthorized,
                            message = "You are not authorized"
                        });
                        return context.Response.WriteAsync(result);
                    }

                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new
                    {
                        code = StatusCodes.Status403Forbidden,
                        message = "You do not have access to this resource"
                    });
                    return context.Response.WriteAsync(result);
                }
            };
        });

        return services;
    }
}
