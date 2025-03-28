using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Collections.Generic;
using PayMeChat_V1.Filters;

var builder = WebApplication.CreateBuilder(args);

// ✅ Cargar variables de entorno
builder.Configuration.AddEnvironmentVariables();

// ✅ Validar clave JWT
var jwtSecret = builder.Configuration["JWT_SECRET_KEY"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    Console.WriteLine("🔴 ERROR: La clave JWT no está configurada.");
    throw new InvalidOperationException("Clave JWT no configurada.");
}

// ✅ Validar conexión a la base de datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("🔴 ERROR: La cadena de conexión no está configurada.");
    throw new InvalidOperationException("Cadena de conexión no configurada.");
}

// ✅ Inyección de dependencias para Dapper y SqlConnection
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connection = new SqlConnection(connectionString);
    connection.Open();
    return connection;
});

// ✅ Inyección de IConfiguration explícitamente
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// ✅ Inyección de HttpClient para el controlador de WhatsApp
builder.Services.AddHttpClient();

// ✅ Agregar controladores
builder.Services.AddControllers();

// ✅ Configuración de CORS
var corsPolicy = "AllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:3001"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

// ✅ Configuración de autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// ✅ Agregar autorización
builder.Services.AddAuthorization();

// ✅ Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PayMeChat API",
        Version = "v1",
        Description = "Documentación de la API de PayMeChat"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en el formato: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });

    c.OperationFilter<FileUploadOperationFilter>();
    c.EnableAnnotations();
});

// ✅ Crear carpeta 'uploads' si no existe
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// ✅ Construcción de la app
var app = builder.Build();

// ✅ Habilitar middleware y Swagger
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PayMeChat API v1");
        c.RoutePrefix = "swagger";
    });
}

// ✅ Middlewares importantes
app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ✅ Establecer URL explícita si se desea (opcional si ya está en appsettings.json)
app.Run();
