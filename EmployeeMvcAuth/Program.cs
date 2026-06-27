using EmployeeMvcAuth.Data;
using EmployeeMvcAuth.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    //options.IdleTimeout = TimeSpan.FromHours(8);
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddScoped<AuthRepository>();
builder.Services.AddSingleton<IPasswordHasher<EmployeePasswordUser>, PasswordHasher<EmployeePasswordUser>>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

//app.UseAuthorization();
//app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var repo = scope.ServiceProvider.GetRequiredService<AuthRepository>();

    await repo.CreateEmployeeIfNotExistsAsync(
        userId: "E1001",
        password: "Test@123",
        fullName: "Demo Employee",
        email: "demo@example.com"
    );
}

app.Run();
