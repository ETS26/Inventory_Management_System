using FluentValidation;
using FluentValidation.AspNetCore;
using Inventory_Management.Application.Features.Command.UsersCommand;
using Inventory_Management.Application.Features.Handlers.CategoriesHandler;
using Inventory_Management.Application.Features.Commands.UsersCommand; // Validator'ýn olduðu yer
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies; 
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný Baðlantýsý
builder.Services.AddDbContext<Inventory_Management_Context>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 2. MediatR Kurulumu (Application Katmanýný Bulmasý Ýçin)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(GetCategoriesQueryHandler).Assembly));

// 3. Validasyon Kurulumu (FluentValidation)
builder.Services.AddValidatorsFromAssemblyContaining<CreateUsersCommandValidator>();
builder.Services.AddFluentValidationAutoValidation();

// 4. Pipeline Behavior (Validasyonun devreye girmesi için)
// (ValidationBehavior sýnýfýnýn namespace'ini eklemeyi unutma)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// 5. Authentication (MVC Ýçin COOKIE Ayarý - JWT Yerine)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Giriþ yapmamýþ kiþiyi buraya at
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Oturum süresi
        options.Cookie.Name = "InventoryAppCookie";
    });

// 6. Session Servisi (EKSÝKTÝ - EKLENDÝ)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// MVC Controller ve View Servisleri
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- Pipeline Sýralamasý ---

app.UseStaticFiles(); // wwwroot dosyalarý için

app.UseRouting();

app.UseSession(); // Session'ý aktif et

app.UseAuthentication(); // Kimlik Doðrulama (Cookie okuma)
app.UseAuthorization();  // Yetkilendirme

// Rota Ayarý
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();