using System.Security.Claims;
using HobbyXP.Data;
using HobbyXP.Services;
using HobbyXP.Web.Auth;
using HobbyXP.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HobbyXpAuthOptions>(
    builder.Configuration.GetSection(HobbyXpAuthOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(connectionString)
    && !connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHobbyXpPostgres(connectionString);
}
else if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddHobbyXpData(options =>
        options.UseSqlite(connectionString));
}
else
{
    // Dev local sin Postgres: SQLite bajo LocalAppData (o HOBBYXP_DATA_DIR).
    builder.Services.AddHobbyXpSqlite();
}

builder.Services.AddHobbyXpServices();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "HobbyXP.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

await app.Services.EnsureHobbyXpDatabaseAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/account/login", async (
    HttpContext http,
    IConfiguration config) =>
{
    var form = await http.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var expectedUser = config["HobbyXp:Auth:Username"] ?? "admin";
    var expectedPass = config["HobbyXp:Auth:Password"] ?? "changeme";

    if (!string.Equals(username, expectedUser, StringComparison.Ordinal)
        || !string.Equals(password, expectedPass, StringComparison.Ordinal))
    {
        return Results.Redirect("/login?error=1");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, username),
        new(ClaimTypes.Role, "Owner")
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/'))
        returnUrl = "/";

    return Results.Redirect(returnUrl);
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/account/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
