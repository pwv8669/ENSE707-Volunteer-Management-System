using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;
using WebApp.Components;
using WebApp.Components.Account;
using WebApp.Data;
using WebApp.Services;
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
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Reporting and Dashboard: volunteer opportunity/request domain services.
// These currently hold data in memory (see the domain library's service
// classes) rather than in the Postgres database, so opportunity and
// request data does not yet survive an app restart. Wiring them to
// ApplicationDbContext is tracked as a follow-up once opportunity/request
// management gets its own persisted entities.
builder.Services.AddSingleton<VolunteerOpportunityService>();
builder.Services.AddSingleton<VolunteerRequestService>();
builder.Services.AddSingleton<ReportingService>();

// Volunteer Profile Management: history is built from the in-memory request and
// opportunity services above; profile details and availability are saved in Postgres
// through ApplicationDbContext. TimeProvider lets tests control "now".
builder.Services.AddSingleton<VolunteerHistoryService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<VolunteerProfileService>();

var app = builder.Build();

// Ensure the supported roles exist and give pre-role accounts the default role.
await IdentityDataSeeder.SeedAsync(app.Services);

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
