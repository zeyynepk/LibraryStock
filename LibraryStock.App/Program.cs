using LibraryStock.App.Clean.Components;
using LibraryStock.App.Clean.Services;
using LibraryStock.App.Data;
using LibraryStock.App.Models;
using LibraryStock.App.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Server;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<CircuitOptions>(o => o.DetailedErrors = true);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(); // BaseAddress vermiyoruz; sayfa içinde Nav.ToAbsoluteUri kullanýlacak

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ls.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IStokService, StokService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOrdersService, OrdersService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/login", async (HttpContext http, AppDbContext db, LoginDto dto) =>
{
    var name = (dto.UserName ?? string.Empty).Trim();
    var pass = (dto.Password ?? string.Empty).Trim();

    var user = await db.Users.AsNoTracking()
        .FirstOrDefaultAsync(u => (u.UserName ?? string.Empty).Trim() == name);

    if (user is null) return Results.Unauthorized();
    if ((user.Password ?? string.Empty).Trim() != pass) return Results.Unauthorized();

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName ?? name),
        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        new Claim(ClaimTypes.Role, user.Role.ToString())
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.Now.AddHours(8),
            AllowRefresh = true
        });

    return Results.Ok(new { ok = true });
})
.DisableAntiforgery();

app.MapGet("/api/antiforgery", (IAntiforgery af, HttpContext ctx) =>
{
    var tokens = af.GetAndStoreTokens(ctx);
    return Results.Json(new { token = tokens.RequestToken });
});

app.MapPost("/api/users", async (AppDbContext db, NewUserDto input) =>
{
    var roleEnum = string.Equals(input.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                   ? Role.Admin : Role.Personel;

    var u = new User
    {
        UserName = input.UserName?.Trim(),
        Email = input.Email?.Trim(),
        Password = input.Password,
        Role = roleEnum,
        CreatedAdd = DateTime.Now,
        LastLoginDate = DateTime.Now
    };

    db.Users.Add(u);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{u.Id}", u);
});

app.MapPut("/api/users/{id:int}", async (int id, AppDbContext db, UpdateUserDto input) =>
{
    var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
    if (u == null) return Results.NotFound($"User {id} not found");

    var roleEnum = string.Equals(input.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                   ? Role.Admin : Role.Personel;

    u.UserName = input.UserName?.Trim() ?? u.UserName;
    u.Email = input.Email?.Trim() ?? u.Email;

    if (!string.IsNullOrWhiteSpace(input.Password))
        u.Password = input.Password;

    u.Role = roleEnum;

    await db.SaveChangesAsync();
    return Results.Ok(u);
});

app.MapDelete("/api/users/{id:int}", async (int id, AppDbContext db) =>
{
    var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
    if (u == null) return Results.NotFound($"User {id} not found");

    db.Users.Remove(u);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Home.razor'ýn kullandýðý sayým endpoint'i
app.MapGet("/api/users/count", async (AppDbContext db, string? role) =>
{
    IQueryable<User> q = db.Users.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(role))
    {
        var roleEnum = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                       ? Role.Admin : Role.Personel;
        q = q.Where(u => u.Role == roleEnum);
    }

    var count = await q.CountAsync();
    return Results.Ok(count);
});

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();

public record NewUserDto(string? UserName, string? Email, string? Password, string? Role);
public record UpdateUserDto(string? UserName, string? Email, string? Password, string? Role);
public record LoginDto(string? UserName, string? Password);
