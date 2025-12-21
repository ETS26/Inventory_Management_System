using FluentValidation;
using Inventory_Management.Application.Features.Commands.SuppliersCommand;

namespace Inventory_Management.Application.Validators.Suppliers
{
    public class CreateSuppliersCommandValidator : AbstractValidator<CreateSuppliersCommand>
    {
        public CreateSuppliersCommandValidator()
        {
            RuleFor(s => s.SupplierName)
                .NotEmpty().WithMessage("Tedarikçi adı boş olamaz.")
                .MaximumLength(150).WithMessage("Tedarikçi adı 150 karakterden uzun olamaz.");

            RuleFor(s => s.ContactPerson)
                .NotEmpty().WithMessage("Yetkili kişi adı boş olamaz.")
                .MaximumLength(100).WithMessage("Yetkili kişi adı 100 karakterden uzun olamaz.");

            RuleFor(s => s.Email)
                .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
                .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi girin.")
                .MaximumLength(100).WithMessage("E-posta 100 karakterden uzun olamaz.");

            RuleFor(s => s.PhoneNumber)
                .NotEmpty().WithMessage("Telefon numarası boş olamaz.")
                .Matches(@"^[0-9\s\(\)\-\+]{10,15}$").WithMessage("Geçersiz telefon numarası formatı.")
                .MaximumLength(15).WithMessage("Telefon numarası 15 karakterden uzun olamaz.");

            RuleFor(s => s.Address)
                .NotEmpty().WithMessage("Adres boş olamaz.")
                .MaximumLength(250).WithMessage("Adres 250 karakterden uzun olamaz.");
        }
    }
}
