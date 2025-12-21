using FluentValidation;
using Inventory_Management.Application.Features.Commands.CategoriesCommand;

namespace Inventory_Management.Application.Validators.Categories
{
    public class CreateCategoriesCommandValidator : AbstractValidator<CreateCategoriesCommand>
    {
        public CreateCategoriesCommandValidator()
        {
            RuleFor(c => c.CategoryName)
                .NotEmpty().WithMessage("Kategori adı boş olamaz.")
                .MaximumLength(100).WithMessage("Kategori adı 100 karakterden uzun olamaz.");

            // Description is optional, no rule needed.
        }
    }
}
