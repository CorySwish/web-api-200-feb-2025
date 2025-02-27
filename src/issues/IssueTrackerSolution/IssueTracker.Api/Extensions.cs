using System.IdentityModel.Tokens.Jwt;
using IssueTracker.Api.Catalog.Api;
using IssueTracker.Api.Employees.Api;

using IssueTracker.Api.Employees.Services;
using IssueTracker.Api.HelpDesk;
using IssueTracker.Api.Middleware;
using IssueTracker.Api.VipApiEndpoints;
using Marten;
using Marten.Events.Projections;


namespace IssueTracker.Api;

public static class Extensions
{
    public static IHostApplicationBuilder AddIssueTrackerServices(this IHostApplicationBuilder host)
    {
        var services = host.Services;

        // .net 8 and forward - good idea.
        services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
        services.AddScoped<ICheckForVipEmployees, EveryoneIsAVip>();
        var notificationApiUrl = host.Configuration.GetConnectionString("vips") ?? throw new ChaosException("Need a URL for the VIP API");
        services.AddHttpClient<VipNotifier>(client =>
        {
            client.BaseAddress = new Uri(notificationApiUrl);
        });

        services.AddScoped<INotifyTheVipApiOfAProblem>(sp =>
        {
            return sp.GetRequiredService<VipNotifier>();
        });
        services.AddScoped<IProvideTheEmployeeId, EmployeeIdProvider>();

        services.AddAuthorization();
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services.AddAuthentication().AddJwtBearer(opts =>
        {

            opts.MapInboundClaims = false;
        });

        // We'll use this later, for when our aggregates need to the context.
        services.AddHttpContextAccessor();

        var connectionString = host.Configuration.GetConnectionString("postgres") ?? throw new ChaosException("No connection string found");


        services.AddNpgsqlDataSource(connectionString);

        services.AddMarten(config =>
        {
            config.Connection(connectionString);
            config.Projections.Snapshot<EmployeeProblemReadModel>(SnapshotLifecycle.Inline);
            config.Projections.Snapshot<Employee>(SnapshotLifecycle.Inline);
            config.Projections.Snapshot<VipIssueReadModel>(SnapshotLifecycle.Inline);




        }).UseNpgsqlDataSource().UseLightweightSessions();


        return host;
    }

    public static IEndpointRouteBuilder MapIssueTracker(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCatalog();
        endpoints.MapEmployees();
        endpoints.MapVipApiEndpoints();
        endpoints.MapStaff();

        return endpoints;
    }


}