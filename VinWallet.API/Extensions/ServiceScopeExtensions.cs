namespace VinWallet.API.Extensions
{
    public static class ServiceScopeExtensions
    {
        public static async Task ExecuteScopedAsync<T>(this IServiceScopeFactory scopeFactory, Func<T, Task> action) where T : notnull
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            await action(service);
        }
    }
}
