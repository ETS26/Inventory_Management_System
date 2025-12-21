using FluentValidation;
using Inventory_Management.Application.Features.Commands.ProductsCommand;
using System;

namespace Inventory_Management.Application.Validators.Products
{
    public class CreateProductsCommandValidator : AbstractValidator<CreateProductsCommand>
    {
        public CreateProductsCommandValidator()
        {
            RuleFor(p => p.ProductName)
                .NotEmpty().WithMessage("Ürün adı boş olamaz.")
                .MaximumLength(150).WithMessage("Ürün adı 150 karakterden uzun olamaz.");

            RuleFor(p => p.Barcode)
                .NotEmpty().WithMessage("Barkod boş olamaz.")
                .MaximumLength(50).WithMessage("Barkod 50 karakterden uzun olamaz.");

            RuleFor(p => p.CategoryId)
                .NotEqual(Guid.Empty).WithMessage("Kategori seçimi zorunludur.");

            RuleFor(p => p.UnitTypeId)
                .NotEqual(Guid.Empty).WithMessage("Birim tipi seçimi zorunludur.");

            // ImageURL and Description are optional.
        }
    }
}
