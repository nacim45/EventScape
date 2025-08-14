using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Data;
using soft20181_starter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using soft20181_starter.Services;
using soft20181_starter.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Map Azure environment variables to configuration
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    // Map Azure environment variable names to configuration keys
    { "Stripe:PublicKey", Environment.GetEnvironmentVariable("StripePublic") ?? builder.Configuration["Stripe:PublicKey"] },
    { "Stripe:SecretKey", Environment.GetEnvironmentVariable("StripeSecret") ?? builder.Configuration["Stripe:SecretKey"] },
    { "Stripe:WebhookSecret", Environment.GetEnvironmentVariable("StripeWebhook") ?? builder.Configuration["Stripe:WebhookSecret"] },
    { "PayPal:ClientId", Environment.GetEnvironmentVariable("PaypalClientID") ?? builder.Configuration["PayPal:ClientId"] },
    { "PayPal:Secret", Environment.GetEnvironmentVariable("PaypalSecret") ?? builder.Configuration["PayPal:Secret"] }
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

var Configuration = builder.Configuration;
builder.Services.AddDbContext<EventAppDbContext>(options => 
    options.UseSqlite(Configuration.GetConnectionString("Default")));

// Register application services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSender>();

// Register audit services
builder.Services.AddScoped<SimpleAuditService>();
builder.Services.AddHttpContextAccessor();

// Add Identity services
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<EventAppDbContext>()
.AddDefaultTokenProviders();

// Configure Identity Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "EventScapeAuth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ContactAccess", policy => policy.RequireClaim(ClaimTypes.Role, "Contact"));
    options.AddPolicy("UserAccess", policy => policy.RequireClaim(ClaimTypes.Role, "User"));
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("Administrator")));
    options.AddPolicy("ContactManager", policy => policy.RequireClaim(ClaimTypes.Role, "Contact", "Admin", "Administrator"));
});

// Configure Razor Pages authorization
builder.Services.AddRazorPages(options =>
{
    // Make the Admin area accessible only to administrators
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    
    // Example of applying a more restrictive policy to an admin page
    // options.Conventions.AuthorizePage("/Admin/ManageContacts", "ContactManager");
});

builder.Services.AddHttpClient();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EventAppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var webHostEnvironment = services.GetRequiredService<IWebHostEnvironment>();
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        // Initialize roles if they don't exist
        string[] roleNames = { "Administrator", "Admin", "User", "Standard" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                var initLogger = services.GetRequiredService<ILogger<Program>>();
                initLogger.LogInformation("Created role: {Role}", roleName);
            }
        }
        
        // Check if default admin user exists
        var adminEmail = "admin@eventscape.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            // Create default admin user
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Name = "Admin",
                Surname = "User",
                EmailConfirmed = true,
                Role = "Administrator",
                RegisteredDate = DateTime.Now,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                ReceiveNotifications = true,
                ReceiveMarketingEmails = false,
                PreferredLanguage = "en",
                TimeZone = "UTC",
                SecurityStamp = Guid.NewGuid().ToString()
            };
            
            var createResult = await userManager.CreateAsync(adminUser, "Admin123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
                var adminLogger = services.GetRequiredService<ILogger<Program>>();
                adminLogger.LogInformation("Created default admin user: {Email}", adminEmail);
            }
            else
            {
                var errorLogger = services.GetRequiredService<ILogger<Program>>();
                errorLogger.LogError("Failed to create default admin user: {Errors}", 
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
        
        // Seed the database with events
        await DatabaseSeeder.SeedEvents(context, webHostEnvironment);
        
        var dbLogger = services.GetRequiredService<ILogger<Program>>();
        dbLogger.LogInformation("Database initialized successfully with persistent user data");
    }
    catch (Exception ex)
    {
        var dbLogger = services.GetRequiredService<ILogger<Program>>();
        dbLogger.LogError(ex, "An error occurred while initializing the database");
    }
}

// Log payment configuration status for debugging (after app is built)
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== Payment Configuration Status ===");
logger.LogInformation("Stripe Public Key: {Configured} (Length: {Length})", 
    !string.IsNullOrEmpty(app.Configuration["Stripe:PublicKey"]), 
    app.Configuration["Stripe:PublicKey"]?.Length ?? 0);
logger.LogInformation("Stripe Secret Key: {Configured} (Length: {Length})", 
    !string.IsNullOrEmpty(app.Configuration["Stripe:SecretKey"]), 
    app.Configuration["Stripe:SecretKey"]?.Length ?? 0);
logger.LogInformation("PayPal Client ID: {Configured} (Length: {Length})", 
    !string.IsNullOrEmpty(app.Configuration["PayPal:ClientId"]), 
    app.Configuration["PayPal:ClientId"]?.Length ?? 0);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Development-only features
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add custom error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();