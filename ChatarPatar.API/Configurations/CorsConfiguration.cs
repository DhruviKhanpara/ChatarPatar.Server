namespace ChatarPatar.API.Configuration
{
    public static class CorsConfiguration
    {
        public static void AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: "AllowSpecificOrigin",
                    policy =>
                    {
                        var origins = configuration.GetSection("AppSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

                        policy.SetIsOriginAllowed(origin =>
                         {
                             if (string.IsNullOrWhiteSpace(origin))
                                 return false;

                             if (origins.Contains(origin,
                                 StringComparer.OrdinalIgnoreCase))
                                 return true;

                             // Lovable preview domains
                             //if (origin.EndsWith(".lovable.app",
                             //    StringComparison.OrdinalIgnoreCase))
                             //    return true;

                             // ngrok domains
                             //if (origin.Contains("ngrok-free.app",
                             //    StringComparison.OrdinalIgnoreCase) ||
                             //    origin.Contains("ngrok-free.dev",
                             //    StringComparison.OrdinalIgnoreCase))
                             //    return true;

                             return false;
                         })
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    });
            });
        }
    }
}
