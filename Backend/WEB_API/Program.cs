using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Api.Jobs;
using API.Middlewares;
using Application.Abstractions;
using Domain;
using Domain.Entities.Identity;
using Domain.Models;
using Infrastructure.Data;
using System.Globalization;
using System.Text;
using Identity.Infrastructure;
using Microsoft.Extensions.Configuration;
using WEB_API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.AddScheduler();

builder.Services.AddScoped<InvoiceJob>();


builder.Services.AddControllers(config =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    config.Filters.Add(new AuthorizeFilter(policy));
})
                .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API SPA", Version = "v1", Description = "Desarrollado por 4DevsCorp" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        new List<string>()
                    }
                });

});
builder.Services.Configure<TokenManagement>(builder.Configuration.GetSection("TokenManagement"));

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));

builder.Services.AddDomain();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("es-PE");
});

builder.Services.Configure<ApiUrl>(opt => builder.Configuration.GetSection("ApiUrl").Bind(opt));

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");


var secretKey = Encoding.ASCII.GetBytes(builder.Configuration.GetSection("TokenManagement:SecretKey").Value);
var decriptKey = Encoding.UTF8.GetBytes(builder.Configuration.GetSection("TokenManagement:EncryptionSecret").Value);

var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
    //TokenDecryptionKey = new SymmetricSecurityKey(decriptKey),
    ValidateIssuer = false,
    ValidateAudience = false,
    RequireExpirationTime = false,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,
};

builder.Services.AddSingleton(tokenValidationParameters);

builder.Services.AddHealthChecks().AddDbContextCheck<SpaContext>();


builder.Services.AddDbContext<SpaContext>(o =>
{
    o.UseNpgsql(options => options.MigrationsAssembly(typeof(SpaContext).Assembly.FullName));
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
});



builder.Services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<SpaContext>()
                .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = tokenValidationParameters;
});


builder.Services.AddSingleton<ITenantRegistry, TenantRegistry>();
builder.Services.AddScoped<ITenantResolver, TenantResolver>();
builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .SetIsOriginAllowed(origin =>
            {
                return origin.EndsWith(".4devscorp.com") ||
                       origin.EndsWith(".amplifyapp.com") ||
                       origin.EndsWith("5173") ||
                       origin.EndsWith("5174") ||
                       origin.EndsWith(".lobytech.com") ||
                       origin.EndsWith(".lobytech.com:2013") ||
                       origin.EndsWith("3000") ||
                       origin.EndsWith("3001") ||
                       origin.EndsWith("3002");
            })
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


//migration



builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

//DatabaseHelper.EnsureLatestDatabase(builder.Services);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5002); // Cambia el puerto si es necesario
});

var app = builder.Build();


var supportedCultures = new[] { new CultureInfo("es-PE") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es-PE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

var scheduler = app.Services.GetRequiredService<IScheduler>();

scheduler.OnWorker("InvoiceJob");
//scheduler.Schedule<InvoiceJob>().EveryFiveMinutes().PreventOverlapping("InvoiceJob").RunOnceAtStart();

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", $"API SPA {app.Environment.EnvironmentName}");
    c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
    c.RoutePrefix = string.Empty;
});

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseMiddleware<TenantContextMiddleware>();

app.UseHandlerUsers();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health");
});

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetService<ILoggerFactory>();

    try
    {
        var context = services.GetRequiredService<SpaContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();

        await context.Database.MigrateAsync();
        await PuntoVentaDbContextData.LoadDataAsync(context,loggerFactory);

        await SeedSuperAdminRole(userManager, roleManager);
    }
    catch (Exception ex)

    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "Error en la migraci�n");
    }
}

// Solo el usuario ADMIN puede crear tenants/empresas nuevas (ver TenantController).
static async Task SeedSuperAdminRole(UserManager<User> userManager, RoleManager<Role> roleManager)
{
    if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        await roleManager.CreateAsync(new Role { Id = Guid.NewGuid().ToString(), Name = "SuperAdmin" });

    var admin = await userManager.FindByNameAsync("ADMIN");

    if (admin != null && !await userManager.IsInRoleAsync(admin, "SuperAdmin"))
        await userManager.AddToRoleAsync(admin, "SuperAdmin");
}

    app.Run();

//FINAL TEST CI/CD 2