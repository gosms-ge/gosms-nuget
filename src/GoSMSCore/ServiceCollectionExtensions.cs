using System;
using Microsoft.Extensions.DependencyInjection;

namespace GoSMSCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGoSmsClient(
        this IServiceCollection services,
        Action<GoSmsClientOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        services.Configure(configure);

        services.AddHttpClient<IGoSmsClient, GoSmsClient>((sp, client) =>
        {
            var options = new GoSmsClientOptions();
            configure(options);
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
        });

        return services;
    }
}
