using Inventory_Management.Application.Features.Commands.SuppliersCommand;
using Inventory_Management.Application.Interfaces;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.SuppliersHandler
{
    public class SendOrderEmailHandler : IRequestHandler<SendOrderEmailCommand>
    {
        private readonly Inventory_Management_Context _context;
        private readonly IEmailService _emailService;

        public SendOrderEmailHandler(Inventory_Management_Context context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task Handle(SendOrderEmailCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
            if (supplier == null) throw new Exception("Tedarikçi bulunamadı.");
            if (string.IsNullOrEmpty(supplier.Email)) throw new Exception("Tedarikçinin e-posta adresi kayıtlı değil.");

            // Ek Bilgileri Getir (InventoryId veya ProductId ile)
            Inventories inventory = null;
            Products product = null;

            if (request.InventoryId.HasValue && request.InventoryId.Value != Guid.Empty)
            {
                inventory = await _context.Inventories
                    .Include(i => i.Product)
                    .FirstOrDefaultAsync(i => i.Id == request.InventoryId.Value, cancellationToken);
                product = inventory?.Product;
            }
            
            if (product == null && request.ProductId.HasValue && request.ProductId.Value != Guid.Empty)
            {
                product = await _context.Products.FindAsync(new object[] { request.ProductId.Value }, cancellationToken);
            }

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            var barcode = product?.Barcode ?? "-";
            var batchNumber = inventory?.BatchNumber ?? "-";
            var prodName = product?.ProductName ?? request.ProductName; // Product tablosundan güncel ismi almayı tercih et
            
            var userPhone = user?.PhoneNumber ?? "-";
            var userEmail = user?.Email ?? "-";
            var companyAddress = user?.Company?.Address ?? "-";
            var companyName = user?.Company?.CompanyName ?? request.UserCompany;
            var descriptionHtml = !string.IsNullOrWhiteSpace(request.Description) 
                ? $"<div style='background-color: #fff3cd; padding: 10px; border-left: 4px solid #ffc107; margin: 10px 0; font-style: italic;'><strong>Not:</strong> {request.Description}</div>" 
                : "";

            var subject = $"Sipariş Talebi: {request.ProductName}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; max-width: 600px;'>
                    <h2 style='color: #0d6efd;'>Yeni Sipariş Talebi</h2>
                    <p>Merhaba,</p>
                    <p>Aşağıdaki ürün için sipariş talebimiz bulunmaktadır:</p>
                    
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>{request.ProductName}</h3>
                        <p style='margin: 5px 0;'><strong>Miktar:</strong> <span style='font-size: 1.2em; font-weight: bold; color: #0d6efd;'>{request.Quantity}</span></p>
                        <p style='margin: 5px 0;'><strong>Barkod:</strong> {barcode}</p>
                        <p style='margin: 5px 0;'><strong>Seri/Parti No:</strong> {batchNumber}</p>
                        {descriptionHtml}
                    </div>

                    <h4 style='border-bottom: 1px solid #eee; padding-bottom: 5px; margin-top: 20px;'>İletişim ve Teslimat Bilgileri</h4>
                    <p style='margin: 5px 0;'><strong>Firma:</strong> {companyName}</p>
                    <p style='margin: 5px 0;'><strong>Yetkili:</strong> {request.UserFullName}</p>
                    <p style='margin: 5px 0;'><strong>Telefon:</strong> {userPhone}</p>
                    <p style='margin: 5px 0;'><strong>E-posta:</strong> {userEmail}</p>
                    <p style='margin: 5px 0;'><strong>Teslimat Adresi:</strong><br/>{companyAddress}</p>
                    
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                    <small style='color: #6c757d;'>Bu e-posta InventoryETS stok yönetim sistemi üzerinden otomatik olarak gönderilmiştir.</small>
                </div>
            ";

            await _emailService.SendEmailAsync(supplier.Email, subject, body);
        }
    }
}