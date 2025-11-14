using LifeHub.Data;
using LifeHub.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.Extensions.Caching.Distributed;
using LifeHub.Models.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Stripe;
using LifeHub.Models.IA.Services;
using Microsoft.ML;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    
var stripeSecretKey = builder.Configuration["StripeSettings:SecretKey"];
if (string.IsNullOrEmpty(stripeSecretKey))
{
    throw new InvalidOperationException("Stripe SecretKey no está configurado en appsettings.json");
}

// Configurar Entity Framework con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ✅ CONFIGURAR REDIS
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
                           ?? "localhost:6379,abortConnect=false,connectTimeout=30000";
    options.InstanceName = "LifeHub_";
});

// ✅ CONFIGURACIÓN COMPLETA DE IDENTITY
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => 
{
    // Configuración de opciones de Identity
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // Configuración de usuario
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
    
    // Configuración de lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ✅ CONFIGURACIÓN DE AUTENTICACIÓN EXTERNA (GOOGLE) - VERSIÓN CORREGIDA
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] 
                      ?? throw new InvalidOperationException("Google ClientId no configurado");
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] 
                          ?? throw new InvalidOperationException("Google ClientSecret no configurado");
    options.CallbackPath = "/signin-google";
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    // Opciones adicionales para mejor compatibilidad
    options.SaveTokens = true;
});

// Configurar políticas de autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("UserOrAdmin", policy =>
        policy.RequireRole("User", "Admin"));
});

// Configurar cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// ✅ CONFIGURACIÓN STRIPE (CORREGIDO)
StripeConfiguration.ApiKey = stripeSecretKey;
builder.Services.AddScoped<IStripeService, StripeService>();

// Add controllers with views
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Registrar servicios personalizados (CORREGIDO - usar nombre completo para evitar conflicto)
builder.Services.AddScoped<ISubscriptionService, LifeHub.Services.SubscriptionService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<RoleInitializer>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IHabitService, HabitService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddHttpClient<IMedicationApiService, RxNormService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

builder.Services.AddSingleton<MLContext>();
builder.Services.AddScoped<IFinanceAIService, FinanceAIService>();
builder.Services.AddScoped<IHabitAIService, HabitAIService>();
builder.Services.AddScoped<IHabitMLService, HabitMLService>();
builder.Services.AddScoped<IHealthNotificationService, HealthNotificationService>();

// ✅ REGISTRO CORRECTO DE SERVICIOS FASE 2
builder.Services.AddHttpClient<IAppointmentChatbotService, AppointmentChatbotService>();
builder.Services.AddScoped<IPatternDetectionService, LifeHub.Models.Services.PatternDetectionService>();
builder.Services.AddScoped<IAppointmentChatbotService, AppointmentChatbotService>();
builder.Services.AddScoped<IMedicalReportService, MedicalReportService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddHttpClient<IChatbotService, ChatbotService>();
// Logging
builder.Services.AddLogging();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ✅ INICIALIZAR ROLES
using (var scope = app.Services.CreateScope())
{
    var roleInitializer = scope.ServiceProvider.GetRequiredService<RoleInitializer>();
    await roleInitializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ ORDEN CORRECTO: Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear Razor Pages (necesario para Identity)
app.MapRazorPages();

// ✅ ENDPOINT PARA PROBAR REDIS
app.MapGet("/test-redis", async (IDistributedCache cache) =>
{
    try 
    {
        await cache.SetStringAsync("test_key", "¡Redis funciona correctamente!", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });
        
        var value = await cache.GetStringAsync("test_key");
        return Results.Ok(new { 
            status = "✅ Redis conectado correctamente", 
            test_value = value,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"❌ Error de Redis: {ex.Message}");
    }
});

// ✅ ENDPOINT PARA PROBAR GOOGLE AUTH CONFIG
app.MapGet("/test-google-config", (IConfiguration config) =>
{
    var clientId = config["Authentication:Google:ClientId"];
    var clientSecret = config["Authentication:Google:ClientSecret"];
    
    return Results.Ok(new
    {
        GoogleClientId = string.IsNullOrEmpty(clientId) ? "❌ No configurado" : "✅ Configurado",
        GoogleClientSecret = string.IsNullOrEmpty(clientSecret) ? "❌ No configurado" : "✅ Configurado",
        HasClientId = !string.IsNullOrEmpty(clientId),
        HasClientSecret = !string.IsNullOrEmpty(clientSecret)
    });
});

app.Run();