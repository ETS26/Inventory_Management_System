// Gerekli using bildirimleri
using FluentValidation;
using FluentValidation.AspNetCore;
using Inventory_Management.Application.Features.Command.UsersCommand;
using Inventory_Management.Application.Features.Handlers.CategoriesHandler; // MediatR'�n Handler'lar� bulmas� i�in
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// --- 1. Servislerin Eklenmesi (Dependency Injection) ---

// Veritaban� (SQL Server) ba�lant�s�n� ekle
builder.Services.AddDbContext<Inventory_Management_Context>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// MediatR'� ekle (Application katman�n� bularak)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(GetCategoriesQueryHandler).Assembly));



builder.Services.AddValidatorsFromAssemblyContaining<CreateUsersCommandValidator>();
builder.Services.AddFluentValidationAutoValidation();

// 2. MediatR Pipeline'�na ValidationBehavior'� ekle
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Controller'lar� ekle
builder.Services.AddControllers();

// API Explorer (Swagger i�in gerekli)
builder.Services.AddEndpointsApiExplorer();

// --- 2. JWT (Authentication) Servislerini Eklenmesi ---
// appsettings.json dosyan�zdaki "Jwt" b�l�m�n� okur
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
    };
});

// Authorization (Yetkilendirme - [Authorize] etiketleri i�in) servisini ekle
builder.Services.AddAuthorization();


// --- 3. SwaggerGen'in JWT ("Authorize" Butonu) ile Yap�land�r�lmas� ---
builder.Services.AddSwaggerGen(x =>
{
    x.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory_Management API", Version = "v1" });

    // JWT i�in "Authorize" butonu tan�m�
    x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\""
    });

    // Bu tan�m� t�m endpoint'lere uygula
    x.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// --- HTTP Request Pipeline'in Yap�land�r�lmas� ---
var app = builder.Build();

// Geli�tirme ortam�ndaysa Swagger'� a�
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory_Management API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// ---- G�venlik S�ralamas� (�nemli) ----
// (React olmad���ndan CORS'a gerek yok)

// 1. Authentication (Kimlik Do�rulama): Gelen iste�in token'�n� oku
app.UseAuthentication();

// 2. Authorization (Yetkilendirme): Token'a g�re izinleri kontrol et
app.UseAuthorization();

// ---- ------------------------------ ----

// Controller'lar� e�le
app.MapControllers();

app.Run();