using IssueTracker.Api.Employees.Services;
using IssueTracker.Api.Middleware;
using Marten;

namespace IssueTracker.Api.HelpDesk;


public static class Extensions
{

    public static IEndpointRouteBuilder MapStaff(this IEndpointRouteBuilder routes)
    {
        var staffGroup = routes.MapGroup("/help-desk-staff").RequireAuthorization(policy =>
        {
            policy.RequireRole("help-desk");
        });

        staffGroup.MapGet("/employees", async (IDocumentSession session) =>
        {
            var response = await session.Query<Employee>().ToListAsync();
            return TypedResults.Ok(response);
        });

        return staffGroup;

    }

}
