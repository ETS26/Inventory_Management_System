
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.Stock_MovementsResult
{
    public class GetStock_MovementsQueryResult : BaseEntity
    {
        // --- Görüntülenecek Ýsimler (DTO Mantýðý) ---
        public string ProductName { get; set; }      // Hangi Ürün?
        public string MoveTypeName { get; set; }     // Giriþ mi Çýkýþ mý?
        public string UserName { get; set; }         // Kim yaptý?
        public string SupplierName { get; set; }     // Hangi Tedarikçi? (Varsa)
        public string? BatchNumber { get; set; }       // Parti Numarasý
        public DateTime ExpirationDate { get; set; }  // Son Kullanma Tarihi

        // --- Sayýsal Veriler ---
        public int Quantity { get; set; }            // Adet
        public float Payment { get; set; }           // HESAPLANACAK TUTAR (Miktar x Fiyat)

        // --- Düzeltilen Alan ---
        public string? Description { get; set; }      
    }
}
