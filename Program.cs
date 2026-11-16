using System.Security.Claims;
using System.Text;
using LogiTrack.Data;
using LogiTrack.Models;
using LogiTrack.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// BANCO DE DADOS
// ======================================================

builder.Services.AddDbContext<LogiTrackContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' was not found."
        );

    options.UseSqlite(connectionString);
});

// ======================================================
// ASP.NET IDENTITY
// ======================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Regras de senha
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Email único por usuário
        options.User.RequireUniqueEmail = true;

        // Bloqueio após tentativas inválidas
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LogiTrackContext>()
    .AddDefaultTokenProviders();

// ======================================================
// JWT
// ======================================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT key was not configured."
    );

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer was not configured."
    );

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience was not configured."
    );

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                RoleClaimType = ClaimTypes.Role,

                NameClaimType = ClaimTypes.Name
            };
    });

// ======================================================
// AUTORIZAÇÃO
// ======================================================

builder.Services.AddAuthorization();

// ======================================================
// NOSSO SERVIÇO JWT
// ======================================================

builder.Services.AddScoped<JwtTokenService>();

// ======================================================
// CONTROLLERS
// ======================================================

builder.Services.AddControllers();

// ======================================================    
// MEMÓRIA CACHE
// ======================================================

builder.Services.AddMemoryCache();

// ======================================================
// SWAGGER
// ======================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Enter your JWT token."
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },

                Array.Empty<string>()
            }
        }
    );
});

var app = builder.Build();

// ======================================================
// SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ======================================================
// CONFIGURA BANCO + ROLES + DADOS INICIAIS
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context =
        services.GetRequiredService<LogiTrackContext>();

    // Aplica migrations pendentes.
    await context.Database.MigrateAsync();

    // --------------------------------------------------
    // ROLES
    // --------------------------------------------------

    var roleManager =
        services.GetRequiredService<
            RoleManager<IdentityRole>
        >();

    string[] roleNames =
    {
        "User",
        "Manager"
    };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new IdentityRole(roleName);

            await roleManager.CreateAsync(role);
        }
    }

/*   **PROMOCAO DE USUARIO PARA MANAGER * 
    var userManager =
    services.GetRequiredService<UserManager<ApplicationUser>>();

    var managerEmail = "manager@logitrack.local";

    var managerUser =
        await userManager.FindByEmailAsync(managerEmail);

    if (managerUser is not null &&
        !await userManager.IsInRoleAsync(managerUser, "Manager"))
    {
        await userManager.AddToRoleAsync(
            managerUser,
            "Manager"
        );

        Console.WriteLine(
            $"User {managerEmail} promoted to Manager."
        );
    }
*/


    // --------------------------------------------------
    // INVENTÁRIO INICIAL
    // --------------------------------------------------

    if (!await context.InventoryItems.AnyAsync())
    {
        var palletJack = new InventoryItem
        {
            Name = "Pallet Jack",
            Quantity = 12,
            Location = "Warehouse A"
        };

        var forklift = new InventoryItem
        {
            Name = "Forklift",
            Quantity = 5,
            Location = "Warehouse B"
        };

        context.InventoryItems.AddRange(
            palletJack,
            forklift
        );

        await context.SaveChangesAsync();
    }

    // Apenas para confirmar os dados no console.
    var items = await context.InventoryItems
        .AsNoTracking()
        .ToListAsync();

    foreach (var item in items)
    {
        item.DisplayInfo();
    }
}

// ======================================================
// MIDDLEWARE DE SEGURANÇA
// ======================================================

app.UseAuthentication();

app.UseAuthorization();

// ======================================================
// ENDPOINT SIMPLES PARA VERIFICAR A APLICAÇÃO
// ======================================================

app.MapGet("/", () => Results.Ok(new
{
    application = "LogiTrack",
    status = "Running"
}));

// ======================================================
// CONTROLLERS
// ======================================================

app.MapControllers();

// ======================================================
// INICIA A APLICAÇÃO
// ======================================================

app.Run();
