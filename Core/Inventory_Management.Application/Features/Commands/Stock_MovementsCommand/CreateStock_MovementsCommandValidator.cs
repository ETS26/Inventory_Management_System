using FluentValidation;

namespace Inventory_Management.Application.Features.Commands.Stock_MovementsCommand
{
    public class CreateStock_MovementsCommandValidator : AbstractValidator<CreateStock_MovementsCommand>
    {
        public CreateStock_MovementsCommandValidator()
        {
            RuleFor(x => x.MoveTypeId).NotEmpty().WithMessage("İşlem tipi seçilmelidir.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır.");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("Kullanıcı bilgisi eksik.");

            // --- SENARYO 1: MEVCUT STOK ---
            // Eğer "Yeni Kart" DEĞİLSE -> InventoryId zorunludur.
            RuleFor(x => x.InventoryId)
                .NotEmpty()
                .When(x => !x.IsNewInventory)
                .WithMessage("Mevcut stok işlemi için listeden bir kayıt seçmelisiniz.");

            // --- SENARYO 2: YENİ KART AÇMA ---
            // Eğer "Yeni Kart" İSE -> ProductId zorunludur. (InventoryId'ye bakılmaz, çünkü biz oluşturacağız)
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .When(x => x.IsNewInventory)
                .WithMessage("Yeni stok kartı açmak için bir Ürün seçmelisiniz.");
        }
    }
}