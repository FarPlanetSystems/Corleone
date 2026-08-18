using Corleone;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

Family family = Family.createSingletonFamily("password");

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var contentTypeProvider = new FileExtensionContentTypeProvider();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "Corleone.Auth";
    options.Cookie.HttpOnly = true;       // JavaScript cannot read it.
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
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


app.MapPost("/api/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).RequireAuthorization();


app.MapGet("/api/user", (ClaimsPrincipal user) =>

    Results.Ok(new { username = user.Identity!.Name })
).RequireAuthorization();


app.MapPost("/api/upload", async (IFormFileCollection files, ClaimsPrincipal user) => {
    Console.Write($"uploading to {user.Identity!.Name}");
    Console.WriteLine(files[0].ToString());
    Console.WriteLine(files.Count);
    await family.Storage.DownloadMemberFiles(user.Identity.Name, files);
}).DisableAntiforgery().RequireAuthorization();


app.MapGet("/api/files", (ClaimsPrincipal user) =>
{
    string memberStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "FileStorage", user.Identity?.Name);

    if (!Directory.Exists(memberStoragePath))
    {
        return Results.Ok(Array.Empty<object>());
    }

    var files = new DirectoryInfo(memberStoragePath)
        .EnumerateFiles()
        .Select(file =>
        {
            contentTypeProvider.TryGetContentType(
                file.Name,
                out string? contentType);

            bool isImage = IsSafeImageType(contentType);

            return new
            {
                name = file.Name,
                size = file.Length,
                isImage,
                url = $"/api/files/content/{Uri.EscapeDataString(file.Name)}"
            };
        });

    return Results.Ok(files);
})
.RequireAuthorization();

app.MapGet(
    "/api/files/content/{fileName}",
    (string fileName, ClaimsPrincipal user) =>
    {
        // Prevent paths such as ../../members.json.
        string safeFileName = Path.GetFileName(fileName);

        if (safeFileName != fileName)
        {
            return Results.BadRequest();
        }

        string memberStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "FileStorage", user.Identity?.Name);
        string filePath = Path.Combine(memberStoragePath, safeFileName);

        if (!File.Exists(filePath))
        {
            return Results.NotFound();
        }

        contentTypeProvider.TryGetContentType(
            filePath,
            out string? contentType);

        if (IsSafeImageType(contentType))
        {
            return Results.File(
                filePath,
                contentType: contentType,
                enableRangeProcessing: true);
        }

        // Other files are downloaded instead of executed in the browser.
        return Results.File(
            filePath,
            contentType: "application/octet-stream",
            fileDownloadName: safeFileName,
            enableRangeProcessing: true);
    })
.RequireAuthorization();

app.MapDelete("/api/files/content/{fileName}", (string fileName, ClaimsPrincipal member) => 
{
    string memberName = member.Identity.Name;
    if (memberName == null || !family.MemberExists(memberName)) {
        return Results.BadRequest(member);
    }

    string safeFileName = Path.GetFileName(fileName);

    if (safeFileName != fileName)
    {
        return Results.BadRequest();
    }

    if (!family.Storage.RemoveFromStorage(memberName, safeFileName))
    {
        return Results.InternalServerError();
    }

    return Results.Ok();

}).RequireAuthorization();

app.Run();

static bool IsSafeImageType(string? contentType)
{
    return contentType is
        "image/jpeg" or
        "image/png" or
        "image/gif" or
        "image/webp" or
        "image/bmp" or
        "image/avif";
}

record UserInput(string username, string password);
