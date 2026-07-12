using ChatarPatar.API.SignalR.Hubs;

namespace ChatarPatar.API.Configurations;

public static class SignalRConfiguration
{
    public static IServiceCollection AddSignalRConfiguration(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            // Keep connections alive — client sends a ping every 15s by default
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            // How long to wait before considering the client disconnected
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static WebApplication MapHubs(this WebApplication app)
    {
        app.MapHub<ChatHub>("/hubs/chat");
        return app;
    }
}
