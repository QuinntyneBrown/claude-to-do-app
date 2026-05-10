using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tickbox.Application.Common;
using Tickbox.Domain;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class OidcEnabledFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"tickbox-oidc-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "InMemory",
                ["Oidc:Enabled"] = "true",
                ["Oidc:Authority"] = "https://idp.test/",
                ["Oidc:ClientId"] = "tickbox-test",
                ["Oidc:ClientSecret"] = "test-secret",
                ["Oidc:RedirectUri"] = "https://tickbox.test/callback",
                ["Oidc:Scopes"] = "openid email profile"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, TestEmailService>();

            services.RemoveAll<IOidcClient>();
            services.AddSingleton<IOidcClient, TestOidcClient>();
        });
    }

    public new System.Net.Http.HttpClient CreateClient()
    {
        EnsureSeeded();
        return base.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    private void EnsureSeeded()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Roles.Any())
        {
            db.Roles.Add(new Role { Id = KnownRoles.UserRoleId, Name = KnownRoles.User });
            db.SaveChanges();
        }
    }
}
