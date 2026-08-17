using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seatsure.BLL.Notifications;
using Seatsure.BLL.Security;
using Seatsure.BLL.Services;
using Seatsure.BLL.Services.Interfaces;

namespace Seatsure.BLL;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the business-logic layer: services, security, notifications, and JWT options.
    /// Call after the DAL's repository registrations, since services depend on repositories.
    /// </summary>
    public static IServiceCollection AddBll(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Replace with the SignalR-backed implementation in the API layer once the hub exists.
        services.AddSingleton<IAvailabilityNotifier, NullAvailabilityNotifier>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITicketTypeService, TicketTypeService>();
        services.AddScoped<IReservationService, ReservationService>();

        return services;
    }
}
