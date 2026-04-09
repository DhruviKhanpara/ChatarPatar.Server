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

                        policy.WithOrigins(origins!)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    });
            });
        }
    }
}
