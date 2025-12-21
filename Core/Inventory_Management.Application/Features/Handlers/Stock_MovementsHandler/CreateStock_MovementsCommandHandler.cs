using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using Inventory_Management.Application.Interfaces;
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
        private readonly IEmailService _emailService;

        public CreateStock_MovementsCommandHandler(Inventory_Management_Context context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task Handle(CreateStock_MovementsCommand request, CancellationToken cancellationToken)
        {
            Inventories targetInventory = null;

            // 0. KULLANICIYI VE ��RKET�N� BUL
            // Envanter ve Hareket kay�tlar� kullan�c�n�n �irketine ait olmal�d�r.
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) throw new Exception("��lemi yapan kullan�c� sistemde bulunamad�.");

            // ---------------------------------------------------------
            // 1. ENVANTER BEL�RLEME (YEN� M� / MEVCUT MU?)
            // ---------------------------------------------------------

            if (request.IsNewInventory)
            {
                // --- SENARYO A: YEN� KART A�MA ---
                if (request.ProductId == null || request.ProductId == Guid.Empty)
                    throw new Exception("Yeni kart a�mak i�in bir �r�n se�melisiniz.");

                // Yeni Envanter Nesnesi (Hen�z veritaban�nda yok)
                targetInventory = new Inventories
                {
                    Id = Guid.NewGuid(),
                    ProductId = request.ProductId.Value,
                    CompanyId = user.CompanyId, // Kullan�c�n�n �irketine kaydet
                    Quantity = 0, // Miktar� a�a��da hareket ile g�ncelleyece�iz

                    // Command'den gelen verileri ata
                    PurchasePrice = request.PurchasePrice,
                    SalePrice = request.SalePrice,
                    CriticalStockQuantity = request.CriticalStockQuantity,
                    BatchNumber = !string.IsNullOrEmpty(request.BatchNumber) ? request.BatchNumber : "AUTO-" + DateTime.Now.ToString("yyMMdd"),
                    ExpirationDate = request.ExpirationDate,

                    Description = "Stok Hareketi ile olu�turuldu.",
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
                    throw new Exception("L�tfen listeye eklemek i�in bir stok kart� se�iniz.");

                targetInventory = await _context.Inventories
                    .Include(x => x.Product)
                    .FirstOrDefaultAsync(x => x.Id == request.InventoryId, cancellationToken);

                if (targetInventory == null)
                    throw new Exception("Se�ilen envanter kayd� bulunamad�.");
            }

            // ---------------------------------------------------------
            // 2. HAREKET T�P� VE STOK M�KTARI G�NCELLEME
            // ---------------------------------------------------------

            var moveType = await _context.Move_Types
                .FirstOrDefaultAsync(x => x.Id == request.MoveTypeId, cancellationToken);

            if (moveType == null)
                throw new Exception("Ge�ersiz hareket tipi!");

            // B�y�k/K���k harf duyars�z kontrol
            string typeName = moveType.MoveType.ToLower();
            bool isIncome = typeName.Contains("stock in") || typeName.Contains("giri�") || typeName.Contains("in");
            bool isOutcome = typeName.Contains("stock out") || typeName.Contains("��k��") || typeName.Contains("out");

            if (isIncome)
            {
                targetInventory.Quantity += request.Quantity;
            }
            else if (isOutcome)
            {
                // Yeni kart a��l�yorsa stok zaten 0'd�r, eksiye d��emez.
                if (targetInventory.Quantity < request.Quantity)
                {
                    throw new Exception($"Yetersiz Stok! Mevcut: {targetInventory.Quantity}, ��k�� �stenen: {request.Quantity}");
                }
                targetInventory.Quantity -= request.Quantity;
            }

            // ---------------------------------------------------------
            // 3. F�YAT HESAPLAMA VE HAREKET KAYDI
            // ---------------------------------------------------------

            // O anki i�lem tutar�n� hesapla (Al��sa Al�� Fiyat�, Sat��sa Sat�� Fiyat�)
            float unitPrice = isIncome ? targetInventory.PurchasePrice : targetInventory.SalePrice;
            float totalPayment = request.Quantity * unitPrice;

            var movement = new Stock_Movements
            {
                Id = Guid.NewGuid(),
                InventoryId = targetInventory.Id, // Yeni ise yeni ID, eskiyse eski ID
                MoveTypeId = request.MoveTypeId,
                UserId = request.UserId,
                SupplierId = request.SupplierId, // Command'da zorunlu Guid, bo�sa Guid.Empty gelir
                Quantity = request.Quantity,
                Payment = totalPayment,
                Description = request.Description,
                CompanyId = targetInventory.CompanyId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _context.Stock_Movements.AddAsync(movement, cancellationToken);

            // Mevcut envanter g�ncellendiyse EF Core'a bildir (Yeni ise zaten Added durumunda)
            if (!request.IsNewInventory)
            {
                _context.Inventories.Update(targetInventory);
            }

            // ---------------------------------------------------------
            // 4. KAYDET (TRANSACTION)
            // ---------------------------------------------------------
            // T�m i�lemler (Yeni Envanter + Stok G�ncelleme + Hareket Kayd�) tek seferde yap�l�r.
            await _context.SaveChangesAsync(cancellationToken);

            // 5. MAIL GONDERIMI (Kritik Stok Kontrolü)
            if (isOutcome && targetInventory.Quantity <= targetInventory.CriticalStockQuantity)
            {
                try 
                {
                    // Şirketin maili olan tüm kullanıcılarını getir
                    var companyUsers = await _context.Users
                        .Where(u => u.CompanyId == targetInventory.CompanyId && !string.IsNullOrEmpty(u.Email))
                        .ToListAsync(cancellationToken);

                    // Tedarikçi adını bul
                    string supplierName = "Belirtilmemiş";
                    if (request.SupplierId != Guid.Empty)
                    {
                        var supplier = await _context.Suppliers.FindAsync(new object[] { request.SupplierId }, cancellationToken);
                        if (supplier != null) supplierName = supplier.SupplierName;
                    }

                    foreach (var u in companyUsers)
                    {
                         string productName = targetInventory.Product?.ProductName ?? "Bilinmeyen Ürün";
                         string barcode = targetInventory.Product?.Barcode ?? "-";
                         string batchNumber = targetInventory.BatchNumber ?? "-";
                         
                         string subject = $"KRİTİK STOK UYARISI: {productName}";
                         
                         string body = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                                <h2 style='color: #d9534f;'>Kritik Stok Uyarısı</h2>
                                <p>Aşağıdaki ürün için stok miktarı kritik seviyenin altına düşmüştür.</p>
                                <ul style='background-color: #f9f9f9; padding: 15px; list-style-type: none;'>
                                    <li><strong>Ürün:</strong> {productName}</li>
                                    <li><strong>Barkod:</strong> {barcode}</li>
                                    <li><strong>Seri/Parti No:</strong> {batchNumber}</li>
                                    <li><strong>Tedarikçi:</strong> {supplierName}</li>
                                    <li style='margin-top: 10px;'><strong>Mevcut Stok:</strong> <span style='color:red; font-weight:bold'>{targetInventory.Quantity}</span></li>
                                    <li><strong>Kritik Seviye:</strong> {targetInventory.CriticalStockQuantity}</li>
                                </ul>
                                <p>Lütfen en kısa sürede stok yenilemesi yapınız.</p>
                                <p style='font-size: 12px; color: #888;'>Bu mesaj otomatik olarak gönderilmiştir.</p>
                            </div>";

                        await _emailService.SendEmailAsync(u.Email, subject, body);
                    }
                }
                catch (Exception ex)
                {
                    // Mail hatası akışı bozmamalı
                    Console.WriteLine($"Mail gönderim hatası: {ex.Message}");
                }
            }
            else 
            {
                // Kritik seviye aşılmadı, işlem yok.
            }
        }
    }
}