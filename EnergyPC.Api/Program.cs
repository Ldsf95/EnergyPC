using System.Text;
using EnergyPC.Api.Services;
using EnergyPC.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Logs : Serilog (console + fichier journalier, 7 jours de rétention) ---
builder.Host.UseSerilog((ctx, cfg) => cfg
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7));

// --- Base de données SQLite via EF Core ---
builder.Services.AddDbContext<EnergyDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// --- Service de collecte en tâche de fond ---
builder.Services.AddHostedService<CollectorHostedService>();

// --- Authentification JWT (bonne pratique, même en local) ---
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "EnergyPC",
            ValidAudience = "EnergyPC.Desktop",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

// --- API + Swagger documenté ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EnergyPC API",
        Version = "v1",
        Description = "API locale de monitoring énergétique du PC (CPU/GPU/RAM/Batterie)."
    });

    // Intègre les commentaires XML aux descriptions Swagger.
    var xml = Path.Combine(AppContext.BaseDirectory,
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);

    // Bouton "Authorize" (Bearer) dans Swagger UI.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Saisir : Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Initialisation de la base : migration + seed au démarrage ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
}

// --- Middleware global d'exception : réponse JSON cohérente ---
app.UseExceptionHandler(b =>
{
    b.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = "Une erreur interne est survenue."
        });
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("EnergyPC API démarrée.");
app.Run();
