using Corleone;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

Family family = Family.createSingletonFamily("password");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "Corleone.Auth";
    options.Cookie.HttpOnly = true;       // JavaScript cannot read it.
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // API requests should get status 401, not an HTML redirect.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapGet("/signup", () => Results.Redirect("/signup.html"));
app.MapGet("/login", ()=> Results.Redirect("/login.html"));
app.MapPost("/signup", (UserInput user) =>
{
    Console.WriteLine($"Signup request received: username={user.username}, password={user.password}");
    if (family.RegisterUser(user.username, user.password)) {
        return Results.Ok();
    }
    return Results.BadRequest();
});
app.MapPost("/login", async (UserInput user, HttpContext context) => {
    if (family.AuthenticateMember(user.username, user.password)) {
        Claim[] claims = new[] { new Claim(ClaimTypes.Name, user.username)};
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true});
        Console.WriteLine("User logged in");
        return Results.Ok();
    }
    return Results.Unauthorized();
}
);
app.MapGet("/api/user", (ClaimsPrincipal user) =>
{
    Results.Ok(new { username = "hello"});
});
app.Run();

record UserInput(string username, string password);
