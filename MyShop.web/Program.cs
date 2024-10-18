using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using MyShop.web.Models;
using MyShop_DataAccess.Data;
using MyShop_DataAccess.DbInitializer;
using MyShop_DataAccess.Immplementation;

using MyShop_Entities.Helper;
using MyShop_Entities.Models;
using MyShop_Entities.Repositories;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Force HTTPS redirection
//builder.Services.AddHttpsRedirection(options =>
//{
//    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
//    options.HttpsPort = 443; // Standard HTTPS port
//});

// Add services to the container.
builder.Services.AddControllersWithViews();
//Runtime Compilation 
 builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

builder.Services.AddDbContext<ApplicationDbContext>(options
    => options.UseSqlServer(builder.Configuration.GetConnectionString("DefultConnection")));

// inject StripeSection from appsittings
// builder.Services.Configure<StripeSection>(builder.Configuration.GetSection("stripe"));

// to make admin lock or un lock any user  
// 1-  lockoutOnFailure: true => in login.cs make this true 
// 2- and here 
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(60);
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;

            })
 .AddEntityFrameworkStores<ApplicationDbContext>()
 .AddDefaultUI()
 .AddDefaultTokenProviders();


// builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender,EmailSender>();


builder.Services.AddScoped<IUnitOfWork , UnitOfWork>();
//builder.Services.AddScoped<IDbIntalizer, DbIntalizer>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 5;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 0;
});


// to set session
builder.Services.AddMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// Get the environment (Development, Production, etc.)
var env = app.Environment;

// Call Seed method only in certain environments
if (env.IsDevelopment() || env.IsProduction())
{
    SeedDatabase(app);
}

// Configure the HTTP request pipeline.
if (!env.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// inject StripeSection from appsittings
// StripeConfiguration.ApiKey = builder.Configuration.GetSection("stripe:Secretkey").Get<string>();

//SeedDb();

//app.UseHttpsRedirection(); // Enforce HTTPS

app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Use custom error handling middleware
app.UseStatusCodePagesWithReExecute("/Error/NotFound");

app.UseRouting();
// to run identity pages 
app.MapRazorPages();



app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{area=Admin}/{controller=Home}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
     name: "areas",
     pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}"
   ); 
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
   );
    endpoints.MapRazorPages();
});


app.Run();

void SeedDatabase(IApplicationBuilder app)
{
    using (var serviceScope = app.ApplicationServices.CreateScope())
    {
        var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure roles are created
        if (!roleManager.RoleExistsAsync(Helpers.AdminRole).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole(Helpers.AdminRole)).GetAwaiter().GetResult();
        }

        // Create admin user if it doesn't exist
        var adminUser = userManager.FindByEmailAsync("admin@myshop.com").GetAwaiter().GetResult();
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@myshop.com",
                Email = "admin@myshop.com",
                EmailConfirmed = true , // Skip email confirmation
                PhoneNumber = "21099106179",
                Name = "Admin",
                City = "??????",
                Address ="????? ???? ????? ???????"
                
            };
            userManager.CreateAsync(adminUser, "admin123").GetAwaiter().GetResult();
            userManager.AddToRoleAsync(adminUser, Helpers.AdminRole).GetAwaiter().GetResult();
        }
    }
}
    
//void SeedDb()
//{
//    using(var scope = app.Services.CreateScope())
//    {
//        var DbIntializer = scope.ServiceProvider.GetRequiredService<IDbIntalizer>();
//        DbIntializer.Initalize();
//    }
//}