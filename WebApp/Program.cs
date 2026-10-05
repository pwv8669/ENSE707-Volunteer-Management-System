using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;
using WebApp.Components;
using WebApp.Components.Account;
using WebApp.Data;
using WebApp.Services;
using WebApp.Services.Identity;
using WebApp.Services.Profile;

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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
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

// Reporting and Dashboard: volunteer opportunity/request domain services.
// These currently hold data in memory (see the domain library's service
// classes) rather than in the Postgres database, so opportunity and
// request data does not yet survive an app restart. Wiring them to
// ApplicationDbContext is tracked as a follow-up once opportunity/request
// management gets its own persisted entities.
builder.Services.AddSingleton<VolunteerOpportunityService>();
builder.Services.AddSingleton<VolunteerRequestService>();
builder.Services.AddSingleton<ReportingService>();

// Added for Feature 3 WebApp integration:
// Provides volunteer application functionality such as submitting
// applications and viewing application status.
builder.Services.AddSingleton<VolunteerApplicationService>();

// Added for Feature 4 WebApp integration:
// Stores the domain availability used during volunteer assignment checks.
builder.Services.AddSingleton<VolunteerAvailabilityService>();

// Added for Feature 4 WebApp integration:
// Handles application review, assignment, capacity, availability and
// scheduling conflict checks.
builder.Services.AddSingleton<VolunteerAssignmentService>();

// Added for Features 3 and 4 WebApp integration:
// Converts the logged-in ASP.NET Identity account into the domain User model.
builder.Services.AddScoped<DomainUserService>();

// Added for Feature 4 WebApp integration:
// Synchronises availability from the existing Volunteer Profile database
// with the domain availability service before assignment.
builder.Services.AddScoped<AvailabilitySyncService>();

// Volunteer Profile Management: history is built from the in-memory request and
// opportunity services above; profile details and availability are saved in Postgres
// through ApplicationDbContext. TimeProvider lets tests control "now".
builder.Services.AddSingleton<VolunteerHistoryService>();
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

    // Demo opportunities so the profile history and reporting pages have events to show locally.
    DevelopmentSampleData.SeedOpportunities(app.Services.GetRequiredService<VolunteerOpportunityService>());
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
