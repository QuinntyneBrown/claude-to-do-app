using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Application;
using Tickbox.Application.Auth.Oidc;
using Tickbox.Application.Common;
using Tickbox.Infrastructure.Auth;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RefreshTokenOptions>(configuration.GetSection(RefreshTokenOptions.SectionName));
        services.Configure<OidcOptions>(configuration.GetSection(OidcOptions.SectionName));
        services.AddSingleton<IOidcConfiguration, OidcConfigurationAdapter>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<IEmailService, Email.LoggingEmailService>();

        var oidcSection = configuration.GetSection(OidcOptions.SectionName);
        if (oidcSection.GetValue<bool>("Enabled"))
        {
            services.AddHttpClient<IOidcClient, OidcClient>();
        }
        else
        {
            services.AddSingleton<IOidcClient, DisabledOidcClient>();
        }

        return services;
    }
}
