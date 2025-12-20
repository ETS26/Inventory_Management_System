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
using Inventory_Management.Application.Features.Commands.InventoriesCommand;
using Inventory_Management.Application.Features.Commands.SuppliersCommand;
using Inventory_Management.Application.Features.Commands.Unit_TypesCommand;
using Inventory_Management.Domain.Common;

namespace Inventory_Management.WebApi.Controllers
{
    public class UserQueryModel
    {
        public string Query { get; set; }
        public string? SessionId { get; set; }
    }

    public class ClearSessionRequest { public string SessionId { get; set; } }
    public class ConversationMessage { public string Role { get; set; } public string Content { get; set; } }

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
        [JsonPropertyName("productName")] public string ProductName { get; set; }
        [JsonPropertyName("supplierName")] public string SupplierName { get; set; }
        [JsonPropertyName("moveTypeName")] public string MoveTypeName { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("categoryName")] public string CategoryName { get; set; }
        [JsonPropertyName("dateRange")] public string DateRange { get; set; }
        [JsonPropertyName("take")] public int? Take { get; set; }
        [JsonPropertyName("isBelowCriticalStock")] public bool? IsBelowCriticalStock { get; set; }
        [JsonPropertyName("isActive")] public bool? IsActive { get; set; }
        [JsonPropertyName("sortBy")] public string SortBy { get; set; }
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

        public AiAssistantController(IHttpClientFactory httpClientFactory, IMediator mediator,
            IConfiguration configuration, Inventory_Management_Context context, ICurrentUserService currentUserService)
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
                return StatusCode(500, new { response = "Gemini API anahtarı yapılandırılmamış.", sessionId });

            apiKey = string.Join("", apiKey.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            if (!_sessions.ContainsKey(sessionId))
                _sessions[sessionId] = new SessionContext();

            var session = _sessions[sessionId];
            session.History.Add(new ConversationMessage { Role = "user", Content = userQuery });

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemma-2-27b-it:generateContent?key={apiKey}";

            var systemPrompt = @"Sen InventoryETS envanter yönetim asistanısın. Türkçe komutları JSON'a çevir.

# TEMEL KURALLAR
1. 'kaç', 'sayı', 'toplam', 'ne kadar' -> CALCULATE
2. 'listele', 'göster', 'getir' -> GET  
3. 'ekle', 'yeni', 'oluştur' -> POST (bilgi eksikse ADIM ADIM sor!)
4. 'güncelle', 'değiştir' -> UPDATE
5. 'sil', 'kaldır', 'pasif yap' -> DELETE

# POST (OLUŞTURMA) İŞLEMLERİ

## YENİ ÜRÜN EKLEME
Kullanıcı 'ürün ekle' dediğinde ADIM ADIM:
1. 'Ürün adı nedir?'
2. 'Barkodu nedir?'
3. 'Kategorisi nedir?'
4. 'Birim tipi nedir?'
JSON oluştur. Entity: ""Product"". Payload'a productName, barcode, categoryName, unitTypeName ekle.

## YENİ TEDARİKÇİ EKLEME
Kullanıcı 'tedarikçi ekle' dediğinde ADIM ADIM:
1. 'Tedarikçi firma adı nedir?'
2. 'Yetkili kişi kimdir?'
3. '[Opsiyonel] Telefon numarası nedir?'
4. '[Opsiyonel] E-mail adresi nedir?'
JSON oluştur. Entity: ""Supplier"". Payload: supplierName, contactPerson, phoneNumber, email.

## YENİ BİRİM TİPİ EKLEME
Kullanıcı 'birim tipi ekle' dediğinde:
1. 'Birim tipinin adı nedir? (Örn: Koli, Palet, Düzine)'
JSON oluştur. Entity: ""Unit_Type"". Payload: unitName.

## YENİ STOK HAREKETİ (GİRİŞ/ÇIKIŞ) - ENTEGRE ENVANTER OLUŞTURMA
Kullanıcı 'stok girişi', 'stok ekle', 'stok çıkışı' veya 'stok sat' gibi bir komut kullandığında:

ADIM 1: 'Hangi ürün için?'
ADIM 2: 'Kaç adet?'
ADIM 3: Eğer ürün envanterde yoksa: 'Bu ürün envanterde bulunamadı. Yeni bir envanter kaydı oluşturmak ister misiniz? (evet/hayır)'
  - Eğer 'hayır' -> İşlemi sonlandır.
  - Eğer 'evet' veya zaten yeni envanter bilgileri varsa -> ADIM 4'e geç.
ADIM 4 (Yeni Envanter için):
  - 'Alış fiyatı nedir?'
  - 'Satış fiyatı nedir?'
  - 'Kritik stok seviyesi kaç adet olmalı?'
ADIM 5: '[GİRİŞ İSE] Hangi tedarikçiden geldi? (Tedarikçi adını yazın veya boş bırakmak için """"atla"""" deyin)' (sadece 'Giriş' tipinde sorulur)
ADIM 6: '[İsteğe bağlı] Açıklama eklemek ister misiniz? (Açıklamayı yazın veya boş bırakmak için """"atla"""" deyin)'

SON ADIM: Tüm bilgiler toplandıysa JSON oluştur.
Entity: ""Stock_Movement"". Payload'a productName, quantity, moveTypeName ('Giriş' veya 'Çıkış' olarak), ve varsa supplierName, description ekle.
Eğer yeni envanter oluşturulduysa, Payload'a ek olarak:
isNewInventory: true, purchasePrice, salePrice, criticalStockQuantity ekle.

## ÜRÜNÜ ENVANTERE EKLEME
Kullanıcı 'ürünü envantere ekle' dediğinde:
1. 'Hangi ürün?'
2. 'Alış fiyatı nedir?'
3. 'Satış fiyatı nedir?'
4. 'Kritik stok seviyesi kaç adet olmalı?'
JSON oluştur. Entity: ""Inventory"". Payload: productName, purchasePrice, salePrice, criticalStock.

# GET (LİSTELEME) İŞLEMLERİ
- 'ürünleri listele': { ""operation"": ""GET"", ""entity"": ""Products"" }
- 'tedarikçileri göster': { ""operation"": ""GET"", ""entity"": ""Suppliers"" }
- 'birim tiplerini listele': { ""operation"": ""GET"", ""entity"": ""Unit_Types"" }
- 'kritik stoktaki ürünler': { ""operation"": ""GET"", ""entity"": ""Inventories"", ""filters"": { ""isBelowCriticalStock"": true } }
- 'bugünkü stok hareketleri': { ""operation"": ""GET"", ""entity"": ""Stock_Movements"", ""filters"": { ""dateRange"": ""today"" } }
- 'mouse ürününün hareketleri': { ""operation"": ""GET"", ""entity"": ""Stock_Movements"", ""filters"": { ""productName"": ""mouse"" } }

# UPDATE (GÜNCELLEME) İŞLEMLERİ
- 'Mouse ürününün adını Gaming Mouse yap': { ""operation"": ""UPDATE"", ""entity"": ""Product"", ""filters"": { ""name"": ""Mouse"" }, ""payload"": { ""productName"": ""Gaming Mouse"" } }
- 'Logitech tedarikçisinin yetkilisini Can Yılmaz yap': { ""operation"": ""UPDATE"", ""entity"": ""Supplier"", ""filters"": { ""name"": ""Logitech"" }, ""payload"": { ""contactPerson"": ""Can Yılmaz"" } }
- 'Klavyenin satış fiyatını 500 TL yap': { ""operation"": ""UPDATE"", ""entity"": ""Inventory"", ""filters"": { ""productName"": ""Klavye"" }, ""payload"": { ""salePrice"": 500 } }

# DELETE (SİLME/PASİF ETME) İŞLEMLERİ
- 'Mouse ürününü sil': { ""operation"": ""DELETE"", ""entity"": ""Product"", ""filters"": { ""name"": ""Mouse"" } }
- 'Eski yazıcıyı envanterden kaldır': { ""operation"": ""DELETE"", ""entity"": ""Inventory"", ""filters"": { ""productName"": ""Eski yazıcı"" } }
- 'ABC Tedarikçisini sil': { ""operation"": ""DELETE"", ""entity"": ""Supplier"", ""filters"": { ""name"": ""ABC Tedarikçisi"" } }

# ÖNEMLİ:
- Her komut için ilgili entity ve operation adını doğru belirle.
- Eksik bilgi varsa kullanıcıya sor, tüm bilgiler toplanmadan JSON oluşturma.
- Filtreleri ('filters') ve güncellenecek verileri ('payload') doğru şekilde JSON'a yerleştir.
";

            var conversationHistory = new StringBuilder();
            var recentMessages = session.History.TakeLast(8).ToList(); // Son 8 mesaj (4 soru-cevap)

            if (recentMessages.Any())
            {
                conversationHistory.AppendLine("\n# ÖNCEKİ KONUŞMA:");
                foreach (var msg in recentMessages)
                    conversationHistory.AppendLine($"{(msg.Role == "user" ? "Kullanıcı" : "Asistan")}: {msg.Content}");
            }

            var fullPrompt = systemPrompt + conversationHistory + $"\n\n# ŞİMDİ\nKullanıcı: {userQuery}\n\n# TALİMAT:\n- Kullanıcının ŞİMDİ söylediği mesaja odaklan!\n- Eğer yeni bir 'ürün ekle' komutu geliyorsa, eski bilgileri UNUTUP YENİ bilgileri toplamaya başla.\n- Ürün ekleme diyaloğundaysan ve henüz 4 zorunlu alan (ad, barkod, kategori, birim) toplanmadıysa, eksik olanı sor.\n- 4 zorunlu alan toplandıysa, JSON oluştur.\n- JSON'da MUTLAKA 'payload' alanı olmalı!";

            var payload = new { contents = new[] { new { parts = new[] { new { text = fullPrompt } } } } };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(requestUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { response = $"AI API hatası: {error}", sessionId });
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);
                var llmOutput = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text.Trim();

                if (string.IsNullOrWhiteSpace(llmOutput))
                    return Ok(new { response = "Anlayamadım, tekrar deneyin.", sessionId });

                session.History.Add(new ConversationMessage { Role = "assistant", Content = llmOutput });

                var cleanJson = llmOutput.Replace("```json", "").Replace("```", "").Trim();
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
                                responseText = responseProp.GetValue(resultValue)?.ToString() ?? "";
                        }

                        if (responseText.Contains("başarıyla"))
                        {
                            session.CollectedData.Clear();
                            session.CurrentOperation = null;
                            session.CurrentStep = null;
                        }

                        return Ok(new { response = responseText, sessionId });
                    }
                }

                return Ok(new { response = llmOutput, sessionId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { response = $"Sunucu hatası: {ex.Message}", sessionId });
            }
        }

        private async Task<IActionResult> ProcessAiCommand(string llmJson, SessionContext session)
        {
            string responseText = "Komutu anlayamadım.";

            // İlk olarak gelen JSON'u logla
            Console.WriteLine($"=== RECEIVED JSON ===");
            Console.WriteLine(llmJson);
            Console.WriteLine($"=== END JSON ===");

            try
            {
                var cmd = JsonSerializer.Deserialize<AiCommand>(llmJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cmd == null || string.IsNullOrEmpty(cmd.Operation))
                    return Ok(new { response = "Geçersiz komut." });

                Console.WriteLine($"Operation: {cmd.Operation}, Entity: {cmd.Entity}");

                var op = cmd.Operation.ToLower();
                var entity = cmd.Entity?.ToLower() ?? "";

                // ==================== POST (CREATE) ====================
                if (op == "post")
                {
                    Console.WriteLine($"POST operation detected for entity: {entity}");
                    if (!cmd.Payload.HasValue)
                        return Ok(new { response = "❌ Payload bulunamadı. Lütfen tüm bilgileri sağlayın." });

                    var payloadRaw = cmd.Payload.Value.GetRawText();
                    var p = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadRaw);

                    switch (entity)
                    {
                        case "product":
                            #region Create Product
                            try
                            {
                                if (!p.TryGetValue("productName", out var productNameElement) || string.IsNullOrEmpty(productNameElement.GetString()))
                                    return Ok(new { response = "❌ Ürün adı zorunludur." });
                                if (!p.TryGetValue("barcode", out var barcodeElement) || string.IsNullOrEmpty(barcodeElement.GetString()))
                                    return Ok(new { response = "❌ Barkod zorunludur." });
                                if (!p.TryGetValue("categoryName", out var categoryNameElement) || string.IsNullOrEmpty(categoryNameElement.GetString()))
                                    return Ok(new { response = "❌ Kategori adı zorunludur." });
                                if (!p.TryGetValue("unitTypeName", out var unitTypeNameElement) || string.IsNullOrEmpty(unitTypeNameElement.GetString()))
                                    return Ok(new { response = "❌ Birim tipi adı zorunludur." });

                                var productName = productNameElement.GetString();
                                var barcode = barcodeElement.GetString();
                                var categoryName = categoryNameElement.GetString();
                                var unitTypeName = unitTypeNameElement.GetString();

                                var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName.ToLower() == categoryName.ToLower());
                                if (category == null)
                                    return Ok(new { response = $"❌ '{categoryName}' adında bir kategori bulunamadı." });

                                var unitType = await _context.Unit_Types.FirstOrDefaultAsync(u => u.UnitName.ToLower() == unitTypeName.ToLower());
                                if (unitType == null)
                                    return Ok(new { response = $"❌ '{unitTypeName}' adında bir birim tipi bulunamadı." });

                                var productExists = await _context.Products.AnyAsync(pr => pr.ProductName.ToLower() == productName.ToLower() || pr.Barcode == barcode);
                                if (productExists)
                                    return Ok(new { response = "❌ Bu ürün adı veya barkod zaten kullanılıyor." });

                                var createCommand = new CreateProductsCommand
                                {
                                    ProductName = productName,
                                    Barcode = barcode,
                                    CategoryId = category.Id,
                                    UnitTypeId = unitType.Id,
                                    Description = p.TryGetValue("description", out var desc) ? desc.GetString() : null,
                                    ImageURL = p.TryGetValue("imageUrl", out var img) ? img.GetString() : null
                                };

                                await _mediator.Send(createCommand);
                                responseText = $"✅ '{productName}' ürünü başarıyla eklendi.";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"EXCEPTION in Product creation: {ex.Message}");
                                responseText = $"❌ Ürün eklenirken hata: {ex.InnerException?.Message ?? ex.Message}";
                            }
                            #endregion
                            break;

                        case "supplier":
                            #region Create Supplier
                            try
                            {
                                if (!p.TryGetValue("supplierName", out var supplierNameElement) || string.IsNullOrEmpty(supplierNameElement.GetString()))
                                    return Ok(new { response = "❌ Tedarikçi adı zorunludur." });

                                var supplierName = supplierNameElement.GetString();
                                var contactPerson = p.TryGetValue("contactPerson", out var cpElement) ? cpElement.GetString() : null;
                                var phoneNumber = p.TryGetValue("phoneNumber", out var pnElement) ? pnElement.GetString() : null;
                                var email = p.TryGetValue("email", out var eElement) ? eElement.GetString() : null;

                                var supplierExists = await _context.Suppliers.AnyAsync(s => s.SupplierName.ToLower() == supplierName.ToLower());
                                if (supplierExists)
                                    return Ok(new { response = $"❌ '{supplierName}' adında bir tedarikçi zaten mevcut." });

                                var supplierCommand = new CreateSuppliersCommand
                                {
                                    SupplierName = supplierName,
                                    ContactPerson = contactPerson,
                                    PhoneNumber = phoneNumber,
                                    Email = email
                                };
                                await _mediator.Send(supplierCommand);
                                responseText = $"✅ '{supplierName}' tedarikçisi başarıyla eklendi.";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"EXCEPTION in Supplier creation: {ex.Message}");
                                responseText = $"❌ Tedarikçi oluşturulurken bir hata oluştu: {ex.InnerException?.Message ?? ex.Message}";
                            }
                            #endregion
                            break;

                        case "unit_type":
                            #region Create Unit_Type
                            try
                            {
                                if (!p.TryGetValue("unitName", out var unitNameElement) || string.IsNullOrEmpty(unitNameElement.GetString()))
                                    return Ok(new { response = "❌ Birim tipi adı zorunludur." });

                                var unitName = unitNameElement.GetString();
                                var unitExists = await _context.Unit_Types.AnyAsync(u => u.UnitName.ToLower() == unitName.ToLower());
                                if (unitExists)
                                    return Ok(new { response = $"❌ '{unitName}' birim tipi zaten mevcut." });

                                await _mediator.Send(new CreateUnit_TypesCommand { UnitName = unitName });
                                responseText = $"✅ '{unitName}' birim tipi başarıyla eklendi.";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"EXCEPTION in Unit_Type creation: {ex.Message}");
                                responseText = $"❌ Birim tipi oluşturulurken bir hata oluştu: {ex.InnerException?.Message ?? ex.Message}";
                            }
                            #endregion
                            break;

                        case "stock_movement":
                            #region Create Stock Movement
                            try
                            {
                                if (!p.TryGetValue("productName", out var smProdNameEl) || string.IsNullOrEmpty(smProdNameEl.GetString()))
                                    return Ok(new { response = "❌ Ürün adı zorunludur." });
                                if (!p.TryGetValue("quantity", out var smQtyEl))
                                    return Ok(new { response = "❌ Miktar zorunludur." });
                                if (!p.TryGetValue("moveTypeName", out var smTypeEl) || string.IsNullOrEmpty(smTypeEl.GetString()))
                                    return Ok(new { response = "❌ Hareket tipi (Giriş/Çıkış) zorunludur." });

                                var prodName = smProdNameEl.GetString();
                                var quantity = smQtyEl.GetInt32();
                                var typeName = smTypeEl.GetString();
                                var isNewInventory = p.TryGetValue("isNewInventory", out var ini) && ini.GetBoolean();

                                var product = await _context.Products.FirstOrDefaultAsync(pr => pr.ProductName.ToLower() == prodName.ToLower());
                                if (product == null)
                                    return Ok(new { response = $"❌ '{prodName}' ürünü sistemde bulunamadı. Önce ürünü tanımlamalısınız." });

                                var moveType = await _context.Move_Types.FirstOrDefaultAsync(mt => mt.MoveType.ToLower() == typeName.ToLower());
                                if (moveType == null)
                                    return Ok(new { response = $"❌ '{typeName}' adında bir hareket tipi bulunamadı. (Örn: Giriş, Çıkış)" });

                                Guid supplierId = Guid.Empty;
                                if (p.TryGetValue("supplierName", out var supNameEl) && !string.IsNullOrEmpty(supNameEl.GetString()))
                                {
                                    var sName = supNameEl.GetString();
                                    var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.ToLower() == sName.ToLower());
                                    if (supplier != null) supplierId = supplier.Id;
                                }

                                var smCommand = new CreateStock_MovementsCommand
                                {
                                    ProductId = product.Id,
                                    Quantity = quantity,
                                    MoveTypeId = moveType.Id,
                                    SupplierId = supplierId,
                                    Description = p.TryGetValue("description", out var desc) ? desc.GetString() : null,
                                    IsNewInventory = isNewInventory,
                                    UserId = _currentUserService.UserId
                                };

                                if (isNewInventory)
                                {
                                    smCommand.PurchasePrice = p.TryGetValue("purchasePrice", out var pp) ? (float)pp.GetDecimal() : 0f;
                                    smCommand.SalePrice = p.TryGetValue("salePrice", out var sp) ? (float)sp.GetDecimal() : 0f;
                                    smCommand.CriticalStockQuantity = p.TryGetValue("criticalStockQuantity", out var csq) ? csq.GetInt32() : 0;
                                }
                                else
                                {
                                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == product.Id);
                                    if (inventory != null)
                                        smCommand.InventoryId = inventory.Id;
                                    else if (typeName.ToLower() == "giriş")
                                    {
                                         return Ok(new { response = $"❌ '{prodName}' için envanter kaydı bulunamadı. Lütfen 'yeni envanter oluştur' diyerek fiyat bilgilerini girin." });
                                    }
                                }

                                await _mediator.Send(smCommand);
                                responseText = $"✅ Stok hareketi başarıyla kaydedildi: {prodName} ({typeName}) - {quantity} Adet";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"EXCEPTION in Stock Movement: {ex.Message}");
                                responseText = $"❌ Stok hareketi işlenirken hata: {ex.InnerException?.Message ?? ex.Message}";
                            }
                            #endregion
                            break;

                        case "inventory":
                            #region Create Inventory Directly
                            try
                            {
                                if (!p.TryGetValue("productName", out var invProdNameEl) || string.IsNullOrEmpty(invProdNameEl.GetString()))
                                    return Ok(new { response = "❌ Ürün adı zorunludur." });
                                
                                var invProdName = invProdNameEl.GetString();
                                var product = await _context.Products.FirstOrDefaultAsync(pr => pr.ProductName.ToLower() == invProdName.ToLower());
                                if (product == null)
                                    return Ok(new { response = $"❌ '{invProdName}' ürünü sistemde bulunamadı." });

                                var invExists = await _context.Inventories.AnyAsync(i => i.ProductId == product.Id);
                                if (invExists)
                                    return Ok(new { response = $"❌ '{invProdName}' için zaten bir envanter kaydı var. Güncelleme yapabilirsiniz." });

                                var invCommand = new CreateInventoriesCommand
                                {
                                    ProductId = product.Id,
                                    PurchasePrice = p.TryGetValue("purchasePrice", out var pp) ? (float)pp.GetDecimal() : 0f,
                                    SalePrice = p.TryGetValue("salePrice", out var sp) ? (float)sp.GetDecimal() : 0f,
                                    CriticalStockQuantity = p.TryGetValue("criticalStock", out var csq) ? csq.GetInt32() : 0,
                                    Quantity = 0
                                };

                                await _mediator.Send(invCommand);
                                responseText = $"✅ '{invProdName}' envantere eklendi.";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"EXCEPTION in Inventory create: {ex.Message}");
                                responseText = $"❌ Envanter oluşturulurken hata: {ex.InnerException?.Message ?? ex.Message}";
                            }
                            #endregion
                            break;
                    }
                }
                // ==================== GET (LIST) ====================
                else if (op == "get")
                {
                    switch (entity)
                    {
                        case "products":
                            #region Get Products
                            var prods = await _context.Products
                                .Include(p => p.Category)
                                .Include(p => p.UnitType)
                                .Where(p => p.IsActive)
                                .Take(10)
                                .ToListAsync();

                            if (prods.Any())
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine($"📦 **Ürün Listesi** (Son 10):\n");
                                foreach (var pr in prods)
                                {
                                    sb.AppendLine($"- **{pr.ProductName}** ({pr.Category?.CategoryName}) | Barkod: {pr.Barcode}");
                                }
                                responseText = sb.ToString();
                            }
                            else responseText = "Sistemde kayıtlı ürün bulunamadı.";
                            #endregion
                            break;

                        case "suppliers":
                            #region Get Suppliers
                            var sups = await _context.Suppliers.Where(s => s.IsActive).Take(15)
                                .Select(s => new { s.SupplierName, s.ContactPerson, s.PhoneNumber }).ToListAsync();

                            if (sups.Any())
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine($"🏢 **Tedarikçiler** ({sups.Count} adet):\n");
                                int i = 1;
                                foreach (var s in sups)
                                {
                                    sb.AppendLine($"{i}. **{s.SupplierName}**");
                                    sb.AppendLine($"   Yetkili: {s.ContactPerson ?? "Belirtilmemiş"} | Tel: {s.PhoneNumber ?? "Yok"}\n");
                                    i++;
                                }
                                responseText = sb.ToString();
                            }
                            else responseText = "Sistemde kayıtlı tedarikçi bulunamadı.";
                            #endregion
                            break;

                        case "unit_types":
                            #region Get Unit_Types
                            var units = await _context.Unit_Types.Where(u => u.IsActive).Select(u => u.UnitName).ToListAsync();
                            if (units.Any())
                            {
                                responseText = $"📏 Kayıtlı Birim Tipleri:\n- {string.Join("\n-", units)}";
                            }
                            else
                            {
                                responseText = "Sistemde kayıtlı birim tipi bulunamadı.";
                            }
                            #endregion
                            break;
                        
                        case "inventories":
                            #region Get Inventories
                            var query = _context.Inventories
                                .Include(i => i.Product)
                                .Where(i => i.IsActive);

                            if (cmd.Filters != null && cmd.Filters.IsBelowCriticalStock == true)
                            {
                                query = query.Where(i => i.Quantity <= i.CriticalStockQuantity);
                                responseText = "⚠️ **Kritik Stok Seviyesinin Altındaki Ürünler:**\n";
                            }
                            else
                            {
                                responseText = "📊 **Envanter Durumu:**\n";
                            }

                            var invs = await query.Take(15).ToListAsync();
                            if (invs.Any())
                            {
                                var sb = new StringBuilder(responseText);
                                foreach (var i in invs)
                                {
                                    sb.AppendLine($"- **{i.Product?.ProductName}**: {i.Quantity} Adet (Kritik: {i.CriticalStockQuantity})");
                                }
                                responseText = sb.ToString();
                            }
                            else responseText += "Kayıt bulunamadı.";
                            #endregion
                            break;

                        case "stock_movements":
                            #region Get Stock Movements
                            var smQuery = _context.Stock_Movements
                                .Include(sm => sm.Inventory).ThenInclude(i => i.Product)
                                .Include(sm => sm.MoveType)
                                .Where(sm => sm.IsActive)
                                .OrderByDescending(sm => sm.CreatedAt)
                                .AsQueryable();

                            string title = "📋 **Son Stok Hareketleri:**\n";

                            if (cmd.Filters != null)
                            {
                                if (!string.IsNullOrEmpty(cmd.Filters.DateRange) && cmd.Filters.DateRange.ToLower() == "today")
                                {
                                    var today = DateTime.UtcNow.Date;
                                    smQuery = smQuery.Where(sm => sm.CreatedAt >= today);
                                    title = "📅 **Bugünkü Stok Hareketleri:**\n";
                                }
                                if (!string.IsNullOrEmpty(cmd.Filters.ProductName))
                                {
                                    smQuery = smQuery.Where(sm => sm.Inventory.Product.ProductName.ToLower().Contains(cmd.Filters.ProductName.ToLower()));
                                    title = $"🔍 **'{cmd.Filters.ProductName}' İçin Hareketler:**\n";
                                }
                            }

                            var moves = await smQuery.Take(10).ToListAsync();
                            if (moves.Any())
                            {
                                var sb = new StringBuilder(title);
                                foreach (var m in moves)
                                {
                                    string icon = m.MoveType?.MoveType == "Giriş" ? "📥" : "📤";
                                    sb.AppendLine($"{icon} {m.CreatedAt:dd.MM HH:mm} - **{m.Inventory?.Product?.ProductName}**: {m.Quantity} Adet ({m.MoveType?.MoveType})");
                                }
                                responseText = sb.ToString();
                            }
                            else responseText = "Aradığınız kriterlere uygun stok hareketi bulunamadı.";
                            #endregion
                            break;
                    }
                }
                // ==================== UPDATE ====================
                else if (op == "update")
                {
                    if (!cmd.Payload.HasValue)
                        return Ok(new { response = "❌ Güncellenecek bilgiyi (payload) belirtmediniz." });
                    if (cmd.Filters == null || string.IsNullOrEmpty(cmd.Filters.Name) && string.IsNullOrEmpty(cmd.Filters.ProductName))
                        return Ok(new { response = "❌ Hangi kaydı güncelleyeceğinizi belirtmediniz." });

                    var payloadRaw = cmd.Payload.Value.GetRawText();
                    var p = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadRaw);
                    
                    switch (entity)
                    {
                        case "supplier":
                            #region Update Supplier
                            try
                            {
                                var filterName = cmd.Filters.Name.ToLower();
                                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.ToLower() == filterName);
                                if (supplier == null)
                                    return Ok(new { response = $"❌ '{cmd.Filters.Name}' adında bir tedarikçi bulunamadı." });

                                var updateCmd = new UpdateSuppliersCommand
                                {
                                    Id = supplier.Id,
                                    SupplierName = p.TryGetValue("supplierName", out var sn) ? sn.GetString() : supplier.SupplierName,
                                    ContactPerson = p.TryGetValue("contactPerson", out var cp) ? cp.GetString() : supplier.ContactPerson,
                                    PhoneNumber = p.TryGetValue("phoneNumber", out var ph) ? ph.GetString() : supplier.PhoneNumber,
                                    Email = p.TryGetValue("email", out var em) ? em.GetString() : supplier.Email,
                                    Address = p.TryGetValue("address", out var ad) ? ad.GetString() : supplier.Address,
                                    IsActive = p.TryGetValue("isActive", out var ia) ? ia.GetBoolean() : supplier.IsActive,
                                };
                                await _mediator.Send(updateCmd);
                                responseText = $"✅ '{supplier.SupplierName}' tedarikçisi güncellendi.";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"EXCEPTION in Supplier update: {ex.Message}");
                                responseText = $"❌ Tedarikçi güncellenirken bir hata oluştu: {ex.InnerException?.Message ?? ex.Message}";
                            }
                            #endregion
                            break;

                        case "product":
                            #region Update Product
                            try
                            {
                                var filterName = cmd.Filters.Name?.ToLower() ?? cmd.Filters.ProductName?.ToLower();
                                var product = await _context.Products.FirstOrDefaultAsync(pr => pr.ProductName.ToLower() == filterName);
                                if (product == null)
                                    return Ok(new { response = $"❌ '{filterName}' adında bir ürün bulunamadı." });
                                
                                if (p.TryGetValue("productName", out var newName)) product.ProductName = newName.GetString();
                                
                                _context.Products.Update(product);
                                await _context.SaveChangesAsync();

                                responseText = "✅ Ürün güncellendi.";
                            }
                            catch (Exception ex)
                            {
                                responseText = $"❌ Ürün güncellenirken hata: {ex.Message}";
                            }
                            #endregion
                            break;

                         case "inventory":
                            #region Update Inventory
                            try
                            {
                                var filterName = cmd.Filters.Name?.ToLower() ?? cmd.Filters.ProductName?.ToLower();
                                var product = await _context.Products.FirstOrDefaultAsync(pr => pr.ProductName.ToLower() == filterName);
                                if (product == null)
                                    return Ok(new { response = $"❌ '{filterName}' ürünü bulunamadı." });

                                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == product.Id);
                                if (inventory == null)
                                    return Ok(new { response = $"❌ '{filterName}' için envanter kaydı yok." });

                                if (p.TryGetValue("salePrice", out var sp)) inventory.SalePrice = (float)sp.GetDecimal();
                                if (p.TryGetValue("purchasePrice", out var pp)) inventory.PurchasePrice = (float)pp.GetDecimal();
                                if (p.TryGetValue("criticalStock", out var cs)) inventory.CriticalStockQuantity = cs.GetInt32();

                                _context.Inventories.Update(inventory);
                                await _context.SaveChangesAsync();
                                responseText = $"✅ '{filterName}' envanter bilgileri güncellendi.";
                            }
                            catch (Exception ex)
                            {
                                responseText = $"❌ Envanter güncellenirken hata: {ex.Message}";
                            }
                            #endregion
                            break;
                    }
                }
                // ==================== DELETE ====================
                else if (op == "delete")
                {
                    if (cmd.Filters == null || (string.IsNullOrEmpty(cmd.Filters.Name) && string.IsNullOrEmpty(cmd.Filters.ProductName)))
                        return Ok(new { response = "❌ Hangi kaydı sileceğinizi belirtmediniz." });

                    var filterName = cmd.Filters.Name?.ToLower() ?? cmd.Filters.ProductName?.ToLower();

                    switch (entity)
                    {
                        case "supplier":
                            #region Delete Supplier
                            try
                            {
                                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.ToLower() == filterName);
                                if (supplier == null)
                                    return Ok(new { response = $"❌ '{filterName}' adında bir tedarikçi bulunamadı." });

                                await _mediator.Send(new DeleteSuppliersCommand(supplier.Id));
                                responseText = $"✅ '{supplier.SupplierName}' tedarikçisi silindi.";
                            }
                            catch (Exception ex)
                            {
                                responseText = $"❌ Tedarikçi silinirken hata: {ex.InnerException?.Message ?? ex.Message}";
                            }
                            #endregion
                            break;

                         case "product":
                            #region Delete Product
                            try
                            {
                                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductName.ToLower() == filterName);
                                if (product == null)
                                    return Ok(new { response = $"❌ '{filterName}' ürünü bulunamadı." });

                                try { 
                                    product.IsActive = false; // Soft delete
                                    _context.Products.Update(product);
                                    await _context.SaveChangesAsync();
                                    responseText = $"✅ '{product.ProductName}' ürünü pasif yapıldı (Soft Delete).";
                                } 
                                catch { 
                                     responseText = "❌ Ürün silme komutu çalıştırılamadı.";
                                }
                            }
                            catch (Exception ex)
                            {
                                responseText = $"❌ Ürün silinirken hata: {ex.Message}";
                            }
                            #endregion
                            break;
                        
                        case "inventory":
                             #region Delete Inventory
                            try
                            {
                                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductName.ToLower() == filterName);
                                if (product == null) return Ok(new { response = $"❌ '{filterName}' ürünü bulunamadı." });

                                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == product.Id);
                                if(inventory == null) return Ok(new { response = "Envanter kaydı zaten yok." });

                                inventory.IsActive = false;
                                _context.Inventories.Update(inventory);
                                await _context.SaveChangesAsync();
                                responseText = $"✅ '{product.ProductName}' envanterden kaldırıldı.";
                            }
                            catch(Exception ex)
                            {
                                responseText = $"❌ Envanter silinirken hata: {ex.Message}";
                            }
                            #endregion
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                responseText = $"Hata: {ex.Message}";
            }

            return Ok(new { response = responseText });
        }
    }
}