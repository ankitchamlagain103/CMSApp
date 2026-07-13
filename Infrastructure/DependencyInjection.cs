using System.Text;
using Application.Auth;
using Application.Common.Interfaces;
using Application.Dashboard;
using Application.Roles;
using Application.Users;
using Infrastructure.Common;
using Infrastructure.Email;
using Infrastructure.Files;
using Infrastructure.Identity;
using Infrastructure.Identity.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            RegisterDbContext(services, connectionString);
            RegisterIdentity(services);
            RegisterAuthentication(services, configuration);
            RegisterApplicationServices(services);

            return services;
        }

        private static void RegisterDbContext(IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        }

        private static void RegisterIdentity(IServiceCollection services)
        {
            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            // Applies to both email-confirmation and password-reset tokens, since both use the
            // default DataProtectorTokenProvider -- see CLAUDE.md for the trade-off this implies.
            services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(30));
        }

        private static void RegisterAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var signingKeyValue = configuration["Jwt:Key"];
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyValue));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = signingKey,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        }

        private static void RegisterApplicationServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<JwtTokenGenerator>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        }
    }
}
