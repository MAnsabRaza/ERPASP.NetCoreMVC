using ERP.Models;
using Microsoft.EntityFrameworkCore;
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DbContext BEFORE builder.Build()
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -----------------------
// Add ToastNotification
builder.Services.AddNotyf(config =>
{
    config.DurationInSeconds = 3;          // How long toast will show
    config.IsDismissable = true;           // Show close button
    config.Position = NotyfPosition.TopRight; // Position of toast
});
// -----------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// -----------------------
// Use Notyf middleware
app.UseNotyf();
// -----------------------

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
