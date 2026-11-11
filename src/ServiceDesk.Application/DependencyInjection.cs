using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Services;

namespace ServiceDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ServiceRequestService>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
