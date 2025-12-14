using FluentValidation;
using FluentValidation.AspNetCore;
using Inventory_Management.Application.Features.Command.UsersCommand;
using Inventory_Management.Application.Features.Handlers.CategoriesHandler;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- CORS ---
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

    // --- DB ---
    builder.Services.AddDbContext<Inventory_Management_Context>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<Inventory_Management.Domain.Common.ICurrentUserService, Inventory_Management.WebApi.Services.CurrentUserService>();

    // --- MediatR ---
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssemblies(typeof(GetCategoriesQueryHandler).Assembly));

    // --- FluentValidation ---
    builder.Services.AddValidatorsFromAssemblyContaining<CreateUsersCommandValidator>();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // --- JSON ---
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

    builder.Services.AddEndpointsApiExplorer();

    // --- JWT ---
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
        };
    });

    

    builder.Services.AddAuthorization();

    // --- Swagger ---
    builder.Services.AddSwaggerGen(x =>
    {
        x.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory_Management API", Version = "v1" });

        x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header"
        });

        x.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                new string[] {}
            }
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory_Management API v1"));
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowAll");

    DefaultFilesOptions options = new DefaultFilesOptions();
    options.DefaultFileNames.Clear();
    options.DefaultFileNames.Add("login.html");
    app.UseDefaultFiles(options);

    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine(">>> UYGULAMA ÇÖKTÜ <<<");
    Console.WriteLine(ex);
    throw;
}
