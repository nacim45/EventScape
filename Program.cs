using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Data;
using soft20181_starter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using soft20181_starter.Services;

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
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSender>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// Database initialization and seeding
await InitializeDatabase(app);

app.Run();

async Task InitializeDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var webHostEnvironment = services.GetRequiredService<IWebHostEnvironment>();

    try
    {
        var context = services.GetRequiredService<EventAppDbContext>();

        // Create database and tables
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Database created successfully");

        // Check if identity tables are properly created
        try
        {
            // Check if roles are properly created already
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            
            // Initialize roles
            string[] roleNames = { "Administrator", "ContactSubmitter", "User", "ContactManager" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    logger.LogInformation("Created role {Role}", roleName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking roles: {Message}", ex.Message);
            logger.LogInformation("Skipping role creation due to error");
        }

        // Check if there are any events
        try
        {
            var eventCount = await context.Events.CountAsync();
            logger.LogInformation("Current event count in database: {Count}", eventCount);

            if (eventCount == 0)
            {
                // If empty, seed the database
                await DatabaseSeeder.SeedEvents(context, webHostEnvironment);
                logger.LogInformation("Database seeded successfully");
            }
            else
            {
                logger.LogInformation("Database already contains events. Skipping seeding.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking events: {Message}", ex.Message);
        }

        // Create user accounts
        try
        {
            // Create a default administrator if none exists
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var adminEmail = "admin@eventscape.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true,
                    Role = "Administrator",
                    RegisteredDate = DateTime.Now
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                    logger.LogInformation("Created default administrator account and assigned role");
                }
                else
                {
                    logger.LogError("Failed to create default administrator account: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Default administrator account already exists");
                // Ensure admin role is assigned if user exists but role isn't assigned
                if (!await userManager.IsInRoleAsync(adminUser, "Administrator"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                    logger.LogInformation("Assigned Administrator role to existing default admin account");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating admin user: {Message}", ex.Message);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}