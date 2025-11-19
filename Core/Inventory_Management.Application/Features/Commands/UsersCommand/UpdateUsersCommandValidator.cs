using FluentValidation;
using Inventory_Management.Application.Features.Commands.UsersCommand;
using Inventory_Management.Persistance.Context;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.Command.UsersCommand
{
    public class UpdateUsersCommandValidator : AbstractValidator<UpdateUsersCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateUsersCommandValidator(Inventory_Management_Context context)
        {
            _context = context;

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("İsim gereklidir.")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ ]+$").WithMessage("İsim sadece harflerden oluşmalıdır.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyisim gereklidir.")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ ]+$").WithMessage("Soyisim sadece harflerden oluşmalıdır.");

            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir email giriniz.")
            .Must(BeUniqueEmail).WithMessage("Bu email adresi zaten sistemde kayıtlı.");


            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre gereklidir.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
                .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
                .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
                .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter (!? *.) içermelidir.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Telefon numarası gereklidir.")
                .Matches(@"^5\d{9}$").WithMessage("Telefon numarası 5 ile başlamalı ve 10 haneli olmalıdır. (Örn: 5551234567)");
        }

        private bool BeUniqueEmail(string email)
        {
            // Veritabanında bu email yoksa (Any false dönerse) validasyon geçerli (true) olur.
            // Not: Senkron çalışır, async versiyonu da vardır ama şimdilik bu yeterli.
            return !_context.Users.Any(u => u.Email == email);
        }
    }
}


