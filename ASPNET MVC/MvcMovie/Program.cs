using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcMovie.Data;
using MvcMovie.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure DbContext based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<MvcMovieContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("MvcMovieContext") ??
            throw new InvalidOperationException("Connection string 'MvcMovieContext' not found.")));
}
else
{
    builder.Services.AddDbContext<MvcMovieContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("MvcMovieContext") ??
            throw new InvalidOperationException("Connection string 'MvcMovieContext' not found.")));
}

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
