using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.Stock_MovementsResult
{
    public class GetStock_MovementsQueryResult : BaseEntity
    {
        public bool IsActive { get; set; }
        // --- Görüntülenecek İsimler (DTO Mantığı) ---
        public string ProductName { get; set; }      // Hangi Ürün?
        public string MoveTypeName { get; set; }     // Giriş mi Çıkış mı?
        public string UserName { get; set; }         // Kim yaptı?
        public string SupplierName { get; set; }     // Hangi Tedarikçi? (Varsa)
        public string? BatchNumber { get; set; }       // Parti Numarası
        public DateTime ExpirationDate { get; set; }  // Son Kullanma Tarihi

        // --- Sayısal Veriler ---
        public int Quantity { get; set; }            // Adet
        public float Payment { get; set; }           // HESAPLANACAK TUTAR (Miktar x Fiyat)

        // --- Düzeltilen Alan ---
        public string? Description { get; set; }      
    }
}