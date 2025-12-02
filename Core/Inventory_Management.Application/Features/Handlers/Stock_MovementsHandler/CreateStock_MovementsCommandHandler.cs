using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Stock_MovementsHandler
{
    public class CreateStock_MovementsCommandHandler : IRequestHandler<CreateStock_MovementsCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateStock_MovementsCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateStock_MovementsCommand request, CancellationToken cancellationToken)
        {
            Inventories targetInventory = null;

            // 0. KULLANICIYI VE ÞÝRKETÝNÝ BUL
            // Envanter ve Hareket kayýtlarý kullanýcýnýn þirketine ait olmalýdýr.
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) throw new Exception("Ýþlemi yapan kullanýcý sistemde bulunamadý.");

            // ---------------------------------------------------------
            // 1. ENVANTER BELÝRLEME (YENÝ MÝ / MEVCUT MU?)
            // ---------------------------------------------------------

            if (request.IsNewInventory)
            {
                // --- SENARYO A: YENÝ KART AÇMA ---
                if (request.ProductId == null || request.ProductId == Guid.Empty)
                    throw new Exception("Yeni kart açmak için bir Ürün seçmelisiniz.");

                // Yeni Envanter Nesnesi (Henüz veritabanýnda yok)
                targetInventory = new Inventories
                {
                    Id = Guid.NewGuid(),
                    ProductId = request.ProductId.Value,
                    CompanyId = user.CompanyId, // Kullanýcýnýn þirketine kaydet
                    Quantity = 0, // Miktarý aþaðýda hareket ile güncelleyeceðiz

                    // Command'den gelen verileri ata
                    PurchasePrice = request.PurchasePrice,
                    SalePrice = request.SalePrice,
                    CriticalStockQuantity = request.CriticalStockQuantity,
                    BatchNumber = !string.IsNullOrEmpty(request.BatchNumber) ? request.BatchNumber : "AUTO-" + DateTime.Now.ToString("yyMMdd"),
                    ExpirationDate = request.ExpirationDate,

                    Description = "Stok Hareketi ile oluþturuldu.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Context'e ekle (Transaction sonunda kaydedilecek)
                await _context.Inventories.AddAsync(targetInventory, cancellationToken);
            }
            else
            {
                // --- SENARYO B: MEVCUT ENVANTER ---
                if (request.InventoryId == null || request.InventoryId == Guid.Empty)
                    throw new Exception("Lütfen listeye eklemek için bir stok kartý seçiniz.");

                targetInventory = await _context.Inventories
                    .FirstOrDefaultAsync(x => x.Id == request.InventoryId, cancellationToken);

                if (targetInventory == null)
                    throw new Exception("Seçilen envanter kaydý bulunamadý.");
            }

            // ---------------------------------------------------------
            // 2. HAREKET TÝPÝ VE STOK MÝKTARI GÜNCELLEME
            // ---------------------------------------------------------

            var moveType = await _context.Move_Types
                .FirstOrDefaultAsync(x => x.Id == request.MoveTypeId, cancellationToken);

            if (moveType == null)
                throw new Exception("Geçersiz hareket tipi!");

            // Büyük/Küçük harf duyarsýz kontrol
            string typeName = moveType.MoveType.ToLower();
            bool isIncome = typeName.Contains("stock in") || typeName.Contains("giriþ") || typeName.Contains("in");
            bool isOutcome = typeName.Contains("stock out") || typeName.Contains("çýkýþ") || typeName.Contains("out");

            if (isIncome)
            {
                targetInventory.Quantity += request.Quantity;
            }
            else if (isOutcome)
            {
                // Yeni kart açýlýyorsa stok zaten 0'dýr, eksiye düþemez.
                if (targetInventory.Quantity < request.Quantity)
                {
                    throw new Exception($"Yetersiz Stok! Mevcut: {targetInventory.Quantity}, Çýkýþ Ýstenen: {request.Quantity}");
                }
                targetInventory.Quantity -= request.Quantity;
            }

            // ---------------------------------------------------------
            // 3. FÝYAT HESAPLAMA VE HAREKET KAYDI
            // ---------------------------------------------------------

            // O anki iþlem tutarýný hesapla (Alýþsa Alýþ Fiyatý, Satýþsa Satýþ Fiyatý)
            float unitPrice = isIncome ? targetInventory.PurchasePrice : targetInventory.SalePrice;
            float totalPayment = request.Quantity * unitPrice;

            var movement = new Stock_Movements
            {
                Id = Guid.NewGuid(),
                InventoryId = targetInventory.Id, // Yeni ise yeni ID, eskiyse eski ID
                MoveTypeId = request.MoveTypeId,
                UserId = request.UserId,
                SupplierId = request.SupplierId, // Command'da zorunlu Guid, boþsa Guid.Empty gelir
                Quantity = request.Quantity,
                Payment = totalPayment,
                Description = request.Description,
                CompanyId = targetInventory.CompanyId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _context.Stock_Movements.AddAsync(movement, cancellationToken);

            // Mevcut envanter güncellendiyse EF Core'a bildir (Yeni ise zaten Added durumunda)
            if (!request.IsNewInventory)
            {
                _context.Inventories.Update(targetInventory);
            }

            // ---------------------------------------------------------
            // 4. KAYDET (TRANSACTION)
            // ---------------------------------------------------------
            // Tüm iþlemler (Yeni Envanter + Stok Güncelleme + Hareket Kaydý) tek seferde yapýlýr.
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}