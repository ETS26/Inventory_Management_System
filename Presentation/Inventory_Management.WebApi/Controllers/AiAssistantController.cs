using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Inventory_Management.Persistance.Context;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.ProductsCommand;
using Inventory_Management.Application.Features.Queries.ProductsQuery;
using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using Inventory_Management.Application.Features.Queries.Stock_MovementsQuery;
using Inventory_Management.Application.Features.Queries.InventoriesQuery;
using Inventory_Management.Application.Features.Commands.InventoriesCommand;
using Inventory_Management.Application.Features.Commands.SuppliersCommand;
using Inventory_Management.Application.Features.Commands.Unit_TypesCommand;
using Inventory_Management.Domain.Common;
using Inventory_Management.Application.Features.Queries.SuppliersQuery;
using Inventory_Management.Application.Features.Queries.Delivery_RulesQuery;

namespace Inventory_Management.WebApi.Controllers
{
    // --- MODELS ---
    public class UserQueryModel
    {
        public string Query { get; set; }
        public string? SessionId { get; set; }
    }

    public class ClearSessionRequest
    {
        public string SessionId { get; set; }
    }

    public class ConversationMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
    public class SessionContext
    {
        public string CurrentOperation { get; set; }
        public string CurrentStep { get; set; }
        public Dictionary<string, object> CollectedData { get; set; } = new();
        public List<ConversationMessage> History { get; set; } = new();
    }

    #region Gemini API Models
    public class GeminiCandidate { [JsonPropertyName("content")] public GeminiContent Content { get; set; } }
    public class GeminiContent { [JsonPropertyName("parts")] public GeminiPart[] Parts { get; set; } }
    public class GeminiPart { [JsonPropertyName("text")] public string Text { get; set; } }
    public class GeminiResponse { [JsonPropertyName("candidates")] public GeminiCandidate[] Candidates { get; set; } }
    #endregion

    #region AI Command Models
    public class AiCommand
    {
        [JsonPropertyName("operation")] public string Operation { get; set; }
        [JsonPropertyName("entity")] public string Entity { get; set; }
        [JsonPropertyName("filters")] public AiFilters Filters { get; set; }
        [JsonPropertyName("payload")] public JsonElement? Payload { get; set; }
    }

    public class AiFilters
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("userName")] public string UserName { get; set; }
        [JsonPropertyName("categoryName")] public string CategoryName { get; set; }
        [JsonPropertyName("productName")] public string ProductName { get; set; }
        [JsonPropertyName("supplierName")] public string SupplierName { get; set; }
        [JsonPropertyName("moveTypeName")] public string MoveTypeName { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("quantityFilterType")] public string QuantityFilterType { get; set; }
        [JsonPropertyName("dateRange")] public string DateRange { get; set; }
        [JsonPropertyName("take")] public int? Take { get; set; }
        [JsonPropertyName("isBelowCriticalStock")] public bool? IsBelowCriticalStock { get; set; }
        [JsonPropertyName("expirationDate")] public string ExpirationDate { get; set; }
        [JsonPropertyName("isActive")] public bool? IsActive { get; set; }
        [JsonPropertyName("sortBy")] public string SortBy { get; set; }
        [JsonPropertyName("barcode")] public string Barcode { get; set; }
    }
    #endregion

    [ApiController]
    [Route("api/[controller]")]
    public class AiAssistantController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private readonly Inventory_Management_Context _context;
        private readonly ICurrentUserService _currentUserService;

        private static readonly Dictionary<string, SessionContext> _sessions = new();

        public AiAssistantController(IHttpClientFactory httpClientFactory, IMediator mediator, IConfiguration configuration, Inventory_Management_Context context, ICurrentUserService currentUserService)
        {
            _httpClientFactory = httpClientFactory;
            _mediator = mediator;
            _configuration = configuration;
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessQuery([FromBody] UserQueryModel model)
        {
            string userQuery = model.Query;
            string sessionId = model.SessionId ?? Guid.NewGuid().ToString();

            string apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return StatusCode(500, new { response = "Gemini API anahtarı sunucuda yapılandırılmamış veya geçersiz." });
            }
            apiKey = string.Join("", apiKey.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            if (!_sessions.ContainsKey(sessionId))
            {
                _sessions[sessionId] = new SessionContext();
            }
            var session = _sessions[sessionId];

            session.History.Add(new ConversationMessage { Role = "user", Content = userQuery });

            var client = _httpClientFactory.CreateClient();
            // BAŞINDAKİ '$' İŞARETİNE VE SONUNDAKİ 'key={apiKey}' KISMINA DİKKAT EDİN
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemma-3-12b-it:generateContent?key={apiKey}";

            // SYSTEM PROMPT GÜNCELLENDİ: Daha net entity tanımları
            var systemPrompt = @"Sen bir envanter yönetim asistanısın. Kullanıcı komutlarını JSON'a çevir veya soru sor.

# TEMEL KURALLAR
1. 'kaç', 'adet', 'sayı', 'toplam', 'değeri ne' -> CALCULATE
2. 'listele', 'göster', 'getir', 'bakalım', 'programı ne', 'takvimi' -> GET
3. 'ekle', 'yeni', 'oluştur', 'girişi yap', 'çıkışı yap' -> POST (Eksik bilgi varsa sor!)
4. 'güncelle', 'değiştir', 'fiyatını yap', 'adını yap' -> UPDATE
5. 'sil', 'kaldır', 'pasif et' -> DELETE
6. Anlamadıysan ""Komutu anlayamadım"" deme, ""Daha farklı bir şekilde ifade edebilir misiniz?"" gibi bir soru sor. Asla JSON oluşturma.

# ENTITY (VARLIK) EŞLEŞTİRMELERİ (Buna kesinlikle uy!)
- 'ürün', 'malzeme', 'tanım' -> 'Product'
- 'envanter', 'stok', 'depo', 'elimde ne var' -> 'Inventory'
- 'hareket', 'işlem', 'giriş çıkış', 'geçmiş' -> 'Stock_Movement'
- 'tedarikçi', 'firma', 'distribütör' -> 'Supplier'
- 'teslimat kuralı', 'sevkiyat programı', 'geliş günü' -> 'Delivery_Rule'
- 'kategori' -> 'Category'
- 'birim', 'birim tipi' -> 'Unit_Type'

# 100 ÖRNEK SENARYO

## GET (Listeleme) Senaryoları (30 Adet)
1. ""ürünleri listele"" -> { ""operation"": ""GET"", ""entity"": ""Product"" }
2. ""envanteri getir"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"" }
3. ""stok durumunu göster"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"" }
4. ""kritik stok seviyesinin altındaki ürünler neler?"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""isBelowCriticalStock"": true } }
5. ""stok hareketlerini getir"" -> { ""operation"": ""GET"", ""entity"": ""Stock_Movement"" }
6. ""son 20 işlemi göster"" -> { ""operation"": ""GET"", ""entity"": ""Stock_Movement"", ""filters"": { ""take"": 20 } }
7. ""bugünkü giriş çıkışlar neler?"" -> { ""operation"": ""GET"", ""entity"": ""Stock_Movement"", ""filters"": { ""dateRange"": ""today"" } }
8. ""tedarikçileri listele"" -> { ""operation"": ""GET"", ""entity"": ""Supplier"" }
9. ""Logitech firmasının teslimat takvimi nasıl?"" -> { ""operation"": ""GET"", ""entity"": ""Delivery_Rule"", ""filters"": { ""supplierName"": ""Logitech"" } }
10. ""haftalık sevkiyatları göster"" -> { ""operation"": ""GET"", ""entity"": ""Delivery_Rule"", ""filters"": { ""frequency"": ""Weekly"" } }
11. ""kategorileri getir"" -> { ""operation"": ""GET"", ""entity"": ""Category"" }
12. ""birim tipleri neler?"" -> { ""operation"": ""GET"", ""entity"": ""Unit_Type"" }
13. ""içecek kategorisindeki ürünleri göster"" -> { ""operation"": ""GET"", ""entity"": ""Product"", ""filters"": { ""categoryName"": ""içecek"" } }
14. ""envanterdeki Fanta ürününü bul"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""productName"": ""Fanta"" } }
15. ""son kullanma tarihi bu ay dolacak ürünler"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""expirationDate"": ""this_month"" } }
16. ""Ahmet'in yaptığı son 5 işlemi listele"" -> { ""operation"": ""GET"", ""entity"": ""Stock_Movement"", ""filters"": { ""userName"": ""Ahmet"", ""take"": 5 } }
17. ""pasif durumdaki ürünler"" -> { ""operation"": ""GET"", ""entity"": ""Product"", ""filters"": { ""isActive"": false } }
18. ""ABC firmasından yapılan alımlar"" -> { ""operation"": ""GET"", ""entity"": ""Stock_Movement"", ""filters"": { ""supplierName"": ""ABC"", ""moveTypeName"": ""Giriş"" } }
19. ""en pahalı 5 ürünü göster"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""sortBy"": ""salePrice_desc"", ""take"": 5 } }
20. ""stok miktarı 10'dan az olanlar"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""quantity"": 10, ""quantityFilterType"": ""less_than"" } }
21. ""Geçen hafta hangi ürünler satıldı?"" -> { ""operation"": ""GET"", ""entity"": ""Stock_Movement"", ""filters"": { ""dateRange"": ""last_week"", ""moveTypeName"": ""Çıkış"" } }
22. ""En son eklenen 3 tedarikçi kim?"" -> { ""operation"": ""GET"", ""entity"": ""Supplier"", ""filters"": { ""sortBy"": ""createdAt_desc"", ""take"": 3 } }
23. ""Aylık teslimat yapan firmalar"" -> { ""operation"": ""GET"", ""entity"": ""Delivery_Rule"", ""filters"": { ""frequency"": ""Monthly"" } }
24. ""Envanterdeki en ucuz ürün hangisi?"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""sortBy"": ""salePrice_asc"", ""take"": 1 } }
25. ""Can kullanıcısının tüm hareketleri"" -> { ""operation"": ""GET"", ""entity"": ""Stock_Movement"", ""filters"": { ""userName"": ""Can"" } }
26. ""Satış fiyatı 100 TL ile 500 TL arasındaki ürünler"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""minSalePrice"": 100, ""maxSalePrice"": 500 } }
27. ""Tedarikçileri isme göre alfabetik sırala"" -> { ""operation"": ""GET"", ""entity"": ""Supplier"", ""filters"": { ""sortBy"": ""supplierName_asc"" } }
28. ""Stokta hiç kalmamış ürünler hangileri?"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""quantity"": 0, ""quantityFilterType"": ""equal"" } }
29. ""İsmi 'pro' ile başlayan ürünleri listele."" -> { ""operation"": ""GET"", ""entity"": ""Product"", ""filters"": { ""nameStartsWith"": ""pro"" } }
30. ""En yüksek stoklu 3 ürün hangisi?"" -> { ""operation"": ""GET"", ""entity"": ""Inventory"", ""filters"": { ""sortBy"": ""quantity_desc"", ""take"": 3 } }

## POST (Ekleme) Senaryoları (20 Adet)
31. ""yeni ürün ekle"" -> (Sohbet başlat) ""Harika, ürünün adı ne olacak?""
32. ""stok girişi yap"" -> (Sohbet başlat) ""Hangi ürün için stok girişi yapılacak?""
33. ""yeni tedarikçi kaydet"" -> (Sohbet başlat) ""Tedarikçi firmanın tam adı nedir?""
34. ""yeni kategori oluştur"" -> (Sohbet başlat) ""Kategorinin adı ne olacak?""
35. ""Logitech için yeni teslimat kuralı ekle"" -> (Sohbet başlat) ""Kuralın adı ne olsun? (Örn: Haftalık Klavye Sevkiyatı)""
36. ""10 adet kola satıldı"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""kola"", ""quantity"": 10, ""moveTypeName"": ""Çıkış"" } }
37. ""Yeni ürün: 'Gaming Mouse', Barkod: '12345', Kategori: 'Elektronik', Birim: 'Adet'"" -> { ""operation"": ""POST"", ""entity"": ""Product"", ""payload"": { ""productName"": ""Gaming Mouse"", ""barcode"": ""12345"", ""categoryName"": ""Elektronik"", ""unitTypeName"": ""Adet"" } }
38. ""Pınar firmasından 5 koli süt geldi, alış fiyatı 200 TL"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""Süt"", ""quantity"": 5, ""unitTypeName"": ""Koli"", ""supplierName"": ""Pınar"", ""purchasePrice"": 200, ""moveTypeName"": ""Giriş"" } }
39. ""Yeni birim tipi ekle: Kasa"" -> { ""operation"": ""POST"", ""entity"": ""Unit_Type"", ""payload"": { ""unitName"": ""Kasa"" } }
40. ""'Test' adında yeni kategori oluştur"" -> { ""operation"": ""POST"", ""entity"": ""Category"", ""payload"": { ""categoryName"": ""Test"" } }
41. ""1 adet bozuk monitörü iade olarak giriş yap"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""monitör"", ""quantity"": 1, ""moveTypeName"": ""İade Girişi"" } }
42. ""Yeni firma: 'Süper Lojistik', İletişim: 'Mehmet Kaplan', E-posta: 'm.kaplan@super.com', Telefon: '05051112233', Adres: 'Lojistik Cad. No: 5, İstanbul'"" -> { ""operation"": ""POST"", ""entity"": ""Supplier"", ""payload"": { ""supplierName"": ""Süper Lojistik"", ""contactPerson"": ""Mehmet Kaplan"", ""email"": ""m.kaplan@super.com"", ""phoneNumber"": ""05051112233"", ""address"": ""Lojistik Cad. No: 5, İstanbul"" } }
43. ""'Gıda' kategorisi oluştur, açıklaması 'Tüketilebilir ürünler' olsun"" -> { ""operation"": ""POST"", ""entity"": ""Category"", ""payload"": { ""categoryName"": ""Gıda"", ""description"": ""Tüketilebilir ürünler"" } }
44. ""Süper Lojistik için aylık teslimat kuralı oluştur. Ayın 15'inde saat 10:00'da"" -> { ""operation"": ""POST"", ""entity"": ""Delivery_Rule"", ""payload"": { ""supplierName"": ""Süper Lojistik"", ""ruleName"": ""Aylık Genel Sevkiyat"", ""frequency"": ""Monthly"", ""daysOfMonth"": ""15"", ""arrivalTime"": ""10:00"" } }
45. ""Sayım fazlası olarak 2 adet klavye girişi yap"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""klavye"", ""quantity"": 2, ""moveTypeName"": ""Sayım Fazlası"" } }
46. ""Yeni bir ürün tanımlamak istiyorum"" -> (Sohbet başlat) ""Tabii, ürünün adı nedir?""
47. ""Stoktan ürün düşelim"" -> (Sohbet başlat) ""Hangi üründen kaç adet düşülecek?""
48. ""Yeni bir firma ekle"" -> (Sohbet başlat) ""Harika, firma adı nedir?""
49. ""10 paket makarna için stok girişi yap"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""Makarna"", ""quantity"": 10, ""moveTypeName"": ""Giriş"" } }
50. ""Bir sevkiyat kuralı tanımlayalım"" -> (Sohbet başlat) ""Hangi tedarikçi için kural tanımlanacak?""

## UPDATE (Güncelleme) Senaryoları (25 Adet)
51. ""Kola'nın satış fiyatını 15 TL yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""filters"": { ""productName"": ""Kola"" }, ""payload"": { ""salePrice"": 15 } }
52. ""Logitech klavyenin kritik stok seviyesini 20 yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""filters"": { ""productName"": ""Logitech klavye"" }, ""payload"": { ""criticalStockQuantity"": 20 } }
53. ""12345 barkodlu ürünün adını 'Süper Mouse' olarak değiştir"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""barcode"": ""12345"" }, ""payload"": { ""productName"": ""Süper Mouse"" } }
54. ""Pınar tedarikçisinin yetkilisini 'Ali Yılmaz' yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Supplier"", ""filters"": { ""supplierName"": ""Pınar"" }, ""payload"": { ""contactPerson"": ""Ali Yılmaz"" } }
55. ""Elektronik kategorisinin adını 'Elektronik Ürünler' olarak güncelle"" -> { ""operation"": ""UPDATE"", ""entity"": ""Category"", ""filters"": { ""name"": ""Elektronik"" }, ""payload"": { ""categoryName"": ""Elektronik Ürünler"" } }
56. ""ID'si 1 olan teslimat kuralının saati 16:00 yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Delivery_Rule"", ""filters"": { ""id"": ""1"" }, ""payload"": { ""arrivalTime"": ""16:00"" } }
57. ""Süt ürününün kategorisini 'Süt Ürünleri' yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""name"": ""Süt"" }, ""payload"": { ""categoryName"": ""Süt Ürünleri"" } }
58. ""Klavye ürününün açıklamasını 'Mekanik ve Işıklı' olarak ayarla"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""name"": ""Klavye"" }, ""payload"": { ""description"": ""Mekanik ve Işıklı"" } }
59. ""Tüm süt ürünlerinin satış fiyatını %10 artır"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""filters"": { ""categoryName"": ""Süt Ürünleri"" }, ""payload"": { ""increasePercent"": 10, ""field"": ""salePrice"" } }
60. ""'Adet' birim tipini 'Birim' olarak değiştir"" -> { ""operation"": ""UPDATE"", ""entity"": ""Unit_Type"", ""filters"": { ""name"": ""Adet"" }, ""payload"": { ""unitName"": ""Birim"" } }
61. ""Stok sayımı yaptım, 'Defter' ürünü 120 adet, düzelt"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""filters"": { ""productName"": ""Defter"" }, ""payload"": { ""quantity"": 120 } }
62. ""Tüm 'Kırtasiye' ürünlerinin kritik stok seviyesini 25 yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""filters"": { ""categoryName"": ""Kırtasiye"" }, ""payload"": { ""criticalStockQuantity"": 25 } }
63. ""'Logi Mouse' ürününü 'Logitech Mouse' olarak düzelt"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""name"": ""Logi Mouse"" }, ""payload"": { ""productName"": ""Logitech Mouse"" } }
64. ""'Mega Market' tedarikçisinin telefon numarasını '5550001122' yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Supplier"", ""filters"": { ""name"": ""Mega Market"" }, ""payload"": { ""phoneNumber"": ""5550001122"" } }
65. ""Haftalık sevkiyat kuralını 2 haftada bir olarak güncelle"" -> { ""operation"": ""UPDATE"", ""entity"": ""Delivery_Rule"", ""filters"": { ""name"": ""Haftalık Sevkiyat"" }, ""payload"": { ""interval"": 2 } }
66. ""'Tişört' ürününü aktif yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""name"": ""Tişört"" }, ""payload"": { ""isActive"": true } }
67. ""'Manav' kategorisindeki ürünlerin fiyatını %20 düşür"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""filters"": { ""categoryName"": ""Manav"" }, ""payload"": { ""decreasePercent"": 20, ""field"": ""salePrice"" } }
68. ""'Mousepad' ürününü 'Aksesuarlar' kategorisine taşı"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""name"": ""Mousepad"" }, ""payload"": { ""categoryName"": ""Aksesuarlar"" } }
69. ""Bütün teslimat kurallarını pasif yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Delivery_Rule"", ""payload"": { ""isActive"": false } }
70. ""Tüm envanterin son sayım tarihini bugüne ayarla"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""payload"": { ""lastCountDate"": ""today"" } }
71. ""'Pınar Süt' ürününün parti numarasını 'PNR-2025-Q4' olarak güncelle"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""filters"": { ""productName"": ""Pınar Süt"" }, ""payload"": { ""batchNumber"": ""PNR-2025-Q4"" } }
72. ""Tüm ürünlerin açıklamalarını 'Standart Ürün' olarak ayarla"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""payload"": { ""description"": ""Standart Ürün"" } }
73. ""Pasif olan tüm tedarikçileri tekrar aktif et"" -> { ""operation"": ""UPDATE"", ""entity"": ""Supplier"", ""filters"": { ""isActive"": false }, ""payload"": { ""isActive"": true } }
74. ""'Günlük Süt' kuralının teslimat saatini 08:30 yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Delivery_Rule"", ""filters"":{ ""ruleName"": ""Günlük Süt"" }, ""payload"": { ""arrivalTime"": ""08:30"" } }
75. ""Envanterdeki tüm ürünlerin alış fiyatını sıfırla"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""payload"": { ""purchasePrice"": 0 } }

## DELETE (Silme) Senaryoları (10 Adet)
76. ""Kırık sandalyeyi envanterden düş"" -> { ""operation"": ""DELETE"", ""entity"": ""Inventory"", ""filters"": { ""productName"": ""Kırık sandalye"" } }
77. ""Eski Test kategorisini sil"" -> { ""operation"": ""DELETE"", ""entity"": ""Category"", ""filters"": { ""name"": ""Eski Test"" } }
78. ""ABC firmasini sil"" -> { ""operation"": ""DELETE"", ""entity"": ""Supplier"", ""filters"": { ""name"": ""ABC"" } }
79. ""ID'si 5 olan teslimat kuralını sil"" -> { ""operation"": ""DELETE"", ""entity"": ""Delivery_Rule"", ""filters"": { ""id"": ""5"" } }
80. ""Son kullanma tarihi geçmiş tüm ürünleri stoktan düş"" -> { ""operation"": ""DELETE"", ""entity"": ""Inventory"", ""filters"": { ""expirationDate"": ""expired"" } }
81. ""'İçecekler' kategorisini sil"" -> { ""operation"": ""DELETE"", ""entity"": ""Category"", ""filters"": { ""name"": ""İçecekler"" } }
82. ""Stokta olmayan tüm envanter kayıtlarını temizle"" -> { ""operation"": ""DELETE"", ""entity"": ""Inventory"", ""filters"": { ""quantity"": 0 } }
83. ""'XYZ Lojistik' firmasını pasif yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Supplier"", ""filters"": { ""name"": ""XYZ Lojistik"" }, ""payload"": { ""isActive"": false } }
84. ""Tüm 'Eski' ile başlayan ürünleri sil"" -> { ""operation"": ""DELETE"", ""entity"": ""Product"", ""filters"": { ""nameStartsWith"": ""Eski"" } }
85. ""'Koli' ve 'Paket' birim tiplerini sil"" -> { ""operation"": ""DELETE"", ""entity"": ""Unit_Type"", ""filters"": { ""names"": [""Koli"", ""Paket""] } }

## CALCULATE (Hesaplama) Senaryoları (15 Adet)
86. ""toplam envanter değeri ne kadar?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""InventoryTotalValue"" }
87. ""Elektronik kategorisindeki ürünlerin toplam stok sayısı?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""InventoryCategoryQuantity"", ""filters"": { ""categoryName"": ""Elektronik"" } }
88. ""kaç çeşit ürün var?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""ProductCount"" }
89. ""bu ay kaç adet ürün satıldı?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""StockMovementCount"", ""filters"": { ""dateRange"": ""this_month"", ""moveTypeName"": ""Çıkış"" } }
90. ""sistemde kaç tedarikçi kayıtlı?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""SupplierCount"" }
91. ""Ortalama ürün satış fiyatı ne kadar?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""AverageSalePrice"" }
92. ""Bu hafta kaç adet yeni ürün eklendi?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""ProductCount"", ""filters"": { ""dateRange"": ""this_week"" } }
93. ""En çok stok hangi üründe var?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""HighestStockProduct"" }
94. ""Dünkü toplam ciro ne kadardı?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""SalesTotal"", ""filters"": { ""dateRange"": ""yesterday"" } }
95. ""'Razer' içeren tüm ürünlerin stoğunu say"" -> { ""operation"": ""CALCULATE"", ""entity"": ""InventoryTotalQuantity"", ""filters"": { ""productName"": ""Razer"" } }
96. ""Kaç tane aktif tedarikçim var?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""SupplierCount"", ""filters"": { ""isActive"": true } }
97. ""En son kim stok girişi yaptı?"" -> { ""operation"": ""GET"", ""entity"": ""Stock_Movement"", ""filters"": { ""moveTypeName"": ""Giriş"", ""sortBy"": ""createdAt_desc"", ""take"": 1 } }
98. ""Envanterdeki ürünlerin ortalama stokta kalma süresi?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""AverageStockDuration"" }
99. ""Toplam kategori sayısı?"" -> { ""operation"": ""CALCULATE"", ""entity"": ""CategoryCount"" }
100. ""Bana yardım ettiğin için teşekkürler"" -> (Sohbet) ""Rica ederim! Başka bir isteğiniz var mı?""
";

            var conversationHistory = new StringBuilder();
            var recentMessages = session.History.TakeLast(16).ToList();

            if (recentMessages.Any())
            {
                conversationHistory.AppendLine("\n# GEÇMİŞ KONUŞMA:");
                foreach (var msg in recentMessages)
                {
                    conversationHistory.AppendLine($"- {(msg.Role == "user" ? "Kullanıcı" : "Sen")}: {msg.Content}");
                }
            }

            var fullPrompt = systemPrompt + conversationHistory.ToString() + $"\n\n# ŞİMDİ\nKullanıcı: {userQuery}\n\n(Geçmişi dikkate al. CREATE işlemindeysen eksikleri sor. Değilse JSON üret.)";

            var payload = new { contents = new[] { new { parts = new[] { new { text = fullPrompt } } } } };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(requestUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { response = $"Google AI API hatası: {errorContent}", sessionId });
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);
                var llmOutput = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text.Trim();

                if (string.IsNullOrWhiteSpace(llmOutput))
                {
                    return Ok(new { response = "Anlayamadım, lütfen daha farklı bir şekilde sormayı deneyin.", sessionId });
                }

                session.History.Add(new ConversationMessage { Role = "assistant", Content = llmOutput });

                var cleanJson = llmOutput
                    .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("```", "")
                    .Trim();

                int firstBrace = cleanJson.IndexOf('{');
                int lastBrace = cleanJson.LastIndexOf('}');

                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    cleanJson = cleanJson.Substring(firstBrace, lastBrace - firstBrace + 1);
                    var result = await ProcessAiCommand(cleanJson, session);

                    if (result is OkObjectResult okResult)
                    {
                        var resultValue = okResult.Value;
                        string responseText = "";
                        if (resultValue != null)
                        {
                            var valueType = resultValue.GetType();
                            var responseProp = valueType.GetProperty("response");
                            if (responseProp != null)
                            {
                                responseText = responseProp.GetValue(resultValue)?.ToString() ?? "";
                            }
                        }

                        if (responseText.Contains("başarıyla"))
                        {
                            session.CollectedData.Clear();
                            session.CurrentOperation = null;
                        }

                        return Ok(new { response = responseText, sessionId });
                    }

                    return Ok(new { response = "İşlem tamamlanamadı.", sessionId });
                }
                else
                {
                    return Ok(new { response = llmOutput, sessionId });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { response = $"Bir sunucu hatası oluştu: {ex.Message}", sessionId });
            }
        }

        private async Task<IActionResult> ProcessAiCommand(string llmJson, SessionContext session)
        {
            string responseText = "Bu komutu anlayamadım. Lütfen farklı bir şekilde ifade edin.";
            try
            {
                var command = JsonSerializer.Deserialize<AiCommand>(llmJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (command == null || string.IsNullOrEmpty(command.Operation))
                {
                    return Ok(new { response = "Komutu anlayamadım. Lütfen tekrar deneyin." });
                }

                // --- ENTITY NORMALIZATION (EŞLEŞTİRME DÜZELTMESİ) ---
                var entity = command.Entity?.ToLowerInvariant() ?? "";
                if (entity.Contains("product")) entity = "product";
                else if (entity.Contains("inventor")) entity = "inventory"; // inventory, inventories
                else if (entity.Contains("stock") && entity.Contains("movement")) entity = "stock_movement";
                else if (entity.Contains("supplier")) entity = "supplier";
                else if (entity.Contains("delivery") || entity.Contains("teslimat")) entity = "delivery_rule";
                else if (entity.Contains("category")) entity = "category";
                else if (entity.Contains("unit")) entity = "unit_type";

                var op = command.Operation.ToLowerInvariant();

                // -------------------------------------------------------------------------
                // 1. CREATE / POST İŞLEMLERİ
                // -------------------------------------------------------------------------
                if (op == "post" || op == "create")
                {
                    switch (entity)
                    {
                        case "product":
                            if (command.Payload.HasValue)
                            {
                                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());
                                string productName = payload.ContainsKey("productName") ? payload["productName"].GetString() : null;
                                string barcode = payload.ContainsKey("barcode") ? payload["barcode"].GetString() :  null;

                                if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(barcode))
                                {
                                    responseText = "Ürün oluşturmak için hem ürün adı hem de barkod gereklidir.";
                                    break;
                                }

                                var createCommand = new CreateProductsCommand { ProductName = productName, Barcode = barcode };
                                await _mediator.Send(createCommand);
                                session.CollectedData.Clear();
                                responseText = $"✅ '{productName}' ürünü başarıyla eklendi! (Barkod: {barcode})";
                            }
                            break;

                        case "category":
                            if (command.Payload.HasValue)
                            {
                                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());
                                string categoryName = payload.ContainsKey("categoryName") ? payload["categoryName"].GetString() : null;

                                if (string.IsNullOrWhiteSpace(categoryName)) { responseText = "Kategori adı gerekli."; break; }

                                var category = new Categories { Id = Guid.NewGuid(), CategoryName = categoryName, IsActive = true, CreatedAt = DateTime.UtcNow };
                                _context.Categories.Add(category);
                                await _context.SaveChangesAsync();
                                responseText = $"✅ '{categoryName}' kategorisi başarıyla oluşturuldu!";
                            }
                            break;

                        // Unit_Type (Birim Tipi) ekleme - Senaryo 39
                        case "unit_type":
                            if (command.Payload.HasValue)
                            {
                                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());
                                string unitName = payload.ContainsKey("unitName") ? payload["unitName"].GetString() : null;

                                if (string.IsNullOrWhiteSpace(unitName))
                                {
                                    responseText = "Birim tipi adı gerekli.";
                                    break;
                                }

                                var unitType = new Unit_Types
                                {
                                    Id = Guid.NewGuid(),
                                    UnitName = unitName,
                                    IsActive = true,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.Unit_Types.Add(unitType);
                                await _context.SaveChangesAsync();
                                responseText = $"✅ '{unitName}' birim tipi başarıyla oluşturuldu!";
                            }
                            break;

                        // Supplier (Tedarikçi) ekleme - Senaryo 42
                        case "supplier":
                            if (command.Payload.HasValue)
                            {
                                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());

                                string supplierName = payload.ContainsKey("supplierName") ? payload["supplierName"].GetString() : null;
                                string contactPerson = payload.ContainsKey("contactPerson") ? payload["contactPerson"].GetString() : null;
                                string email = payload.ContainsKey("email") ? payload["email"].GetString() : null;
                                string phoneNumber = payload.ContainsKey("phoneNumber") ? payload["phoneNumber"].GetString() : null;
                                string address = payload.ContainsKey("address") ? payload["address"].GetString() : null;

                                if (string.IsNullOrWhiteSpace(supplierName))
                                {
                                    responseText = "Tedarikçi adı gerekli.";
                                    break;
                                }

                                var createSupplierCmd = new CreateSuppliersCommand
                                {
                                    SupplierName = supplierName,
                                    ContactPerson = contactPerson,
                                    Email = email,
                                    PhoneNumber = phoneNumber,
                                    Address = address
                                };

                                await _mediator.Send(createSupplierCmd);
                                responseText = $"✅ '{supplierName}' tedarikçisi başarıyla eklendi!";
                            }
                            break;

                        // Stock_Movement (Stok Hareketi) ekleme - Senaryo 36, 37, 38, 41, 45, 49
                        case "stock_movement":
                            if (command.Payload.HasValue)
                            {
                                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());

                                string productName = payload.ContainsKey("productName") ? payload["productName"].GetString() : null;
                                int quantity = payload.ContainsKey("quantity") ? payload["quantity"].GetInt32() : 0;
                                string moveTypeName = payload.ContainsKey("moveTypeName") ? payload["moveTypeName"].GetString() : "Giriş";
                                string supplierName = payload.ContainsKey("supplierName") ? payload["supplierName"].GetString() : null;
                                float? purchasePrice = payload.ContainsKey("purchasePrice") ? payload["purchasePrice"].GetSingle() : null;

                                if (string.IsNullOrWhiteSpace(productName) || quantity <= 0)
                                {
                                    responseText = "Ürün adı ve miktar gerekli.";
                                    break;
                                }

                                // Ürünü bul
                                var product = await _context.Products
                                    .FirstOrDefaultAsync(p => p.ProductName.ToLower().Contains(productName.ToLower()) && p.IsActive);

                                if (product == null)
                                {
                                    responseText = $"'{productName}' ürünü bulunamadı. Önce ürünü sisteme ekleyin.";
                                    break;
                                }

                                // Envanter kaydını bul veya oluştur
                                var inventory = await _context.Inventories
                                    .FirstOrDefaultAsync(i => i.ProductId == product.Id && i.IsActive);

                                if (inventory == null)
                                {
                                    responseText = $"'{productName}' için envanter kaydı bulunamadı.";
                                    break;
                                }

                                // MoveType bul
                                var moveType = await _context.Move_Types
                                    .FirstOrDefaultAsync(m => m.MoveType.ToLower().Contains(moveTypeName.ToLower()) && m.IsActive);

                                if (moveType == null)
                                {
                                    responseText = $"'{moveTypeName}' hareket tipi bulunamadı.";
                                    break;
                                }

                                // Supplier varsa bul
                                Guid? supplierId = null;
                                if (!string.IsNullOrWhiteSpace(supplierName))
                                {
                                    var supplier = await _context.Suppliers
                                        .FirstOrDefaultAsync(s => s.SupplierName.ToLower().Contains(supplierName.ToLower()) && s.IsActive);
                                    supplierId = supplier?.Id;
                                }

                                // UserId kontrolü
                                Guid userId = _currentUserService.UserId;

                                // Stok hareketi oluştur
                                var movement = new Stock_Movements
                                {
                                    Id = Guid.NewGuid(),
                                    InventoryId = inventory.Id,
                                    Quantity = quantity,
                                    MoveTypeId = moveType.Id,
                                    UserId = userId,
                                    IsActive = true,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.Stock_Movements.Add(movement);

                                // Stok güncelle (Giriş ise artır, Çıkış ise azalt)
                                if (moveTypeName.ToLower().Contains("giriş") || moveTypeName.ToLower().Contains("iade") || moveTypeName.ToLower().Contains("sayım fazlası"))
                                {
                                    inventory.Quantity += quantity;
                                    if (purchasePrice.HasValue)
                                        inventory.PurchasePrice = purchasePrice.Value;
                                }
                                else if (moveTypeName.ToLower().Contains("çıkış") || moveTypeName.ToLower().Contains("satış"))
                                {
                                    if (inventory.Quantity < quantity)
                                    {
                                        responseText = $"⚠️ Yetersiz stok! '{productName}' için mevcut: {inventory.Quantity}, talep: {quantity}";
                                        break;
                                    }
                                    inventory.Quantity -= quantity;
                                }

                                await _context.SaveChangesAsync();
                                responseText = $"✅ {quantity} adet '{productName}' için {moveTypeName} işlemi başarıyla kaydedildi!";
                            }
                            break;

                        // Delivery_Rule (Teslimat Kuralı) ekleme - Senaryo 44
                        case "delivery_rule":
                            if (command.Payload.HasValue)
                            {
                                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());

                                string supplierName = payload.ContainsKey("supplierName") ? payload["supplierName"].GetString() : null;
                                string ruleName = payload.ContainsKey("ruleName") ? payload["ruleName"].GetString() : null;
                                string frequency = payload.ContainsKey("frequency") ? payload["frequency"].GetString() : "Weekly";
                                string daysOfMonth = payload.ContainsKey("daysOfMonth") ? payload["daysOfMonth"].GetString() : null;
                                string arrivalTime = payload.ContainsKey("arrivalTime") ? payload["arrivalTime"].GetString() : "09:00";

                                if (string.IsNullOrWhiteSpace(supplierName) || string.IsNullOrWhiteSpace(ruleName))
                                {
                                    responseText = "Tedarikçi adı ve kural adı gerekli.";
                                    break;
                                }

                                var supplier = await _context.Suppliers
                                    .FirstOrDefaultAsync(s => s.SupplierName.ToLower().Contains(supplierName.ToLower()) && s.IsActive);

                                if (supplier == null)
                                {
                                    responseText = $"'{supplierName}' tedarikçisi bulunamadı.";
                                    break;
                                }

                                var frequencyEnum = frequency.ToLower() == "monthly"
                                    ? Delivery_Rules.FrequencyType.Monthly
                                    : Delivery_Rules.FrequencyType.Weekly;

                                var rule = new Delivery_Rules
                                {
                                    Id = Guid.NewGuid(),
                                    SupplierId = supplier.Id,
                                    RuleName = ruleName,
                                    Frequency = frequencyEnum,
                                    DaysOfMonth = daysOfMonth,
                                    ArrivalTime = TimeSpan.Parse(arrivalTime),
                                    Interval = 1,
                                    IsActive = true,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.Delivery_Rules.Add(rule);
                                await _context.SaveChangesAsync();
                                responseText = $"✅ '{supplier.SupplierName}' için '{ruleName}' teslimat kuralı oluşturuldu!";
                            }
                            break;

                    }
                }
                // -------------------------------------------------------------------------
                // 2. CALCULATE İŞLEMLERİ
                // -------------------------------------------------------------------------
                else if (op == "calculate")
                {
                    switch (entity)
                    {
                        case "productcount":
                            var pCount = await _context.Products.CountAsync(p => p.IsActive);
                            responseText = $"Sistemde toplam {pCount} adet aktif ürün tanımı bulunmaktadır.";
                            break;
                        
                        case "inventorytotalvalue":
                             var totalValue = await _context.Inventories
                                .Where(i => i.IsActive)
                                .SumAsync(i => i.Quantity * i.PurchasePrice);
                            responseText = $" Mevcut envanterinizin toplam alış değeri: {totalValue:C2}.";
                            break;

                        case "suppliercount":
                            var supplierCount = await _context.Suppliers.CountAsync(s => s.IsActive);
                            responseText = $"Sistemde {supplierCount} adet tedarikçi kayıtlıdır.";
                            break;

                        case "stockmovementcount":
                             IQueryable<Stock_Movements> movementQuery = _context.Stock_Movements;
                             if (command.Filters?.DateRange == "this_month")
                             {
                                 var today = DateTime.UtcNow;
                                 var firstDay = new DateTime(today.Year, today.Month, 1);
                                 movementQuery = movementQuery.Where(m => m.CreatedAt >= firstDay);
                             }
                             if(command.Filters?.MoveTypeName?.ToLower() == "çıkış"){
                                 movementQuery = movementQuery.Where(m => m.MoveType.MoveType.ToLower().Contains("çıkış"));
                             }
                             var movementCount = await movementQuery.CountAsync();
                             responseText = $"Bu ay {movementCount} adet satış işlemi gerçekleşti.";
                             break;
                        
                        default:
                            responseText = $"'{command.Entity}' için hesaplama işlemi henüz tanımlanmamış.";
                            break;

                        // Ortalama satış fiyatı - Senaryo 91
                        case "averagesaleprice":
                            var avgSalePrice = await _context.Inventories
                                .Where(i => i.IsActive && i.SalePrice > 0)
                                .AverageAsync(i => i.SalePrice);
                            responseText = $"📊 Ortalama ürün satış fiyatı: {avgSalePrice:C2}";
                            break;

                        // En yüksek stoklu ürün - Senaryo 93
                        case "higheststockproduct":
                            var highestStock = await _context.Inventories
                                .Include(i => i.Product)
                                .Where(i => i.IsActive)
                                .OrderByDescending(i => i.Quantity)
                                .FirstOrDefaultAsync();

                            responseText = highestStock != null
                                ? $"📦 En çok stoklu ürün: **{highestStock.Product.ProductName}** ({highestStock.Quantity} adet)"
                                : "Envanter boş.";
                            break;

                        // Günlük satış toplamı - Senaryo 94
                        case "salestotal":
                            var salesQuery = _context.Stock_Movements
                                .Include(m => m.MoveType)
                                .Include(m => m.Inventory)
                                .Where(m => m.IsActive && m.MoveType.MoveType.ToLower().Contains("çıkış"));

                            if (command.Filters?.DateRange == "yesterday")
                            {
                                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                                salesQuery = salesQuery.Where(m => m.CreatedAt.Date == yesterday);
                            }

                            var totalSales = await salesQuery.SumAsync(m => m.Quantity * m.Inventory.SalePrice);
                            responseText = $"💰 Dünkü toplam satış cirosu: {totalSales:C2}";
                            break;

                        // Kategori sayısı - Senaryo 99
                        case "categorycount":
                            var categoryCount = await _context.Categories.CountAsync(c => c.IsActive);
                            responseText = $"📂 Sistemde {categoryCount} adet kategori bulunuyor.";
                            break;

                        // Envanter toplam miktar - Senaryo 95
                        case "inventorytotalquantity":
                            var totalQuantity = await _context.Inventories.Where(i => i.IsActive).SumAsync(i => i.Quantity);

                            if (command.Filters != null && !string.IsNullOrEmpty(command.Filters.ProductName))
                            {
                                totalQuantity = await _context.Inventories
                                    .Include(i => i.Product)
                                    .Where(i => i.IsActive && i.Product.ProductName.ToLower().Contains(command.Filters.ProductName.ToLower()))
                                    .SumAsync(i => i.Quantity);
                                responseText = $"📦 '{command.Filters.ProductName}' ürünlerinin toplam stoğu: {totalQuantity} adet";
                            }
                            else
                            {
                                responseText = $"📦 Toplam envanter miktarı: {totalQuantity} adet";
                            }
                            break;

                    }
                }
                // -------------------------------------------------------------------------
                // 3. GET (LIST) İŞLEMLERİ
                // -------------------------------------------------------------------------
                else if (op == "get")
                {
                    switch (entity)
                    {
                        // 1. ÜRÜNLERİ GETİR
                        case "product":
                            var products = await _mediator.Send(new GetProductsQuery { IsActive = true });
                            if (command.Filters != null && !string.IsNullOrEmpty(command.Filters.Name))
                            {
                                products = products.Where(p => p.ProductName.ToLower().Contains(command.Filters.Name.ToLower())).ToList();
                            }
                            
                            var productResponse = products.Take(command.Filters?.Take ?? 10)
                                .Select(p => $"**{p.ProductName}** (Barkod: {p.Barcode})");

                            responseText = products.Any()
                                ? $"📦 **Ürün Listesi**:\n- {string.Join("\n- ", productResponse)}"
                                : "Sistemde ürün bulunamadı.";
                            break;

                        // 2. ENVANTERİ GETİR (DÜZELTİLMİŞ VERSİYON)
                        case "inventory":
                            var inventoryQuery = _context.Inventories
                                .Include(i => i.Product) // İlişkili ürünü dahil et
                                .Where(i => i.IsActive); // Sadece aktif kayıtlar

                            // 1. ADIM: FİLTRELERİ UYGULA
                            if (command.Filters != null)
                            {
                                if (command.Filters.IsBelowCriticalStock == true)
                                    inventoryQuery = inventoryQuery.Where(i => i.Quantity <= i.CriticalStockQuantity);

                                if (!string.IsNullOrEmpty(command.Filters.ProductName))
                                    inventoryQuery = inventoryQuery.Where(i => i.Product != null && i.Product.ProductName.ToLower().Contains(command.Filters.ProductName.ToLower()));

                                if (command.Filters.Quantity.HasValue && !string.IsNullOrEmpty(command.Filters.QuantityFilterType))
                                {
                                    switch (command.Filters.QuantityFilterType)
                                    {
                                        case "less_than": inventoryQuery = inventoryQuery.Where(i => i.Quantity < command.Filters.Quantity.Value); break;
                                        case "greater_than": inventoryQuery = inventoryQuery.Where(i => i.Quantity > command.Filters.Quantity.Value); break;
                                        case "equal": inventoryQuery = inventoryQuery.Where(i => i.Quantity == command.Filters.Quantity.Value); break;
                                    }
                                }

                                if (!string.IsNullOrEmpty(command.Filters.ExpirationDate) && command.Filters.ExpirationDate == "this_month")
                                {
                                    var today = DateTime.UtcNow;
                                    var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                                    inventoryQuery = inventoryQuery.Where(i => i.ExpirationDate >= firstDayOfMonth && i.ExpirationDate <= lastDayOfMonth);
                                }
                            }

                            // 2. ADIM: SIRALAMA (SAFE SORTING)
                            // Varsayılan sıralama Product.ProductName üzerine, ancak null check ekledik.
                            if (!string.IsNullOrEmpty(command.Filters?.SortBy))
                            {
                                switch (command.Filters.SortBy)
                                {
                                    case "salePrice_desc": inventoryQuery = inventoryQuery.OrderByDescending(i => i.SalePrice); break;
                                    case "quantity_asc": inventoryQuery = inventoryQuery.OrderBy(i => i.Quantity); break;
                                    case "quantity_desc": inventoryQuery = inventoryQuery.OrderByDescending(i => i.Quantity); break;
                                    default: inventoryQuery = inventoryQuery.OrderBy(i => i.Product != null ? i.Product.ProductName : ""); break;
                                }
                            }
                            else
                            {
                                // Filtre yoksa varsayılan sıralama
                                inventoryQuery = inventoryQuery.OrderBy(i => i.Product != null ? i.Product.ProductName : "");
                            }

                            // 3. ADIM: VERİYİ ÇEK VE MAPLE
                            var inventoryItems = await inventoryQuery.Take(command.Filters?.Take ?? 15).ToListAsync();

                            var inventoryResponse = inventoryItems
                                .Select(i =>
                                {
                                    string pName = i.Product?.ProductName ?? "Bilinmeyen Ürün";
                                    string stockStatus = i.Quantity <= i.CriticalStockQuantity ? "⚠️ Kritik" : "✅";
                                    return $"**{pName}**: {i.Quantity} Adet (Min: {i.CriticalStockQuantity}) {stockStatus} - Fiyat: {i.SalePrice:C2}";
                                });

                            responseText = inventoryItems.Any()
                                ? $"🏭 **Envanter Durumu**:\n- {string.Join("\n- ", inventoryResponse)}"
                                : "Kriterlere uygun envanter kaydı bulunamadı (veya stok boş).";
                            break;

                        // 3. STOK HAREKETLERİ
                        case "stock_movement":
                            var movementsQuery = _context.Stock_Movements
                                .Include(m => m.Inventory.Product)
                                .Include(m => m.User)
                                .Include(m => m.Supplier)
                                .Include(m => m.MoveType)
                                .Where(m => m.IsActive);

                            if (command.Filters != null)
                            {
                                if (!string.IsNullOrEmpty(command.Filters.DateRange) && command.Filters.DateRange == "today")
                                    movementsQuery = movementsQuery.Where(m => m.CreatedAt.Date == DateTime.UtcNow.Date);
                                
                                if (!string.IsNullOrEmpty(command.Filters.UserName))
                                    movementsQuery = movementsQuery.Where(m => (m.User.FirstName + " " + m.User.LastName).ToLower().Contains(command.Filters.UserName.ToLower()));

                                if (!string.IsNullOrEmpty(command.Filters.SupplierName))
                                    movementsQuery = movementsQuery.Where(m => m.Supplier.SupplierName.ToLower().Contains(command.Filters.SupplierName.ToLower()));

                                if (!string.IsNullOrEmpty(command.Filters.MoveTypeName))
                                    movementsQuery = movementsQuery.Where(m => m.MoveType.MoveType.ToLower().Contains(command.Filters.MoveTypeName.ToLower()));
                            }

                            var movements = await movementsQuery.OrderByDescending(m => m.CreatedAt).Take(command.Filters?.Take ?? 10).ToListAsync();
                            
                            var movementResponse = movements
                                .Select(m => $"{(m.MoveType.MoveType.Contains("Giriş") ? "📥" : "📤")} **{m.Inventory.Product.ProductName}**: {m.Quantity} Adet, {m.CreatedAt:dd.MM HH:mm}");

                            responseText = movements.Any()
                                ? $"📋 **Son Stok Hareketleri**:\n- {string.Join("\n- ", movementResponse)}"
                                : "Stok hareketi bulunamadı.";
                            break;
                        
                        // 4. TEDARİKÇİLER
                        case "supplier":
                             var suppliers = await _mediator.Send(new GetSuppliersQuery());
                             var supplierResponse = suppliers.Take(10).Select(s => $"**{s.SupplierName}** (Yetkili: {s.ContactPerson ?? "N/A"})");
                             responseText = suppliers.Any()
                                ? $"🏢 **Tedarikçiler**:\n- {string.Join("\n- ", supplierResponse)}"
                                : "Tedarikçi bulunamadı.";
                            break;

                        // 5. TESLİMAT KURALLARI
                        case "delivery_rule":
                            IQueryable<Delivery_Rules> rulesQuery = _context.Delivery_Rules.Include(r => r.Supplier);
                            
                            if (command.Filters != null)
                            {
                                if (!string.IsNullOrEmpty(command.Filters.SupplierName))
                                    rulesQuery = rulesQuery.Where(r => r.Supplier.SupplierName.ToLower().Contains(command.Filters.SupplierName.ToLower()));
                                
                                // Burada 'frequency' filtresi de eklenebilir. Örnek:
                                // if (!string.IsNullOrEmpty(command.Filters.Frequency))
                                //    rulesQuery = rulesQuery.Where(r => r.Frequency.ToString().ToLower() == command.Filters.Frequency.ToLower()));
                            }

                            var rules = await rulesQuery.ToListAsync();

                            var rulesResponse = rules.Select(r => {
                                string freq = r.Frequency == Delivery_Rules.FrequencyType.Weekly ? "Haftalık" : "Aylık";
                                return $"**{r.Supplier.SupplierName} - {r.RuleName}**: {freq}, her {r.Interval} periyotta bir, saat {r.ArrivalTime:hh\\:mm}'da.";
                            });

                            responseText = rules.Any()
                                ? $"📅 **Teslimat Kuralları**:\n- {string.Join("\n- ", rulesResponse)}"
                                : "Teslimat kuralı bulunamadı.";
                            break;

                        default:
                            responseText = $"'{command.Entity}' için listeleme (GET) işlemi henüz tanımlanmamış.";
                            break;
                    }
                }
                // -------------------------------------------------------------------------
                // 4. UPDATE İŞLEMLERİ
                // -------------------------------------------------------------------------
                else if (op == "update")
                {
                    if (!command.Payload.HasValue)
                        return Ok(new { response = "Güncellenecek bilgiyi (payload) belirtmediniz." });

                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());

                    switch (entity)
                    {
                        case "inventory":
                            IQueryable<Inventories> inventoriesToUpdate = _context.Inventories.Include(i => i.Product).ThenInclude(p => p.Category);

                            // Filtreleme
                            if (command.Filters != null)
                            {
                                if (!string.IsNullOrEmpty(command.Filters.ProductName))
                                    inventoriesToUpdate = inventoriesToUpdate.Where(i => i.Product.ProductName.ToLower().Contains(command.Filters.ProductName.ToLower()));
                                if (!string.IsNullOrEmpty(command.Filters.CategoryName))
                                    inventoriesToUpdate = inventoriesToUpdate.Where(i => i.Product.Category.CategoryName.ToLower().Contains(command.Filters.CategoryName.ToLower()));
                            }

                            var inventoryList = await inventoriesToUpdate.ToListAsync();
                            if (!inventoryList.Any())
                            {
                                responseText = "Güncellenecek envanter kaydı bulunamadı.";
                                break;
                            }

                            // Toplu Güncelleme Mantığı
                            foreach (var inventory in inventoryList)
                            {
                                if (payload.TryGetValue("salePrice", out var salePrice)) inventory.SalePrice = salePrice.GetSingle();
                                if (payload.TryGetValue("purchasePrice", out var purchasePrice)) inventory.PurchasePrice = purchasePrice.GetSingle();
                                if (payload.TryGetValue("criticalStockQuantity", out var criticalStock)) inventory.CriticalStockQuantity = criticalStock.GetInt32();
                                if (payload.TryGetValue("quantity", out var quantity)) inventory.Quantity = quantity.GetInt32(); // Stok sayımı düzeltme

                                // Yüzdesel artış/azalış
                                if (payload.TryGetValue("increasePercent", out var incPercent) && payload.TryGetValue("field", out var incField))
                                {
                                    if (incField.GetString() == "salePrice") inventory.SalePrice *= (1 + incPercent.GetSingle() / 100);
                                }
                                if (payload.TryGetValue("decreasePercent", out var decPercent) && payload.TryGetValue("field", out var decField))
                                {
                                     if (decField.GetString() == "salePrice") inventory.SalePrice *= (1 - decPercent.GetSingle() / 100);
                                }
                            }
                            
                            await _context.SaveChangesAsync();
                            responseText = $"✅ {inventoryList.Count} adet envanter kaydı başarıyla güncellendi.";
                            break;

                        default:
                            responseText = $"'{entity}' için güncelleme işlemi henüz tanımlanmamış.";
                            break;

                        // Product güncelleme - Senaryo 53, 57, 58, 63, 66, 68
                        case "product":
                            IQueryable<Products> productsToUpdate = _context.Products.Include(p => p.Category);

                            if (command.Filters != null)
                            {
                                if (!string.IsNullOrEmpty(command.Filters.Name))
                                    productsToUpdate = productsToUpdate.Where(p => p.ProductName.ToLower().Contains(command.Filters.Name.ToLower()));
                            }

                            var productList = await productsToUpdate.ToListAsync();
                            if (!productList.Any())
                            {
                                responseText = "Güncellenecek ürün bulunamadı.";
                                break;
                            }

                            foreach (var product in productList)
                            {
                                if (payload.TryGetValue("productName", out var pName))
                                    product.ProductName = pName.GetString();

                                if (payload.TryGetValue("description", out var desc))
                                    product.Description = desc.GetString();

                                if (payload.TryGetValue("isActive", out var isActive))
                                    product.IsActive = isActive.GetBoolean();

                                if (payload.TryGetValue("categoryName", out var catName))
                                {
                                    var category = await _context.Categories
                                        .FirstOrDefaultAsync(c => c.CategoryName.ToLower().Contains(catName.GetString().ToLower()));
                                    if (category != null)
                                        product.CategoryId = category.Id;
                                }
                            }

                            await _context.SaveChangesAsync();
                            responseText = $"✅ {productList.Count} adet ürün başarıyla güncellendi.";
                            break;

                        // Supplier güncelleme - Senaryo 54, 64, 73
                        case "supplier":
                            IQueryable<Suppliers> suppliersToUpdate = _context.Suppliers;

                            if (command.Filters != null)
                            {
                                if (!string.IsNullOrEmpty(command.Filters.Name))
                                    suppliersToUpdate = suppliersToUpdate.Where(s => s.SupplierName.ToLower().Contains(command.Filters.Name.ToLower()));

                                if (command.Filters.IsActive.HasValue)
                                    suppliersToUpdate = suppliersToUpdate.Where(s => s.IsActive == command.Filters.IsActive.Value);
                            }

                            var supplierList = await suppliersToUpdate.ToListAsync();
                            if (!supplierList.Any())
                            {
                                responseText = "Güncellenecek tedarikçi bulunamadı.";
                                break;
                            }

                            foreach (var supplier in supplierList)
                            {
                                if (payload.TryGetValue("contactPerson", out var contact))
                                    supplier.ContactPerson = contact.GetString();

                                if (payload.TryGetValue("phoneNumber", out var phone))
                                    supplier.PhoneNumber = phone.GetString();

                                if (payload.TryGetValue("email", out var email))
                                    supplier.Email = email.GetString();

                                if (payload.TryGetValue("isActive", out var isActive))
                                    supplier.IsActive = isActive.GetBoolean();
                            }

                            await _context.SaveChangesAsync();
                            responseText = $"✅ {supplierList.Count} adet tedarikçi başarıyla güncellendi.";
                            break;

                        // Category güncelleme - Senaryo 55
                        case "category":
                            IQueryable<Categories> categoriesToUpdate = _context.Categories;

                            if (command.Filters != null && !string.IsNullOrEmpty(command.Filters.Name))
                                categoriesToUpdate = categoriesToUpdate.Where(c => c.CategoryName.ToLower().Contains(command.Filters.Name.ToLower()));

                            var categoryList = await categoriesToUpdate.ToListAsync();
                            if (!categoryList.Any())
                            {
                                responseText = "Güncellenecek kategori bulunamadı.";
                                break;
                            }

                            foreach (var category in categoryList)
                            {
                                if (payload.TryGetValue("categoryName", out var catName))
                                    category.CategoryName = catName.GetString();

                                if (payload.TryGetValue("description", out var desc))
                                    category.Description = desc.GetString();
                            }

                            await _context.SaveChangesAsync();
                            responseText = $"✅ {categoryList.Count} adet kategori başarıyla güncellendi.";
                            break;

                        // Delivery_Rule güncelleme - Senaryo 56, 65, 69, 74
                        case "delivery_rule":
                            IQueryable<Delivery_Rules> rulesToUpdate = _context.Delivery_Rules.Include(r => r.Supplier);

                            if (command.Filters != null)
                            {
                                if (!string.IsNullOrEmpty(command.Filters.Id))
                                    rulesToUpdate = rulesToUpdate.Where(r => r.Id.ToString() == command.Filters.Id);

                                if (!string.IsNullOrEmpty(command.Filters.Name))
                                    rulesToUpdate = rulesToUpdate.Where(r => r.RuleName.ToLower().Contains(command.Filters.Name.ToLower()));
                            }

                            var ruleList = await rulesToUpdate.ToListAsync();
                            if (!ruleList.Any())
                            {
                                responseText = "Güncellenecek teslimat kuralı bulunamadı.";
                                break;
                            }

                            foreach (var rule in ruleList)
                            {
                                if (payload.TryGetValue("arrivalTime", out var time))
                                    rule.ArrivalTime = TimeSpan.Parse(time.GetString());

                                if (payload.TryGetValue("interval", out var interval))
                                    rule.Interval = interval.GetInt32();

                                if (payload.TryGetValue("isActive", out var isActive))
                                    rule.IsActive = isActive.GetBoolean();
                            }

                            await _context.SaveChangesAsync();
                            responseText = $"✅ {ruleList.Count} adet teslimat kuralı güncellendi.";
                            break;

                        // Unit_Type güncelleme - Senaryo 60
                        case "unit_type":
                            IQueryable<Unit_Types> unitsToUpdate = _context.Unit_Types;

                            if (command.Filters != null && !string.IsNullOrEmpty(command.Filters.Name))
                                unitsToUpdate = unitsToUpdate.Where(u => u.UnitName.ToLower().Contains(command.Filters.Name.ToLower()));

                            var unitList = await unitsToUpdate.ToListAsync();
                            if (!unitList.Any())
                            {
                                responseText = "Güncellenecek birim tipi bulunamadı.";
                                break;
                            }

                            foreach (var unit in unitList)
                            {
                                if (payload.TryGetValue("unitName", out var unitName))
                                    unit.UnitName = unitName.GetString();
                            }

                            await _context.SaveChangesAsync();
                            responseText = $"✅ {unitList.Count} adet birim tipi güncellendi.";
                            break;


                    }
                }
                // -------------------------------------------------------------------------
                // 5. DELETE İŞLEMLERİ
                // -------------------------------------------------------------------------
                else if (op == "delete")
                {
                     if (command.Filters == null) {
                        responseText = "Silme işlemi için filtre belirtmelisiniz.";
                        
                    }

                    switch(entity)
                    {
                        case "supplier":
                            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.ToLower().Contains(command.Filters.Name.ToLower()));
                            if(supplier == null) {
                                responseText = $"'{command.Filters.Name}' adında bir tedarikçi bulunamadı.";
                                break;
                            }
                            supplier.IsActive = false; // Soft delete
                            await _context.SaveChangesAsync();
                            responseText = $"✅ '{supplier.SupplierName}' tedarikçisi silindi (pasif olarak ayarlandı).";
                            break;
                        
                        case "inventory":
                             IQueryable<Inventories> inventoriesToDelete = _context.Inventories;
                             if(!string.IsNullOrEmpty(command.Filters.ProductName))
                                inventoriesToDelete = inventoriesToDelete.Where(i => i.Product.ProductName.ToLower().Contains(command.Filters.ProductName.ToLower()));
                            
                             if (command.Filters.ExpirationDate == "expired")
                                inventoriesToDelete = inventoriesToDelete.Where(i => i.ExpirationDate < DateTime.UtcNow);
                            
                            var deletedCount = await inventoriesToDelete.ExecuteUpdateAsync(s => s.SetProperty(i => i.IsActive, false)); // Toplu Soft Delete
                            responseText = $"✅ {deletedCount} adet envanter kaydı silindi (pasif yapıldı).";
                            break;

                        default:
                            responseText = $"'{entity}' için silme işlemi henüz tanımlanmamış.";
                            break;

                        // Category silme - Senaryo 77, 81
                        case "category":
                            var categoryToDelete = await _context.Categories
                                .FirstOrDefaultAsync(c => c.CategoryName.ToLower().Contains(command.Filters.Name.ToLower()));

                            if (categoryToDelete == null)
                            {
                                responseText = $"'{command.Filters.Name}' kategorisi bulunamadı.";
                                break;
                            }

                            categoryToDelete.IsActive = false;
                            await _context.SaveChangesAsync();
                            responseText = $"✅ '{categoryToDelete.CategoryName}' kategorisi silindi.";
                            break;

                        // Delivery_Rule silme - Senaryo 79
                        case "delivery_rule":
                            Delivery_Rules ruleToDelete = null;

                            if (!string.IsNullOrEmpty(command.Filters.Id))
                            {
                                ruleToDelete = await _context.Delivery_Rules
                                    .FirstOrDefaultAsync(r => r.Id.ToString() == command.Filters.Id);
                            }

                            if (ruleToDelete == null)
                            {
                                responseText = "Silinecek teslimat kuralı bulunamadı.";
                                break;
                            }

                            ruleToDelete.IsActive = false;
                            await _context.SaveChangesAsync();
                            responseText = $"✅ Teslimat kuralı silindi.";
                            break;

                        // Product silme - Senaryo 84
                        case "product":
                            IQueryable<Products> productsToDelete = _context.Products;

                            if (command.Filters != null && !string.IsNullOrEmpty(command.Filters.Name))
                            {
                                if (command.Filters.Name.Contains("StartsWith:"))
                                {
                                    var prefix = command.Filters.Name.Replace("StartsWith:", "").ToLower();
                                    productsToDelete = productsToDelete.Where(p => p.ProductName.ToLower().StartsWith(prefix));
                                }
                                else
                                {
                                    productsToDelete = productsToDelete.Where(p => p.ProductName.ToLower().Contains(command.Filters.Name.ToLower()));
                                }
                            }

                            var deletedProductCount = await productsToDelete.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
                            responseText = $"✅ {deletedProductCount} adet ürün silindi.";
                            break;

                        // Unit_Type silme - Senaryo 85
                        case "unit_type":
                            IQueryable<Unit_Types> unitsToDelete = _context.Unit_Types;

                            if (command.Filters != null && !string.IsNullOrEmpty(command.Filters.Name))
                                unitsToDelete = unitsToDelete.Where(u => u.UnitName.ToLower().Contains(command.Filters.Name.ToLower()));

                            var deletedUnitCount = await unitsToDelete.ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));
                            responseText = $"✅ {deletedUnitCount} adet birim tipi silindi.";
                            break;
                    }
                }
                else
                {
                    responseText = $"Komut ({command.Operation}) işlendi ancak detaylı mantık henüz eklenmedi.";
                }
            }
            catch (Exception ex)
            {
                responseText = $"İşlem sırasında bir hata oluştu: {ex.Message}";
            }

            return Ok(new { response = responseText });
        }

        [HttpPost("clear-session")]
        public IActionResult ClearSession([FromBody] ClearSessionRequest request)
        {
            if (_sessions.ContainsKey(request.SessionId))
            {
                _sessions.Remove(request.SessionId);
                return Ok(new { message = "Session temizlendi." });
            }
            return NotFound(new { message = "Session bulunamadı." });
        }
    }
}