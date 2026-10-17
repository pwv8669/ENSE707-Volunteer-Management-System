using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;
using WebApp.Components;
using WebApp.Components.Account;
using WebApp.Data;
using WebApp.Services;
using WebApp.Services.Activity;
using WebApp.Services.Attendance;
using WebApp.Services.Identity;
using WebApp.Services.Management;
using WebApp.Services.Profile;
using WebApp.Services.Scheduling;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// The factory gives singleton scheduling services a fresh database context for
// each operation while still registering ApplicationDbContext for scoped pages.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // The prototype does not use an email delivery service, so newly
        // registered accounts can sign in without email confirmation.
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<UserRoleManagementService>();

// Reporting and Dashboard retain domain services as read models. The
// activity sync service rebuilds them from persisted Features 2 to 6 data.
builder.Services.AddSingleton<VolunteerOpportunityService>();
builder.Services.AddSingleton<VolunteerRequestService>();
builder.Services.AddSingleton<ReportingService>();

// Retained as a compatibility read model for older development data and
// activity-sync tests. WebApp application changes use the persisted service.
builder.Services.AddSingleton<VolunteerApplicationService>();

// Added for Feature 4 WebApp integration:
// Stores the domain availability used during volunteer assignment checks.
builder.Services.AddSingleton<VolunteerAvailabilityService>();

// Retained for domain tests; the WebApp assignment page uses the persisted
// service so assignments remain available after a restart.
builder.Services.AddSingleton<VolunteerAssignmentService>();

// Features 2 to 4 use this service to persist opportunities, applications
// and assignments in the configured Supabase database.
builder.Services.AddSingleton<VolunteerManagementPersistenceService>();

// Added for Feature 5 WebApp integration:
// Persists assigned shifts and notifications in the Supabase database so
// volunteer schedules survive application restarts.
builder.Services.AddSingleton<VolunteerSchedulePersistenceService>();

// Added for Feature 6 WebApp integration:
// Records attendance and completed hours against the persisted Feature 5
// shifts, then provides each volunteer's participation history.
builder.Services.AddSingleton<VolunteerAttendanceService>();

// Added for Features 3 and 4 WebApp integration:
// Converts the logged-in ASP.NET Identity account into the domain User model.
builder.Services.AddScoped<DomainUserService>();

// Added for Feature 4 WebApp integration:
// Synchronises availability from the existing Volunteer Profile database
// with the domain availability service before assignment.
builder.Services.AddScoped<AvailabilitySyncService>();

// Volunteer Profile Management reads the activity-sync read model rebuilt from
// persisted data; profile details and availability use ApplicationDbContext.
// TimeProvider lets tests control "now".
builder.Services.AddSingleton<VolunteerHistoryService>();

// Reporting and Dashboard and the profile history read "requests" that are rebuilt from the real
// volunteer applications and the saved attendance (see VolunteerActivitySyncService).
builder.Services.AddSingleton<VolunteerActivitySyncService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<VolunteerProfileService>();

var app = builder.Build();

// Ensure the supported roles exist, give pre-role accounts the default role,
// and promote the configured first administrator when the role is still empty.
await IdentityDataSeeder.SeedAsync(
    app.Services,
    app.Configuration["Identity:BootstrapAdministratorEmail"]);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    // Development data is now created through the persisted opportunity page.
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
