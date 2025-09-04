using HashiCorpIntegration.Data;
using HashiCorpIntegration.Vault;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Vault settings
builder.Services.Configure<VaultSettings>(builder.Configuration.GetSection("Vault"));

// Add services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<IConnectionStringProvider, VaultConnectionStringProvider>();

// Configure DbContext with connection string provider
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    // Use fallback connection string for initial setup
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var fallbackConnectionString = configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(fallbackConnectionString);
});

// Then use a background service to update connection string if needed

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

