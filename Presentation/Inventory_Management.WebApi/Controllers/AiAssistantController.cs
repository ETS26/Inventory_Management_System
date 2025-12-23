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
using Inventory_Management.Application.Features.Queries.Delivery_RulesQuery;
using Inventory_Management.Application.Features.Commands.Delivery_RulesCommand;
using Inventory_Management.Application.Features.Commands.Unit_TypesCommand;
using Inventory_Management.Domain.Common;
using Inventory_Management.Application.Features.Queries.SuppliersQuery;

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
        [JsonPropertyName("ruleName")] public string RuleName { get; set; }
        [JsonPropertyName("moveTypeName")] public string MoveTypeName { get; set; }
        [JsonPropertyName("moveTypeId")] public string MoveTypeId { get; set; }
        [JsonPropertyName("inventoryId")] public string InventoryId { get; set; }
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
3. 'ekle', 'yeni', 'oluştur', 'girişi yap', 'çıkışı yap' -> POST
4. 'güncelle', 'değiştir', 'fiyatını yap', 'adını yap' -> UPDATE
5. 'sil', 'kaldır', 'pasif et' -> DELETE
6. 'stock in', 'giriş', 'alım', 'geldi' -> 'stock in' ; 'stock out', 'çıkış', 'satış', 'gitti' -> 'stock out' olarak algıla. Veritabanında SADECE 'stock in' ve 'stock out' var.
7. ÖNEMLİ: Eğer kullanıcı virgülle ayrılmış (CSV benzeri) bir formatta veri girerse (Örn: 'Ürün,Fiyat,Miktar...') ASLA soru sorma. Eksik bilgi olsa bile tahmin et ve JSON üret.
8. TOPLU İŞLEM: Eğer kullanıcı '+' işaretiyle ayrılmış birden fazla kayıt girerse (Örn: 'A,B,C + X,Y,Z'), payload içinde 'items' dizisi oluştur.
9. RENK DÖNÜŞÜMÜ: Kullanıcı renk ismi verirse (mavi, kırmızı, yeşil, sarı, turuncu, mor, gri, siyah, beyaz vb.) bunu mutlaka HEX koduna çevir. (Örn: 'mavi' -> '#0000FF', 'kırmızı' -> '#FF0000', 'yeşil' -> '#008000', 'sarı' -> '#FFFF00', 'turuncu' -> '#FFA500').

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
36. ""Kablosuz Klavye,869000000784,Elektronik,UNIT"" -> { ""operation"": ""POST"", ""entity"": ""Product"", ""payload"": { ""productName"": ""Kablosuz Klavye"", ""barcode"": ""869000000784"", ""categoryName"": ""Elektronik"", ""unitTypeName"": ""UNIT"" } }
37. ""Kablosuz Klavye,869000000784,Elektronik,UNIT,https://imageURL,ürün çok güzel"" -> { ""operation"": ""POST"", ""entity"": ""Product"", ""payload"": { ""productName"": ""Kablosuz Klavye"", ""barcode"": ""869000000784"", ""categoryName"": ""Elektronik"", ""unitTypeName"": ""UNIT"", ""imageURL"": ""https://imageURL"", ""description"": ""ürün çok güzel"" } }
38. ""Kırmızı mercimek,Yusuf Gıda,stock in,5"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""Kırmızı mercimek"", ""supplierName"": ""Yusuf Gıda"", ""moveTypeName"": ""stock in"", ""quantity"": 5 } }
39. ""Kırmızı mercimek,Yusuf Gıda,stock in,5,kaliteli ürün"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""Kırmızı mercimek"", ""supplierName"": ""Yusuf Gıda"", ""moveTypeName"": ""stock in"", ""quantity"": 5, ""description"": ""kaliteli ürün"" } }
40. ""Kıyma 1KG,150,300,15,31.12.2025,Yusuf Gıda,stock in,30"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""Kıyma 1KG"", ""purchasePrice"": 150, ""salePrice"": 300, ""criticalStockQuantity"": 15, ""expirationDate"": ""31.12.2025"", ""supplierName"": ""Yusuf Gıda"", ""moveTypeName"": ""stock in"", ""quantity"": 30 } }
41. ""Patates 1KG x 10,20,50,30,15.01.2026,Yusuf Gıda,stock in,50"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""Patates 1KG x 10"", ""purchasePrice"": 20, ""salePrice"": 50, ""criticalStockQuantity"": 30, ""expirationDate"": ""15.01.2026"", ""supplierName"": ""Yusuf Gıda"", ""moveTypeName"": ""stock in"", ""quantity"": 50 } }
42. ""Kıyma 1KG,150,300,15,432436-08,31.12.2025,Yusuf Gıda,stock in,30,kaliteli kıyma"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""Kıyma 1KG"", ""purchasePrice"": 150, ""salePrice"": 300, ""criticalStockQuantity"": 15, ""batchNumber"": ""432436-08"", ""expirationDate"": ""31.12.2025"", ""supplierName"": ""Yusuf Gıda"", ""moveTypeName"": ""stock in"", ""quantity"": 30, ""description"": ""kaliteli kıyma"" } }
43. ""Yeni birim tipi ekle: Kasa"" -> { ""operation"": ""POST"", ""entity"": ""Unit_Type"", ""payload"": { ""unitName"": ""Kasa"" } }
44. ""'Test' adında yeni kategori oluştur"" -> { ""operation"": ""POST"", ""entity"": ""Category"", ""payload"": { ""categoryName"": ""Test"" } }
45. ""Klavye,123,Elek,Adet + Mouse,456,Elek,Adet"" -> { ""operation"": ""POST"", ""entity"": ""Product"", ""payload"": { ""items"": [ { ""productName"": ""Klavye"", ""barcode"": ""123"", ""categoryName"": ""Elek"", ""unitTypeName"": ""Adet"" }, { ""productName"": ""Mouse"", ""barcode"": ""456"", ""categoryName"": ""Elek"", ""unitTypeName"": ""Adet"" } ] } }
46. ""Un,Yusuf,stock in,5 + Şeker,Yusuf,stock in,10"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""items"": [ { ""productName"": ""Un"", ""supplierName"": ""Yusuf"", ""moveTypeName"": ""stock in"", ""quantity"": 5 }, { ""productName"": ""Şeker"", ""supplierName"": ""Yusuf"", ""moveTypeName"": ""stock in"", ""quantity"": 10 } ] } }
47. ""Mehmet Manav,Mehmet Yılmaz,5536860912,abc@gmail.com,bucak/burdur + Ceylan Pastanesi,Ceylan Yıldırım,5556321247,cba@gmail.com,ankara"" -> { ""operation"": ""POST"", ""entity"": ""Supplier"", ""payload"": { ""items"": [ { ""supplierName"": ""Mehmet Manav"", ""contactPerson"": ""Mehmet Yılmaz"", ""phoneNumber"": ""5536860912"", ""email"": ""abc@gmail.com"", ""address"": ""bucak/burdur"" }, { ""supplierName"": ""Ceylan Pastanesi"", ""contactPerson"": ""Ceylan Yıldırım"", ""phoneNumber"": ""5556321247"", ""email"": ""cba@gmail.com"", ""address"": ""ankara"" } ] } }
48. ""kasa+çuval"" -> { ""operation"": ""POST"", ""entity"": ""Unit_Type"", ""payload"": { ""items"": [ { ""unitName"": ""kasa"" }, { ""unitName"": ""çuval"" } ] } }
49. ""Yusuf Gıda,Ekmek Teslimatı,01.01.2026,30.04.2026,08.30,haftalık,2,2,1;3;5,mavi + Yusuf Gıda,Peynir Teslimatı,01.01.2026,30.04.2026,08.30,haftalık,2,3,2;4;6,yeşil"" -> { ""operation"": ""POST"", ""entity"": ""Delivery_Rule"", ""payload"": { ""items"": [ { ""supplierName"": ""Yusuf Gıda"", ""ruleName"": ""Ekmek Teslimatı"", ""startDate"": ""01.01.2026"", ""endDate"": ""30.04.2026"", ""arrivalTime"": ""08.30"", ""frequency"": ""Weekly"", ""interval"": 2, ""leadTimeDays"": 2, ""daysOfWeek"": ""1,3,5"", ""calendarColor"": ""#0000FF"" }, { ""supplierName"": ""Yusuf Gıda"", ""ruleName"": ""Peynir Teslimatı"", ""startDate"": ""01.01.2026"", ""endDate"": ""30.04.2026"", ""arrivalTime"": ""08.30"", ""frequency"": ""Weekly"", ""interval"": 2, ""leadTimeDays"": 3, ""daysOfWeek"": ""2,4,6"", ""calendarColor"": ""#008000"" } ] } }
50. ""Sayım fazlası olarak 2 adet klavye girişi yap"" -> { ""operation"": ""POST"", ""entity"": ""Stock_Movement"", ""payload"": { ""productName"": ""klavye"", ""quantity"": 2, ""moveTypeName"": ""Sayım Fazlası"" } }

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
75a. ""Kola 20 TL, Fanta 25 TL olsun"" -> { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""payload"": { ""items"": [ { ""productName"": ""Kola"", ""salePrice"": 20 }, { ""productName"": ""Fanta"", ""salePrice"": 25 } ] } }
75b. ""Ahmet 5551234567, Mehmet 5557654321 olsun"" -> { ""operation"": ""UPDATE"", ""entity"": ""Supplier"", ""payload"": { ""items"": [ { ""supplierName"": ""Ahmet"", ""phoneNumber"": ""5551234567"" }, { ""supplierName"": ""Mehmet"", ""phoneNumber"": ""5557654321"" } ] } }
75c. ""ID'si 123 olan hareketi güncelle: adet 50 olsun"" -> { ""operation"": ""UPDATE"", ""entity"": ""Stock_Movement"", ""filters"": { ""id"": ""123"" }, ""payload"": { ""quantity"": 50 } }
75d. ""Domatesin birim tipini Kilo yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""productName"": ""Domates"" }, ""payload"": { ""unitTypeName"": ""Kilo"" } }
75e. ""'Adet' birim ismini 'Tane' olarak değiştir"" -> { ""operation"": ""UPDATE"", ""entity"": ""Unit_Type"", ""filters"": { ""name"": ""Adet"" }, ""payload"": { ""unitName"": ""Tane"" } }
75f. ""Marul kategorisini Bakliyat yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""productName"": ""Marul"" }, ""payload"": { ""categoryName"": ""Bakliyat"" } }
75g. ""Bakliyat kategorisinin adını Kuru Gıda yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Category"", ""filters"": { ""name"": ""Bakliyat"" }, ""payload"": { ""categoryName"": ""Kuru Gıda"" } }
75h. ""414903e3-de77-48bf-8c5e-725422518caf işlemini stock in yap"" -> { ""operation"": ""UPDATE"", ""entity"": ""Stock_Movement"", ""filters"": { ""id"": ""414903e3-de77-48bf-8c5e-725422518caf"" }, ""payload"": { ""moveTypeName"": ""stock in"" } }

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
85a. ""Eski Masa ve Kırık Sandalye ürünlerini sil"" -> { ""operation"": ""DELETE"", ""entity"": ""Product"", ""payload"": { ""items"": [ { ""productName"": ""Eski Masa"" }, { ""productName"": ""Kırık Sandalye"" } ] } }
85b. ""Hatalı girişi sil (ID: 999)"" -> { ""operation"": ""DELETE"", ""entity"": ""Stock_Movement"", ""filters"": { ""id"": ""999"" } }

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
                                var root = command.Payload.Value;
                                var items = new List<JsonElement>();

                                if (root.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in itemsElement.EnumerateArray()) items.Add(item);
                                }
                                else
                                {
                                    items.Add(root);
                                }

                                var sb = new StringBuilder();
                                int successCount = 0;

                                foreach (var item in items)
                                {
                                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText());
                                    string productName = payload.ContainsKey("productName") ? payload["productName"].GetString() : null;
                                    string barcode = payload.ContainsKey("barcode") ? payload["barcode"].GetString() : null;
                                    string categoryName = payload.ContainsKey("categoryName") ? payload["categoryName"].GetString() : null;
                                    string unitTypeName = payload.ContainsKey("unitTypeName") ? payload["unitTypeName"].GetString() : null;
                                    string imageUrl = payload.ContainsKey("imageURL") ? payload["imageURL"].GetString() : null;
                                    string description = payload.ContainsKey("description") ? payload["description"].GetString() : null;

                                    if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(barcode))
                                    {
                                        sb.AppendLine($"❌ Hata: Ürün adı ve barkod zorunludur. (Gelen: {productName})");
                                        continue;
                                    }

                                    Guid categoryId = Guid.Empty;
                                    Guid unitTypeId = Guid.Empty;

                                    if (!string.IsNullOrWhiteSpace(categoryName))
                                    {
                                        var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName.ToLower() == categoryName.ToLower());
                                        if (category == null)
                                        {
                                            category = new Categories { Id = Guid.NewGuid(), CategoryName = categoryName, IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(3) };
                                            _context.Categories.Add(category);
                                            await _context.SaveChangesAsync();
                                        }
                                        categoryId = category.Id;
                                    }

                                    if (!string.IsNullOrWhiteSpace(unitTypeName))
                                    {
                                        var unitType = await _context.Unit_Types.FirstOrDefaultAsync(u => u.UnitName.ToLower() == unitTypeName.ToLower());
                                        if (unitType == null)
                                        {
                                            unitType = new Unit_Types { Id = Guid.NewGuid(), UnitName = unitTypeName, IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(3) };
                                            _context.Unit_Types.Add(unitType);
                                            await _context.SaveChangesAsync();
                                        }
                                        unitTypeId = unitType.Id;
                                    }

                                    var createCommand = new CreateProductsCommand
                                    {
                                        ProductName = productName,
                                        Barcode = barcode,
                                        CategoryId = categoryId,
                                        UnitTypeId = unitTypeId,
                                        ImageURL = imageUrl,
                                        Description = description
                                    };
                                    await _mediator.Send(createCommand);
                                    successCount++;
                                    sb.AppendLine($"✅ '{productName}' eklendi.");
                                }

                                session.CollectedData.Clear();
                                responseText = items.Count > 1
                                    ? $"Toplu Ürün Ekleme Sonucu:\n{sb}"
                                    : sb.ToString().Trim();
                                
                                if (successCount == 0 && items.Count > 0 && string.IsNullOrEmpty(responseText))
                                     responseText = "Hiçbir ürün eklenemedi.";
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
                                var root = command.Payload.Value;
                                var items = new List<JsonElement>();

                                if (root.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in itemsElement.EnumerateArray()) items.Add(item);
                                }
                                else
                                {
                                    items.Add(root);
                                }

                                var sb = new StringBuilder();
                                int successCount = 0;

                                foreach (var item in items)
                                {
                                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText());
                                    string unitName = payload.ContainsKey("unitName") ? payload["unitName"].GetString() : null;

                                    if (string.IsNullOrWhiteSpace(unitName))
                                    {
                                        sb.AppendLine("❌ Hata: Birim tipi adı gerekli.");
                                        continue;
                                    }

                                    // Check duplication if needed, but simple add for now
                                    var unitType = new Unit_Types
                                    {
                                        Id = Guid.NewGuid(),
                                        UnitName = unitName,
                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    _context.Unit_Types.Add(unitType);
                                    await _context.SaveChangesAsync();
                                    successCount++;
                                    sb.AppendLine($"✅ '{unitName}' eklendi.");
                                }
                                
                                responseText = items.Count > 1 
                                    ? $"Toplu Birim Ekleme Sonucu:\n{sb}" 
                                    : sb.ToString().Trim();

                                if (successCount == 0 && items.Count > 0 && string.IsNullOrEmpty(responseText))
                                     responseText = "Hiçbir birim tipi eklenemedi.";
                            }
                            break;

                        // Supplier (Tedarikçi) ekleme - Senaryo 42
                        case "supplier":
                            if (command.Payload.HasValue)
                            {
                                var root = command.Payload.Value;
                                var items = new List<JsonElement>();

                                if (root.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in itemsElement.EnumerateArray()) items.Add(item);
                                }
                                else
                                {
                                    items.Add(root);
                                }

                                var sb = new StringBuilder();
                                int successCount = 0;

                                foreach (var item in items)
                                {
                                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText());

                                    string supplierName = payload.ContainsKey("supplierName") ? payload["supplierName"].GetString() : null;
                                    string contactPerson = payload.ContainsKey("contactPerson") ? payload["contactPerson"].GetString() : null;
                                    string email = payload.ContainsKey("email") ? payload["email"].GetString() : null;
                                    string phoneNumber = payload.ContainsKey("phoneNumber") ? payload["phoneNumber"].GetString() : null;
                                    string address = payload.ContainsKey("address") ? payload["address"].GetString() : null;

                                    if (string.IsNullOrWhiteSpace(supplierName))
                                    {
                                        sb.AppendLine("❌ Hata: Tedarikçi adı gerekli.");
                                        continue;
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
                                    successCount++;
                                    sb.AppendLine($"✅ '{supplierName}' eklendi.");
                                }

                                responseText = items.Count > 1 
                                    ? $"Toplu Tedarikçi Ekleme Sonucu:\n{sb}" 
                                    : sb.ToString().Trim();

                                if (successCount == 0 && items.Count > 0 && string.IsNullOrEmpty(responseText))
                                     responseText = "Hiçbir tedarikçi eklenemedi.";
                            }
                            break;

                        // Stock_Movement (Stok Hareketi) ekleme
                        case "stock_movement":
                            if (command.Payload.HasValue)
                            {
                                var root = command.Payload.Value;
                                var items = new List<JsonElement>();

                                if (root.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in itemsElement.EnumerateArray()) items.Add(item);
                                }
                                else
                                {
                                    items.Add(root);
                                }

                                var sb = new StringBuilder();
                                int successCount = 0;

                                foreach (var item in items)
                                {
                                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText());

                                    string productName = payload.ContainsKey("productName") ? payload["productName"].GetString() : null;
                                    int quantity = payload.ContainsKey("quantity") ? payload["quantity"].GetInt32() : 0;
                                    string moveTypeName = payload.ContainsKey("moveTypeName") ? payload["moveTypeName"].GetString() : "Giriş";
                                    string supplierName = payload.ContainsKey("supplierName") ? payload["supplierName"].GetString() : null;
                                    float? purchasePrice = payload.ContainsKey("purchasePrice") ? payload["purchasePrice"].GetSingle() : null;
                                    float? salePrice = payload.ContainsKey("salePrice") ? payload["salePrice"].GetSingle() : null;
                                    int? criticalStockQuantity = payload.ContainsKey("criticalStockQuantity") ? payload["criticalStockQuantity"].GetInt32() : null;
                                    string expirationDateStr = payload.ContainsKey("expirationDate") ? payload["expirationDate"].GetString() : null;
                                    string batchNumber = payload.ContainsKey("batchNumber") ? payload["batchNumber"].GetString() : null;
                                    string description = payload.ContainsKey("description") ? payload["description"].GetString() : null;

                                    if (string.IsNullOrWhiteSpace(productName) || quantity <= 0)
                                    {
                                        sb.AppendLine($"❌ Hata: Ürün adı ve miktar gerekli (Miktar pozitif olmalı). Gelen Ürün: {productName}");
                                        continue;
                                    }

                                    // Ürünü bul
                                    var product = await _context.Products
                                        .FirstOrDefaultAsync(p => p.ProductName.ToLower().Contains(productName.ToLower()) && p.IsActive);

                                    if (product == null)
                                    {
                                        sb.AppendLine($"❌ Hata: '{productName}' ürünü bulunamadı. Önce ürünü sisteme ekleyin.");
                                        continue;
                                    }

                                    // Envanter kaydını bul veya oluştur
                                    var inventory = await _context.Inventories
                                        .FirstOrDefaultAsync(i => i.ProductId == product.Id && i.IsActive);

                                    bool isNewInventory = false;
                                    if (inventory == null)
                                    {
                                        // Yeni envanter oluşturma kontrolü
                                        if (purchasePrice.HasValue && salePrice.HasValue)
                                        {
                                            DateTime expirationDate = DateTime.UtcNow.AddYears(1);
                                            if (!string.IsNullOrWhiteSpace(expirationDateStr) && DateTime.TryParseExact(expirationDateStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                                            {
                                                expirationDate = parsedDate;
                                            }

                                            inventory = new Inventories
                                            {
                                                Id = Guid.NewGuid(),
                                                ProductId = product.Id,
                                                CompanyId = _currentUserService.CompanyId ?? Guid.Empty, // CompanyId kontrolü
                                                Quantity = 0, // Hareket ile artacak
                                                CriticalStockQuantity = criticalStockQuantity ?? 10,
                                                PurchasePrice = purchasePrice.Value,
                                                SalePrice = salePrice.Value,
                                                ExpirationDate = expirationDate,
                                                BatchNumber = batchNumber ?? "STD-" + DateTime.Now.ToString("yyyyMMdd"),
                                                IsActive = true,
                                                CreatedAt = DateTime.UtcNow
                                            };
                                            _context.Inventories.Add(inventory);
                                            await _context.SaveChangesAsync();
                                            isNewInventory = true;
                                        }
                                        else
                                        {
                                            sb.AppendLine($"❌ Hata: '{productName}' için envanter kaydı yok. Yeni kayıt için Alış ve Satış fiyatı zorunludur.");
                                            continue;
                                        }
                                    }

                                    // MoveType bul ve Normalize Et
                                    string targetMoveType = "stock in"; // Varsayılan
                                    string lowerInput = moveTypeName.ToLower();

                                    if (lowerInput.Contains("out") || lowerInput.Contains("çıkış") || lowerInput.Contains("satış"))
                                    {
                                        targetMoveType = "stock out";
                                    }
                                    else
                                    {
                                        targetMoveType = "stock in";
                                    }

                                    var moveType = await _context.Move_Types
                                        .FirstOrDefaultAsync(m => m.MoveType.ToLower() == targetMoveType);

                                    if (moveType == null)
                                    {
                                        sb.AppendLine($"❌ Hata: '{targetMoveType}' hareket tipi sistemde bulunamadı.");
                                        continue;
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
                                        CompanyId = _currentUserService.CompanyId ?? Guid.Empty,
                                        Quantity = quantity,
                                        MoveTypeId = moveType.Id,
                                        SupplierId = supplierId ?? Guid.Empty,
                                        UserId = userId,
                                        Description = description,
                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    _context.Stock_Movements.Add(movement);

                                    // Stok güncelle
                                    bool stockError = false;
                                    if (targetMoveType == "stock in")
                                    {
                                        inventory.Quantity += quantity;
                                        if (!isNewInventory && purchasePrice.HasValue)
                                            inventory.PurchasePrice = purchasePrice.Value;
                                    }
                                    else if (targetMoveType == "stock out")
                                    {
                                        if (inventory.Quantity < quantity)
                                        {
                                            sb.AppendLine($"⚠️ Yetersiz stok: '{productName}' (Mevcut: {inventory.Quantity}, Talep: {quantity})");
                                            stockError = true;
                                            _context.Stock_Movements.Remove(movement); // Hareketi geri al
                                        }
                                        else
                                        {
                                            inventory.Quantity -= quantity;
                                        }
                                    }

                                    if (!stockError)
                                    {
                                        await _context.SaveChangesAsync();
                                        successCount++;
                                        sb.AppendLine($"✅ {productName}: {quantity} adet {targetMoveType} yapıldı.");
                                    }
                                }

                                responseText = items.Count > 1 
                                    ? $"Toplu Stok İşlemi:\n{sb}" 
                                    : sb.ToString().Trim();
                                
                                if (successCount == 0 && items.Count > 0 && string.IsNullOrEmpty(responseText))
                                     responseText = "İşlem yapılamadı.";
                            }
                            break;

                        // Delivery_Rule (Teslimat Kuralı) ekleme - Senaryo 44
                        case "delivery_rule":
                            if (command.Payload.HasValue)
                            {
                                var root = command.Payload.Value;
                                var items = new List<JsonElement>();

                                if (root.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in itemsElement.EnumerateArray()) items.Add(item);
                                }
                                else
                                {
                                    items.Add(root);
                                }

                                var sb = new StringBuilder();
                                int successCount = 0;

                                foreach (var item in items)
                                {
                                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText());

                                    string supplierName = payload.ContainsKey("supplierName") ? payload["supplierName"].GetString() : null;
                                    string ruleName = payload.ContainsKey("ruleName") ? payload["ruleName"].GetString() : null;
                                    string frequency = payload.ContainsKey("frequency") ? payload["frequency"].GetString() : "Weekly";
                                    string daysOfMonth = payload.ContainsKey("daysOfMonth") ? payload["daysOfMonth"].GetString()?.Replace(';', ',') : null;
                                    string daysOfWeek = payload.ContainsKey("daysOfWeek") ? payload["daysOfWeek"].GetString()?.Replace(';', ',') : null;
                                    string arrivalTime = payload.ContainsKey("arrivalTime") ? payload["arrivalTime"].GetString() : "09:00";
                                    
                                    string startDateStr = payload.ContainsKey("startDate") ? payload["startDate"].GetString() : DateTime.UtcNow.ToString("dd.MM.yyyy");
                                    string endDateStr = payload.ContainsKey("endDate") ? payload["endDate"].GetString() : null;
                                    string color = payload.ContainsKey("calendarColor") ? payload["calendarColor"].GetString() : "#3788d8"; // Default Blue
                                    int interval = payload.ContainsKey("interval") ? payload["interval"].GetInt32() : 1;
                                    int leadTimeDays = payload.ContainsKey("leadTimeDays") ? payload["leadTimeDays"].GetInt32() : 0;

                                    if (string.IsNullOrWhiteSpace(supplierName) || string.IsNullOrWhiteSpace(ruleName))
                                    {
                                        sb.AppendLine("❌ Hata: Tedarikçi adı ve kural adı gerekli.");
                                        continue;
                                    }

                                    var supplier = await _context.Suppliers
                                        .FirstOrDefaultAsync(s => s.SupplierName.ToLower().Contains(supplierName.ToLower()) && s.IsActive);

                                    if (supplier == null)
                                    {
                                        sb.AppendLine($"❌ Hata: '{supplierName}' tedarikçisi bulunamadı.");
                                        continue;
                                    }

                                    // Parse Dates
                                    DateTime start = DateTime.UtcNow;
                                    if(DateTime.TryParse(startDateStr, out var sDate)) start = sDate;

                                    DateTime? end = null;
                                    if(!string.IsNullOrWhiteSpace(endDateStr) && DateTime.TryParse(endDateStr, out var eDate)) end = eDate;

                                    // Frequency Enum
                                    var frequencyEnum = frequency.ToLower() == "monthly"
                                        ? Delivery_Rules.FrequencyType.Monthly
                                        : Delivery_Rules.FrequencyType.Weekly;

                                    // FIX: Default DaysOfWeek if missing for Weekly rules
                                    if (frequencyEnum == Delivery_Rules.FrequencyType.Weekly && string.IsNullOrWhiteSpace(daysOfWeek))
                                    {
                                        daysOfWeek = ((int)DateTime.UtcNow.DayOfWeek).ToString();
                                    }

                                    // Arrival Time Fix (xx.xx or xx:xx format handling)
                                    TimeSpan timeSpan = TimeSpan.Parse(arrivalTime.Replace(".", ":"));

                                    var createCommand = new CreateDelivery_RulesCommand
                                    {
                                        SupplierId = supplier.Id,
                                        RuleName = ruleName,
                                        Frequency = frequencyEnum,
                                        Interval = interval,
                                        DaysOfMonth = daysOfMonth,
                                        DaysOfWeek = daysOfWeek,
                                        ArrivalTime = timeSpan,
                                        StartDate = start,
                                        EndDate = end,
                                        CalendarColor = color,
                                        LeadTimeDays = leadTimeDays,
                                        CompanyId = _currentUserService.CompanyId ?? Guid.Empty 
                                    };

                                    // CreateDelivery_RulesCommand Handler'ını kullanıyoruz (Mediator üzerinden)
                                    await _mediator.Send(createCommand);
                                    successCount++;
                                    sb.AppendLine($"✅ '{supplier.SupplierName}' için '{ruleName}' kuralı eklendi. (Başlangıç: {start:dd.MM.yyyy})");
                                }

                                responseText = items.Count > 1 
                                    ? $"Toplu Teslimat Kuralı Ekleme Sonucu:\n{sb}" 
                                    : sb.ToString().Trim();
                                
                                if (successCount == 0 && items.Count > 0 && string.IsNullOrEmpty(responseText))
                                     responseText = "Hiçbir teslimat kuralı eklenemedi.";
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
                                 var today = DateTime.UtcNow.AddHours(3);
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
                                var yesterday = DateTime.UtcNow.AddHours(3).Date.AddDays(-1);
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
                                .Select(p => $"**{p.ProductName}** (Barkod: {p.Barcode}) - Kategori: {p.CategoryName ?? "-"}, Birim: {p.UnitTypeName ?? "-"}");

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
                                    var today = DateTime.UtcNow.AddHours(3);
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
                                    return $"**{pName}**: {i.Quantity} Adet (Min: {i.CriticalStockQuantity}) {stockStatus} - Alış: {i.PurchasePrice:C2}, Satış: {i.SalePrice:C2}, SKT: {i.ExpirationDate:dd.MM.yyyy}, Seri No: {i.BatchNumber ?? "-"}";
                                });

                            responseText = inventoryItems.Any()
                                ? $"🏭 **Envanter Durumu**:\n- {string.Join("\n- ", inventoryResponse)}"
                                : "Kriterlere uygun envanter kaydı bulunamadı (veya stok boş).";
                            break;

                        // 3. STOK HAREKETLERİ
                        case "stock_movement":
                            var movementsQuery = _context.Stock_Movements
                                .Include(m => m.Inventory)
                                    .ThenInclude(i => i.Product)
                                .Include(m => m.User)
                                .Include(m => m.Supplier)
                                .Include(m => m.MoveType)
                                .Where(m => m.IsActive);

                            if (command.Filters != null)
                            {
                                if (!string.IsNullOrEmpty(command.Filters.DateRange) && command.Filters.DateRange == "today")
                                    movementsQuery = movementsQuery.Where(m => m.CreatedAt.Date == DateTime.UtcNow.AddHours(3).Date);
                                
                                if (!string.IsNullOrEmpty(command.Filters.UserName))
                                    movementsQuery = movementsQuery.Where(m => (m.User.FirstName + " " + m.User.LastName).ToLower().Contains(command.Filters.UserName.ToLower()));

                                if (!string.IsNullOrEmpty(command.Filters.SupplierName))
                                    movementsQuery = movementsQuery.Where(m => m.Supplier.SupplierName.ToLower().Contains(command.Filters.SupplierName.ToLower()));

                                if (!string.IsNullOrEmpty(command.Filters.MoveTypeName))
                                    movementsQuery = movementsQuery.Where(m => m.MoveType.MoveType.ToLower().Contains(command.Filters.MoveTypeName.ToLower()));
                            }

                            var movements = await movementsQuery.OrderByDescending(m => m.CreatedAt).Take(command.Filters?.Take ?? 10).ToListAsync();
                            
                            var movementResponse = movements
                                .Select(m => {
                                    string moveTypeIcon = m.MoveType.MoveType.ToLower().Contains("in") || m.MoveType.MoveType.ToLower().Contains("giriş") ? "📥 Giriş" : "📤 Çıkış";
                                    string userName = m.User != null ? $"{m.User.FirstName} {m.User.LastName}" : "Bilinmeyen Kullanıcı";
                                    // Fiyat hesaplama: Alış veya Satış fiyatı üzerinden toplam
                                    float unitPrice = m.MoveType.MoveType.ToLower().Contains("in") ? m.Inventory.PurchasePrice : m.Inventory.SalePrice;
                                    float totalPrice = m.Quantity * unitPrice;
                                    
                                    return $"{moveTypeIcon} - **{m.Inventory.Product.ProductName}**\n   Miktar: {m.Quantity} Adet | Toplam Tutar: {totalPrice:C2}\n   İşlem Tarihi: {m.CreatedAt:dd.MM.yyyy HH:mm} | İşlemi Yapan: {userName}\n   Seri No: {m.Inventory.BatchNumber ?? "-"} | SKT: {m.Inventory.ExpirationDate:dd.MM.yyyy}";
                                });

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

                    var rootPayload = command.Payload.Value;
                    
                    // --- A. BULK UPDATE (PAYLOAD "ITEMS" İÇERİYORSA) ---
                    if (rootPayload.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        var sb = new StringBuilder();
                        int successCount = 0;

                        foreach (var item in itemsElement.EnumerateArray())
                        {
                            var itemDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText());
                            
                            switch (entity)
                            {
                                case "inventory":
                                    string pName = itemDict.ContainsKey("productName") ? itemDict["productName"].GetString() : null;
                                    if (!string.IsNullOrEmpty(pName))
                                    {
                                        var inv = await _context.Inventories.Include(i => i.Product).FirstOrDefaultAsync(i => i.Product.ProductName.ToLower() == pName.ToLower() && i.IsActive);
                                        if (inv != null)
                                        {
                                            if (itemDict.TryGetValue("quantity", out var q)) inv.Quantity = q.GetInt32();
                                            if (itemDict.TryGetValue("salePrice", out var sp)) inv.SalePrice = sp.GetSingle();
                                            if (itemDict.TryGetValue("purchasePrice", out var pp)) inv.PurchasePrice = pp.GetSingle();
                                            if (itemDict.TryGetValue("criticalStockQuantity", out var csq)) inv.CriticalStockQuantity = csq.GetInt32();
                                            successCount++;
                                            sb.AppendLine($"✅ '{pName}' güncellendi.");
                                        }
                                        else sb.AppendLine($"⚠️ '{pName}' bulunamadı.");
                                    }
                                    break;

                                case "product":
                                    string prodName = itemDict.ContainsKey("productName") ? itemDict["productName"].GetString() : null;
                                    string barcode = itemDict.ContainsKey("barcode") ? itemDict["barcode"].GetString() : null;
                                    
                                    Products prod = null;
                                    if (!string.IsNullOrEmpty(barcode)) prod = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
                                    else if (!string.IsNullOrEmpty(prodName)) prod = await _context.Products.FirstOrDefaultAsync(p => p.ProductName.ToLower() == prodName.ToLower());

                                    if (prod != null)
                                    {
                                        if (itemDict.TryGetValue("newProductName", out var newName)) prod.ProductName = newName.GetString();
                                        if (itemDict.TryGetValue("description", out var desc)) prod.Description = desc.GetString();
                                        if (itemDict.TryGetValue("isActive", out var act)) prod.IsActive = act.GetBoolean();
                                        successCount++;
                                        sb.AppendLine($"✅ '{prod.ProductName}' güncellendi.");
                                    }
                                    else sb.AppendLine($"⚠️ Ürün bulunamadı (Ad: {prodName}, Barkod: {barcode}).");
                                    break;
                                
                                case "supplier":
                                    string suppName = itemDict.ContainsKey("supplierName") ? itemDict["supplierName"].GetString() : null;
                                    if (!string.IsNullOrEmpty(suppName))
                                    {
                                        var supp = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.ToLower() == suppName.ToLower());
                                        if (supp != null)
                                        {
                                            if (itemDict.TryGetValue("contactPerson", out var cp)) supp.ContactPerson = cp.GetString();
                                            if (itemDict.TryGetValue("phoneNumber", out var ph)) supp.PhoneNumber = ph.GetString();
                                            if (itemDict.TryGetValue("email", out var em)) supp.Email = em.GetString();
                                            successCount++;
                                            sb.AppendLine($"✅ '{suppName}' güncellendi.");
                                        }
                                    }
                                    break;
                            }
                        }

                        if (successCount > 0) await _context.SaveChangesAsync();
                        responseText = string.IsNullOrEmpty(sb.ToString()) ? "Toplu güncelleme başarısız." : sb.ToString();
                    }
                    // --- B. FILTER-BASED UPDATE (TEK PAYLOAD, FİLTREYE GÖRE UYGULA) ---
                    else 
                    {
                        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rootPayload.GetRawText());

                        switch (entity)
                        {
                            case "inventory":
                                IQueryable<Inventories> inventoriesToUpdate = _context.Inventories.Include(i => i.Product).ThenInclude(p => p.Category);
                                bool hasInventoryFilter = false;

                                if (command.Filters != null)
                                {
                                    if (!string.IsNullOrEmpty(command.Filters.ProductName))
                                    {
                                        inventoriesToUpdate = inventoriesToUpdate.Where(i => i.Product.ProductName.ToLower().Contains(command.Filters.ProductName.ToLower()));
                                        hasInventoryFilter = true;
                                    }
                                    if (!string.IsNullOrEmpty(command.Filters.CategoryName))
                                    {
                                        inventoriesToUpdate = inventoriesToUpdate.Where(i => i.Product.Category.CategoryName.ToLower().Contains(command.Filters.CategoryName.ToLower()));
                                        hasInventoryFilter = true;
                                    }
                                }

                                if (!hasInventoryFilter) { responseText = "Güncellenecek envanter için ürün adı veya kategori belirtmelisiniz."; break; }

                                var inventoryList = await inventoriesToUpdate.ToListAsync();
                                if (!inventoryList.Any()) { responseText = "Güncellenecek envanter kaydı bulunamadı."; break; }

                                foreach (var inventory in inventoryList)
                                {
                                    if (payload.TryGetValue("salePrice", out var salePrice)) inventory.SalePrice = salePrice.GetSingle();
                                    if (payload.TryGetValue("purchasePrice", out var purchasePrice)) inventory.PurchasePrice = purchasePrice.GetSingle();
                                    if (payload.TryGetValue("criticalStockQuantity", out var criticalStock)) inventory.CriticalStockQuantity = criticalStock.GetInt32();
                                    if (payload.TryGetValue("quantity", out var quantity)) inventory.Quantity = quantity.GetInt32();

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

                            case "product":
                                IQueryable<Products> productsToUpdate = _context.Products.Include(p => p.Category);
                                bool hasProductFilter = false;

                                if (command.Filters != null)
                                {
                                    var filterName = command.Filters.Name ?? command.Filters.ProductName;

                                    if (!string.IsNullOrEmpty(filterName))
                                    {
                                        var lowerName = filterName.ToLower();
                                        // Exact Match Priority
                                        var exactProduct = await _context.Products.FirstOrDefaultAsync(p => p.ProductName.ToLower() == lowerName);
                                        
                                        if (exactProduct != null)
                                        {
                                            productsToUpdate = productsToUpdate.Where(p => p.Id == exactProduct.Id);
                                        }
                                        else
                                        {
                                            productsToUpdate = productsToUpdate.Where(p => p.ProductName.ToLower().Contains(lowerName));
                                        }
                                        hasProductFilter = true;
                                    }

                                    if (!string.IsNullOrEmpty(command.Filters.Barcode))
                                    {
                                        productsToUpdate = productsToUpdate.Where(p => p.Barcode == command.Filters.Barcode);
                                        hasProductFilter = true;
                                    }
                                }

                                if (!hasProductFilter) { responseText = "Güncellenecek ürün için isim veya barkod belirtmelisiniz."; break; }

                                var productList = await productsToUpdate.ToListAsync();
                                if (!productList.Any()) { responseText = "Güncellenecek ürün bulunamadı."; break; }

                                foreach (var product in productList)
                                {
                                    if (payload.TryGetValue("productName", out var pName)) product.ProductName = pName.GetString();
                                    if (payload.TryGetValue("description", out var prodDesc)) product.Description = prodDesc.GetString();
                                    if (payload.TryGetValue("isActive", out var isActive)) product.IsActive = isActive.GetBoolean();

                                    if (payload.TryGetValue("categoryName", out var catName))
                                    {
                                        var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName.ToLower().Contains(catName.GetString().ToLower()));
                                        if (category != null) 
                                        {
                                            product.CategoryId = category.Id;
                                        }
                                        else
                                        {
                                            responseText += $"\n⚠️ Uyarı: '{catName}' kategorisi bulunamadığı için ürünün kategorisi güncellenemedi.";
                                        }
                                    }
                                    
                                    if (payload.TryGetValue("unitTypeName", out var unitTypeName))
                                    {
                                        var unitType = await _context.Unit_Types.FirstOrDefaultAsync(u => u.UnitName.ToLower() == unitTypeName.GetString().ToLower());
                                        if (unitType != null) 
                                        {
                                            product.UnitTypeId = unitType.Id;
                                        }
                                        else
                                        {
                                            responseText += $"\n⚠️ Uyarı: '{unitTypeName}' birim tipi bulunamadığı için güncellenemedi.";
                                        }
                                    }
                                }

                                await _context.SaveChangesAsync();
                                responseText = $"✅ {productList.Count} adet ürün başarıyla güncellendi.";
                                break;
                            
                            case "stock_movement":
                                Guid movementId = Guid.Empty;
                                if(command.Filters != null && !string.IsNullOrEmpty(command.Filters.Id)) 
                                     Guid.TryParse(command.Filters.Id, out movementId);
                                
                                if(movementId == Guid.Empty) { responseText = "Güncellenecek stok hareketinin ID'si (filters.id) belirtilmelidir."; break; }

                                var movement = await _context.Stock_Movements.Include(m => m.MoveType).FirstOrDefaultAsync(m => m.Id == movementId);
                                if(movement == null) { responseText = "Stok hareketi bulunamadı."; break; }

                                int newQty = movement.Quantity;
                                if (payload.TryGetValue("quantity", out var qty)) newQty = qty.GetInt32();
                                
                                string newDesc = movement.Description;
                                if (payload.TryGetValue("description", out var movDesc)) newDesc = movDesc.GetString();

                                Guid newMoveTypeId = movement.MoveTypeId;
                                if (payload.TryGetValue("moveTypeId", out var mtId))
                                {
                                    Guid.TryParse(mtId.GetString(), out newMoveTypeId);
                                }
                                else if (payload.TryGetValue("moveTypeName", out var mtName))
                                {
                                    string targetMoveType = mtName.GetString().ToLower();
                                    if (targetMoveType.Contains("in") || targetMoveType.Contains("giriş")) targetMoveType = "stock in";
                                    else if (targetMoveType.Contains("out") || targetMoveType.Contains("çıkış")) targetMoveType = "stock out";

                                    var moveType = await _context.Move_Types.FirstOrDefaultAsync(m => m.MoveType.ToLower() == targetMoveType);
                                    if (moveType != null) newMoveTypeId = moveType.Id;
                                }

                                Guid newInventoryId = movement.InventoryId;
                                if (payload.TryGetValue("inventoryId", out var invId))
                                {
                                    Guid.TryParse(invId.GetString(), out newInventoryId);
                                }

                                var updateCmd = new UpdateStock_MovementsCommand {
                                    Id = movement.Id,
                                    CompanyId = movement.CompanyId,
                                    InventoryId = newInventoryId,
                                    MoveTypeId = newMoveTypeId,
                                    SupplierId = movement.SupplierId,
                                    UserId = _currentUserService.UserId,
                                    Quantity = newQty,
                                    Description = newDesc,
                                    IsActive = true
                                };

                                await _mediator.Send(updateCmd);
                                responseText = "✅ Stok hareketi başarıyla güncellendi (Stok miktarları yeniden hesaplandı).";
                                break;

                            case "supplier":
                                IQueryable<Suppliers> suppliersToUpdate = _context.Suppliers;
                                bool hasSupplierFilter = false;

                                if (command.Filters != null)
                                {
                                    var filterName = command.Filters.Name ?? command.Filters.SupplierName;

                                    if (!string.IsNullOrEmpty(filterName))
                                    {
                                        var lowerName = filterName.ToLower();
                                        // Öncelik tam eşleşmede (Exact Match Priority)
                                        var exactMatch = await _context.Suppliers
                                            .FirstOrDefaultAsync(s => s.SupplierName.ToLower() == lowerName);

                                        if (exactMatch != null)
                                        {
                                            suppliersToUpdate = suppliersToUpdate.Where(s => s.Id == exactMatch.Id);
                                        }
                                        else
                                        {
                                            suppliersToUpdate = suppliersToUpdate.Where(s => s.SupplierName.ToLower().Contains(lowerName));
                                        }
                                        hasSupplierFilter = true;
                                    }

                                    if (command.Filters.IsActive.HasValue)
                                    {
                                        suppliersToUpdate = suppliersToUpdate.Where(s => s.IsActive == command.Filters.IsActive.Value);
                                        hasSupplierFilter = true;
                                    }
                                }

                                if (!hasSupplierFilter) { responseText = "Güncellenecek tedarikçi ismini belirtmelisiniz."; break; }

                                var supplierList = await suppliersToUpdate.ToListAsync();
                                if (!supplierList.Any()) { responseText = "Güncellenecek tedarikçi bulunamadı."; break; }

                                foreach (var supplier in supplierList)
                                {
                                    if (payload.TryGetValue("contactPerson", out var contact)) supplier.ContactPerson = contact.GetString();
                                    if (payload.TryGetValue("phoneNumber", out var phone)) supplier.PhoneNumber = phone.GetString();
                                    if (payload.TryGetValue("email", out var email)) supplier.Email = email.GetString();
                                    if (payload.TryGetValue("isActive", out var isActive)) supplier.IsActive = isActive.GetBoolean();
                                    if (payload.TryGetValue("address", out var addr)) supplier.Address = addr.GetString();
                                }
                                await _context.SaveChangesAsync();
                                responseText = $"✅ {supplierList.Count} adet tedarikçi güncellendi.";
                                break;

                            case "category":
                                IQueryable<Categories> categoriesToUpdate = _context.Categories;
                                bool hasCategoryFilter = false;

                                if (command.Filters != null)
                                {
                                    var filterName = command.Filters.Name ?? command.Filters.CategoryName;

                                    if (!string.IsNullOrEmpty(filterName))
                                    {
                                        var lowerName = filterName.ToLower();
                                        var exactCategory = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName.ToLower() == lowerName);

                                        if (exactCategory != null)
                                        {
                                            categoriesToUpdate = categoriesToUpdate.Where(c => c.Id == exactCategory.Id);
                                        }
                                        else
                                        {
                                            categoriesToUpdate = categoriesToUpdate.Where(c => c.CategoryName.ToLower().Contains(lowerName));
                                        }
                                        hasCategoryFilter = true;
                                    }
                                }

                                if (!hasCategoryFilter) { responseText = "Güncellenecek kategori ismini belirtmelisiniz."; break; }

                                var categoryList = await categoriesToUpdate.ToListAsync();
                                if (!categoryList.Any()) { responseText = "Güncellenecek kategori bulunamadı."; break; }

                                foreach (var category in categoryList)
                                {
                                    if (payload.TryGetValue("categoryName", out var catName)) category.CategoryName = catName.GetString();
                                    if (payload.TryGetValue("description", out var catDesc)) category.Description = catDesc.GetString();
                                }
                                await _context.SaveChangesAsync();
                                responseText = $"✅ {categoryList.Count} adet kategori güncellendi.";
                                break;

                            case "delivery_rule":
                                IQueryable<Delivery_Rules> rulesToUpdate = _context.Delivery_Rules.Include(r => r.Supplier);
                                bool hasRuleFilter = false;

                                if (command.Filters != null)
                                {
                                    if (!string.IsNullOrEmpty(command.Filters.Id)) 
                                    {
                                        rulesToUpdate = rulesToUpdate.Where(r => r.Id.ToString() == command.Filters.Id);
                                        hasRuleFilter = true;
                                    }
                                    
                                    var filterName = command.Filters.Name ?? command.Filters.RuleName;
                                    if (!string.IsNullOrEmpty(filterName)) 
                                    {
                                        rulesToUpdate = rulesToUpdate.Where(r => r.RuleName.ToLower().Contains(filterName.ToLower()));
                                        hasRuleFilter = true;
                                    }
                                }

                                if (!hasRuleFilter) { responseText = "Güncellenecek teslimat kuralı için ID veya İsim belirtmelisiniz."; break; }

                                var ruleList = await rulesToUpdate.ToListAsync();
                                if (!ruleList.Any()) { responseText = "Güncellenecek teslimat kuralı bulunamadı."; break; }

                                foreach (var rule in ruleList)
                                {
                                    if (payload.TryGetValue("arrivalTime", out var time)) rule.ArrivalTime = TimeSpan.Parse(time.GetString());
                                    if (payload.TryGetValue("interval", out var interval)) rule.Interval = interval.GetInt32();
                                    if (payload.TryGetValue("isActive", out var isActive)) rule.IsActive = isActive.GetBoolean();
                                }
                                await _context.SaveChangesAsync();
                                responseText = $"✅ {ruleList.Count} adet teslimat kuralı güncellendi.";
                                break;

                            case "unit_type":
                                IQueryable<Unit_Types> unitsToUpdate = _context.Unit_Types;
                                bool hasUnitFilter = false;

                                if (command.Filters != null)
                                {
                                    var filterName = command.Filters.Name; // UnitType usually just 'Name'
                                    if (!string.IsNullOrEmpty(filterName))
                                    {
                                        var lowerName = filterName.ToLower();
                                        var exactUnit = await _context.Unit_Types.FirstOrDefaultAsync(u => u.UnitName.ToLower() == lowerName);

                                        if (exactUnit != null)
                                        {
                                            unitsToUpdate = unitsToUpdate.Where(u => u.Id == exactUnit.Id);
                                        }
                                        else
                                        {
                                            unitsToUpdate = unitsToUpdate.Where(u => u.UnitName.ToLower().Contains(lowerName));
                                        }
                                        hasUnitFilter = true;
                                    }
                                }

                                if (!hasUnitFilter) { responseText = "Güncellenecek birim tipi ismini belirtmelisiniz."; break; }

                                var unitList = await unitsToUpdate.ToListAsync();
                                if (!unitList.Any()) { responseText = "Güncellenecek birim tipi bulunamadı."; break; }

                                foreach (var unit in unitList)
                                {
                                    if (payload.TryGetValue("unitName", out var unitName)) unit.UnitName = unitName.GetString();
                                }
                                await _context.SaveChangesAsync();
                                responseText = $"✅ {unitList.Count} adet birim tipi güncellendi.";
                                break;
                                
                            default:
                                responseText = $"'{entity}' için güncelleme işlemi tanımlanmamış.";
                                break;
                        }
                    }
                }
                // -------------------------------------------------------------------------
                // 5. DELETE İŞLEMLERİ
                // -------------------------------------------------------------------------
                else if (op == "delete")
                {
                    // 1. BULK DELETE via PAYLOAD "ITEMS"
                    if (command.Payload.HasValue && command.Payload.Value.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                         int successCount = 0;
                         foreach (var item in itemsElement.EnumerateArray())
                         {
                             var itemDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText());
                             
                             switch(entity)
                             {
                                 case "product":
                                     string pName = itemDict.ContainsKey("productName") ? itemDict["productName"].GetString() : null;
                                     if(!string.IsNullOrEmpty(pName)) {
                                         await _context.Products.Where(p => p.ProductName.ToLower() == pName.ToLower()).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
                                         successCount++;
                                     }
                                     break;
                                 case "inventory":
                                     string iName = itemDict.ContainsKey("productName") ? itemDict["productName"].GetString() : null;
                                     if(!string.IsNullOrEmpty(iName)) {
                                         // İlgili ürünün tüm stoklarını pasife çek
                                          var prod = await _context.Products.FirstOrDefaultAsync(p => p.ProductName.ToLower() == iName.ToLower());
                                          if(prod != null) {
                                              await _context.Inventories.Where(i => i.ProductId == prod.Id).ExecuteUpdateAsync(s => s.SetProperty(i => i.IsActive, false));
                                              successCount++;
                                          }
                                     }
                                     break;
                             }
                         }
                         responseText = successCount > 0 ? $"✅ {successCount} adet kayıt silindi (toplu işlem)." : "Silinecek kayıt bulunamadı.";
                    }
                    // 2. FILTER BASED DELETE (Mevcut Mantık)
                    else 
                    {
                        if (command.Filters == null) {
                            responseText = "Silme işlemi için filtre belirtmelisiniz.";
                        }
                        else
                        {
                             switch(entity)
                             {
                                case "supplier":
                                    var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.ToLower().Contains(command.Filters.Name.ToLower()));
                                    if(supplier == null) { responseText = $"'{command.Filters.Name}' adında bir tedarikçi bulunamadı."; break; }
                                    supplier.IsActive = false;
                                    await _context.SaveChangesAsync();
                                    responseText = $"✅ '{supplier.SupplierName}' tedarikçisi silindi (pasif olarak ayarlandı).";
                                    break;
                                
                                case "inventory":
                                     IQueryable<Inventories> inventoriesToDelete = _context.Inventories;
                                     if(!string.IsNullOrEmpty(command.Filters.ProductName))
                                        inventoriesToDelete = inventoriesToDelete.Where(i => i.Product.ProductName.ToLower().Contains(command.Filters.ProductName.ToLower()));
                                    
                                     if (command.Filters.ExpirationDate == "expired")
                                        inventoriesToDelete = inventoriesToDelete.Where(i => i.ExpirationDate < DateTime.UtcNow);
                                    
                                    var deletedCount = await inventoriesToDelete.ExecuteUpdateAsync(s => s.SetProperty(i => i.IsActive, false));
                                    responseText = $"✅ {deletedCount} adet envanter kaydı silindi (pasif yapıldı).";
                                    break;

                                case "category":
                                    var categoryToDelete = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName.ToLower().Contains(command.Filters.Name.ToLower()));
                                    if (categoryToDelete == null) { responseText = $"'{command.Filters.Name}' kategorisi bulunamadı."; break; }
                                    categoryToDelete.IsActive = false;
                                    await _context.SaveChangesAsync();
                                    responseText = $"✅ '{categoryToDelete.CategoryName}' kategorisi silindi.";
                                    break;

                                case "delivery_rule":
                                    Delivery_Rules ruleToDelete = null;
                                    if (!string.IsNullOrEmpty(command.Filters.Id))
                                        ruleToDelete = await _context.Delivery_Rules.FirstOrDefaultAsync(r => r.Id.ToString() == command.Filters.Id);

                                    if (ruleToDelete == null) { responseText = "Silinecek teslimat kuralı bulunamadı."; break; }
                                    ruleToDelete.IsActive = false;
                                    await _context.SaveChangesAsync();
                                    responseText = $"✅ Teslimat kuralı silindi.";
                                    break;

                                case "product":
                                    if (command.Filters == null || string.IsNullOrWhiteSpace(command.Filters.Name))
                                    {
                                        responseText = "Silinecek ürünün adını belirtmelisiniz.";
                                        break;
                                    }

                                    // Special Case: Explicit Bulk Delete via 'StartsWith:'
                                    if (command.Filters.Name.StartsWith("StartsWith:"))
                                    {
                                        var prefix = command.Filters.Name.Replace("StartsWith:", "").Trim().ToLower();
                                        if (string.IsNullOrEmpty(prefix)) { responseText = "Başlangıç değeri belirtilmedi."; break; }

                                        var bulkDeleteCount = await _context.Products
                                            .Where(p => p.ProductName.ToLower().StartsWith(prefix) && p.IsActive)
                                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
                                        
                                        responseText = $"✅ '{prefix}...' ile başlayan {bulkDeleteCount} adet ürün silindi.";
                                    }
                                    else
                                    {
                                        // Standard Deletion Logic (Safety First)
                                        var filterName = command.Filters.Name.Trim().ToLower();

                                        // 1. Try Exact Match
                                        var exactProduct = await _context.Products
                                            .FirstOrDefaultAsync(p => p.ProductName.ToLower() == filterName && p.IsActive);

                                        if (exactProduct != null)
                                        {
                                            exactProduct.IsActive = false;
                                            await _context.SaveChangesAsync();
                                            responseText = $"✅ '{exactProduct.ProductName}' başarıyla silindi.";
                                        }
                                        else
                                        {
                                            // 2. Check Partial Matches
                                            var partialMatches = await _context.Products
                                                .Where(p => p.ProductName.ToLower().Contains(filterName) && p.IsActive)
                                                .ToListAsync();

                                            if (partialMatches.Count == 0)
                                            {
                                                responseText = $"❌ '{command.Filters.Name}' isminde silinecek aktif bir ürün bulunamadı.";
                                            }
                                            else if (partialMatches.Count == 1)
                                            {
                                                partialMatches[0].IsActive = false;
                                                await _context.SaveChangesAsync();
                                                responseText = $"✅ '{partialMatches[0].ProductName}' silindi.";
                                            }
                                            else
                                            {
                                                // 3. Too Many Matches -> Safety Stop
                                                var names = string.Join(", ", partialMatches.Take(3).Select(p => p.ProductName));
                                                responseText = $"⚠️ '{command.Filters.Name}' ile eşleşen {partialMatches.Count} farklı ürün bulundu ({names}...). Yanlışlıkla çoklu silme yapmamak için lütfen ürünün tam adını yazın.";
                                            }
                                        }
                                    }
                                    break;

                                case "unit_type":
                                    IQueryable<Unit_Types> unitsToDelete = _context.Unit_Types;
                                    if (command.Filters != null && !string.IsNullOrEmpty(command.Filters.Name))
                                        unitsToDelete = unitsToDelete.Where(u => u.UnitName.ToLower().Contains(command.Filters.Name.ToLower()));
                                    var deletedUnitCount = await unitsToDelete.ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));
                                    responseText = $"✅ {deletedUnitCount} adet birim tipi silindi.";
                                    break;
                                
                                case "stock_movement":
                                    Guid moveId = Guid.Empty;
                                    if(command.Filters != null && !string.IsNullOrEmpty(command.Filters.Id)) 
                                        Guid.TryParse(command.Filters.Id, out moveId);
                                    
                                    if(moveId == Guid.Empty) { responseText = "Silinecek stok hareketinin ID'si belirtilmelidir."; break; }

                                    await _mediator.Send(new DeleteStock_MovementsCommand(moveId));
                                    responseText = "✅ Stok hareketi başarıyla silindi (Envanter güncellendi).";
                                    break;

                                default:
                                    responseText = $"'{entity}' için silme işlemi tanımlanmamış.";
                                    break;
                             }
                        }
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