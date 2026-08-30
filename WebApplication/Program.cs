using Volunteer_Management_System;
using WebApplication;
using WebApplication.Components;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<VolunteerOpportunityService>();
builder.Services.AddSingleton<VolunteerRequestService>();
builder.Services.AddSingleton<ReportingService>();
builder.Services.AddSingleton<VolunteerNameDirectory>();

var app = builder.Build();

Dictionary<Guid, string> volunteerNames = SampleDataSeeder.Seed(
    app.Services.GetRequiredService<VolunteerOpportunityService>(),
    app.Services.GetRequiredService<VolunteerRequestService>());
app.Services.GetRequiredService<VolunteerNameDirectory>().Load(volunteerNames);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
