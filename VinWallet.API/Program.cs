using Hangfire;
using VinWallet.API.Extensions;
using VinWallet.API.Helper;
using VinWallet.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDatabase();
builder.Services.AddUnitOfWork();
builder.Services.AddServices(builder.Configuration);
builder.Services.AddJwtValidation();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpContextAccessor();
builder.Services.AddConfigSwagger();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()
               .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseRouting();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllAuthorizationFilter() }
});
app.UseWebSockets();

app.MapHub<VinWalletHub>("/vinWalletHub");

app.UseMiddleware<ExceptionHandlingMiddleware>();


app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
