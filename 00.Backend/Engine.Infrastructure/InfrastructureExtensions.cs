using Engine.Application.Admin;
using Engine.Application.Approval;
using Engine.Application.Attendance;
using Engine.Application.Auth;
using Engine.Application.Board;
using Engine.Application.Calendar;
using Engine.Application.Common.Interfaces;
using Engine.Application.Notification;
using Engine.Application.Organization;
using Engine.Application.Portal;
using Engine.Infrastructure.Cache;
using Engine.Infrastructure.Logging;
using Engine.Infrastructure.Persistence.Mongo;
using Engine.Infrastructure.Security;
using Engine.Infrastructure.Services;
using Engine.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Engine.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB
        var mongoConnStr = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
        var mongoDbName = configuration["Mongo:DatabaseName"] ?? "groupwaredb";
        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnStr));
        services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDbName));
        services.AddSingleton<GroupwareDbContext>();

        // Redis
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConn))
        {
            try
            {
                var redis = ConnectionMultiplexer.Connect(redisConn);
                services.AddSingleton<IConnectionMultiplexer>(redis);
                services.AddSingleton<ICacheService, RedisCacheService>();
            }
            catch
            {
                services.AddSingleton<ICacheService, InMemoryCacheService>();
            }
        }
        else
        {
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }

        // Security / Auth
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<IAuthService, AuthServiceImpl>();

        // Logging / Audit
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();

        // File Storage
        services.AddScoped<IFileStorageService>(sp =>
        {
            var basePath = configuration["FileStorage:BasePath"] ?? "uploads";
            return new LocalFileStorageService(basePath);
        });

        // Domain Services
        services.AddScoped<IOrganizationService, OrganizationServiceImpl>();
        services.AddScoped<IUserService, UserServiceImpl>();
        services.AddScoped<IApprovalFormService, ApprovalFormServiceImpl>();
        services.AddScoped<IApprovalDocumentService, ApprovalDocumentServiceImpl>();
        services.AddScoped<IBoardService, BoardServiceImpl>();
        services.AddScoped<IAttendanceService, AttendanceServiceImpl>();
        services.AddScoped<ICalendarService, CalendarServiceImpl>();
        services.AddScoped<IResourceReservationService, ResourceReservationServiceImpl>();
        services.AddScoped<INotificationService, NotificationServiceImpl>();
        services.AddScoped<IPortalService, PortalServiceImpl>();

        // Admin Services
        services.AddScoped<ISystemCodeService, SystemCodeServiceImpl>();
        services.AddScoped<IMenuService, MenuServiceImpl>();
        services.AddScoped<ISystemSettingService, SystemSettingServiceImpl>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryServiceImpl>();

        return services;
    }
}
