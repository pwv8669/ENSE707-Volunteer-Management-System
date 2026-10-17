using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using WebApp.Data;

// This class contains tests that check which WebApp pages require a signed-in user, and which roles the Reporting page allows.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class PageAuthorizationTests
    {
        // The only routable pages that anyone (even someone who isn't signed in) may open.
        private static readonly string[] PublicPageNames = ["Home", "Error", "NotFound"];

        // Test that the Reporting and Dashboard page is limited to coordinators and organisation administrators.
        [TestMethod]
        public void ReportingPage_RequiresCoordinatorOrAdministratorRole()
        {
            Type reportingPage = typeof(AppRoles).Assembly.GetType("WebApp.Components.Pages.Reporting")!;

            AuthorizeAttribute? authorize = reportingPage.GetCustomAttribute<AuthorizeAttribute>();

            Assert.IsNotNull(authorize, "The Reporting page must have an [Authorize] attribute.");
            string[] allowedRoles = (authorize.Roles ?? string.Empty).Split(',', StringSplitOptions.TrimEntries);
            CollectionAssert.Contains(allowedRoles, AppRoles.Coordinator);
            CollectionAssert.Contains(allowedRoles, AppRoles.OrganisationAdministrator);
        }

        // Test that volunteers are not allowed to open the Reporting page, because it shows other volunteers' data.
        [TestMethod]
        public void ReportingPage_DoesNotAllowVolunteers()
        {
            Type reportingPage = typeof(AppRoles).Assembly.GetType("WebApp.Components.Pages.Reporting")!;

            AuthorizeAttribute authorize = reportingPage.GetCustomAttribute<AuthorizeAttribute>()!;

            string[] allowedRoles = (authorize.Roles ?? string.Empty).Split(',', StringSplitOptions.TrimEntries);
            CollectionAssert.DoesNotContain(allowedRoles, AppRoles.Volunteer);
        }

        // Test that every page with a web address needs a signed-in user, except the sign-in/register pages and the public pages,
        // so a new page can't be added by accident without an access rule.
        [TestMethod]
        public void EveryRoutablePage_ExceptAccountAndPublicPages_RequiresSignIn()
        {
            Assembly webApp = typeof(AppRoles).Assembly;

            List<string> pagesWithoutAccessRule = webApp.GetTypes()
                .Where(type => type.GetCustomAttributes<RouteAttribute>().Any())
                .Where(type => !(type.Namespace ?? string.Empty).StartsWith("WebApp.Components.Account"))
                .Where(type => !PublicPageNames.Contains(type.Name))
                .Where(type => type.GetCustomAttribute<AuthorizeAttribute>() is null)
                .Select(type => type.Name)
                .ToList();

            Assert.HasCount(
                0,
                pagesWithoutAccessRule,
                "These pages can be opened without signing in: " + string.Join(", ", pagesWithoutAccessRule));
        }
    }
}
