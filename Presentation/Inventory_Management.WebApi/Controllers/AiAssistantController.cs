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
using Inventory_Management.Application.Features.Results.ProductsResult;

namespace Inventory_Management.WebApi.Controllers
{
    // --- MODELS ---
    public class UserQueryModel
    {
        public string Query { get; set; }
        public string? SessionId { get; set; }
    }

    public class ConversationMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class SessionContext
    {
        public string CurrentOperation { get; set; }
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
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("quantityFilterType")] public string QuantityFilterType { get; set; }
        [JsonPropertyName("dateRange")] public string DateRange { get; set; }
        [JsonPropertyName("take")] public int? Take { get; set; }
        [JsonPropertyName("isBelowCriticalStock")] public bool? IsBelowCriticalStock { get; set; }
        [JsonPropertyName("expirationDate")] public string ExpirationDate { get; set; }
        [JsonPropertyName("isActive")] public bool? IsActive { get; set; }
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

        // Session storage
        private static readonly Dictionary<string, SessionContext> _sessions = new();

        public AiAssistantController(IHttpClientFactory httpClientFactory, IMediator mediator, IConfiguration configuration, Inventory_Management_Context context)
        {
            _httpClientFactory = httpClientFactory;
            _mediator = mediator;
            _configuration = configuration;
            _context = context;
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
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemma-3-27b-it:generateContent?key={apiKey}";

            var systemPrompt = @"Sen bir envanter yönetim asistanısın. Kullanıcı komutlarını JSON'a çevir veya soru sor.

# TEMEL KURALLAR
1. Kullanıcı 'kaç', 'adet', 'sayı', 'toplam' diyorsa → CALCULATE
2. Kullanıcı 'listele', 'göster', 'getir' diyorsa → LIST
3. Kullanıcı 'ekle', 'yeni', 'oluştur' diyorsa → CREATE (eksik bilgi varsa sor!)
4. Kullanıcı 'güncelle', 'değiştir', 'düzenle' diyorsa → UPDATE
5. Kullanıcı 'sil', 'kaldır' diyorsa → DELETE
6. Kullanıcı 'aktif et', 'geri getir' diyorsa → ACTIVATE
7. Anlamadıysan sorunu anlamaya çalış, JSON oluşturma.

# ÜRÜN İŞLEMLERİ İÇİN ALANLAR (PAYLOAD)
- productName (string) [Zorunlu]
- barcode (string) [Zorunlu]
- categoryName (string) [Kategori Adı]
- unitTypeName (string) [Birim Adı]
- imageUrl (string)
- description (string)

# ÖRNEKLER
Kullanıcı: ""Klavye ekle, barkodu 123, kategorisi Elektronik""
→ { ""operation"": ""CREATE"", ""entity"": ""Product"", ""payload"": { ""productName"": ""Klavye"", ""barcode"": ""123"", ""categoryName"": ""Elektronik"" } }
";

            var conversationHistory = new StringBuilder();
            var recentMessages = session.History.TakeLast(4).ToList();

            if (recentMessages.Any())
            {
                conversationHistory.AppendLine("\n# GEÇMİŞ KONUŞMA:");
                foreach (var msg in recentMessages)
                {
                    conversationHistory.AppendLine($"- {(msg.Role == "user" ? "Kullanıcı" : "Sen")}: {msg.Content}");
                }
            }

            var fullPrompt = systemPrompt + conversationHistory.ToString() + $"\n\n# ŞİMDİ\nKullanıcı: {userQuery}\n\nEğer CREATE işlemindeysen ve tüm zorunlu bilgiler toplanmışsa JSON döndür. Değilse eksik bilgiyi sor.";

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
                var errorMsg = ex.InnerException != null ? $"{ex.Message} | Detay: {ex.InnerException.Message}" : ex.Message;
                return StatusCode(500, new { response = $"Bir sunucu hatası oluştu: {errorMsg}", sessionId });
            }
        }

        private async Task<IActionResult> ProcessAiCommand(string llmJson, SessionContext session)
        {
            string responseText = "Bu komutu anlayamadım.";
            try
            {
                var command = JsonSerializer.Deserialize<AiCommand>(llmJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (command == null || string.IsNullOrEmpty(command.Operation))
                {
                    return Ok(new { response = "Komutu anlayamadım. Lütfen tekrar deneyin." });
                }

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

                string operation = command.Operation?.Trim().ToLowerInvariant();
                string entity = command.Entity?.Trim().ToLowerInvariant();

                if (operation == "create")
                {
                    switch (entity)
                    {
                        case "product":
                            if (command.Payload.HasValue)
                            {
                                try
                                {
                                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());

                                    string productName = payload.ContainsKey("productName") ? payload["productName"].GetString() : null;
                                    string barcode = payload.ContainsKey("barcode") ? payload["barcode"].GetString() : null;

                                    // ImageURL kontrolü: Eğer null gelirse varsayılan bir değer ata
                                    string imageUrl = payload.ContainsKey("imageUrl") && payload["imageUrl"].ValueKind != JsonValueKind.Null
                                        ? payload["imageUrl"].GetString()
                                        : "https://placehold.co/600x400?text=No+Image";

                                    string description = payload.ContainsKey("description") ? payload["description"].GetString() : null;

                                    string categoryName = payload.ContainsKey("categoryName") ? payload["categoryName"].GetString() : null;
                                    string unitTypeName = payload.ContainsKey("unitTypeName") ? payload["unitTypeName"].GetString() : null;

                                    if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(barcode))
                                    {
                                        responseText = "Ürün oluşturmak için 'Ürün Adı' ve 'Barkod' gereklidir.";
                                        break;
                                    }

                                    var createCommand = new CreateProductsCommand
                                    {
                                        ProductName = productName,
                                        Barcode = barcode,
                                        Description = description,
                                        ImageURL = imageUrl // Artık asla null olmayacak
                                    };

                                    // --- KATEGORİ İSMİNDEN ID BULMA ---
                                    if (!string.IsNullOrEmpty(categoryName))
                                    {
                                        var category = await _context.Categories
                                            .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == categoryName.ToLower());

                                        if (category != null)
                                            createCommand.CategoryId = category.Id;
                                        else
                                            createCommand.CategoryId = (await _context.Categories.FirstOrDefaultAsync())?.Id ?? Guid.Empty;
                                    }
                                    else
                                    {
                                        createCommand.CategoryId = (await _context.Categories.FirstOrDefaultAsync())?.Id ?? Guid.Empty;
                                    }

                                    // --- BİRİM İSMİNDEN ID BULMA ---
                                    if (!string.IsNullOrEmpty(unitTypeName))
                                    {
                                        // UnitType tablosunda isim alanı 'Name' veya 'UnitName' olarak varsayıldı.
                                        // Veritabanınızda bu alanın adı neyse (Örn: UnitType, Name, Description) onu kullanın.
                                        // Aşağıda genelde kullanılan 'Name' kullanıldı.
                                        var unit = await _context.Unit_Types
                                            .FirstOrDefaultAsync(u => u.UnitName.ToLower() == unitTypeName.ToLower());
                                        if (unit != null)
                                            createCommand.UnitTypeId = unit.Id;
                                        else
                                            createCommand.UnitTypeId = (await _context.Unit_Types.FirstOrDefaultAsync())?.Id ?? Guid.Empty;
                                    }
                                    else
                                    {
                                        createCommand.UnitTypeId = (await _context.Unit_Types.FirstOrDefaultAsync())?.Id ?? Guid.Empty;
                                    }

                                    await _mediator.Send(createCommand);
                                    responseText = $"✅ '{productName}' başarıyla eklendi! (Barkod: {barcode})";
                                }
                                catch (Exception ex)
                                {
                                    string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                                    responseText = $"❌ Ürün eklenirken hata: {msg}";
                                }
                            }
                            break;

                        case "category":
                            if (command.Payload.HasValue)
                            {
                                try
                                {
                                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());
                                    string catName = payload.ContainsKey("categoryName") ? payload["categoryName"].GetString() : null;

                                    if (string.IsNullOrWhiteSpace(catName))
                                    {
                                        responseText = "Kategori adı gereklidir.";
                                        break;
                                    }

                                    var category = new Categories
                                    {
                                        Id = Guid.NewGuid(),
                                        CategoryName = catName,
                                        Description = payload.ContainsKey("description") ? payload["description"].GetString() : null,
                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    _context.Categories.Add(category);
                                    await _context.SaveChangesAsync();
                                    responseText = $"✅ '{catName}' kategorisi oluşturuldu!";
                                }
                                catch (Exception ex)
                                {
                                    responseText = $"❌ Hata: {ex.Message}";
                                }
                            }
                            break;
                    }
                }
                else if (operation == "update")
                {
                    switch (entity)
                    {
                        case "product":
                            if (command.Payload.HasValue && command.Filters != null)
                            {
                                try
                                {
                                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(command.Payload.Value.GetRawText());

                                    Products product = null;
                                    if (!string.IsNullOrEmpty(command.Filters.Id))
                                        product = await _context.Products.FindAsync(Guid.Parse(command.Filters.Id));
                                    else if (!string.IsNullOrEmpty(command.Filters.Name))
                                        product = await _context.Products.FirstOrDefaultAsync(p => p.ProductName.ToLower() == command.Filters.Name.ToLower());

                                    if (product == null)
                                    {
                                        responseText = "Güncellenecek ürün bulunamadı.";
                                        break;
                                    }

                                    var updateCommand = new UpdateProductsCommand
                                    {
                                        Id = product.Id,
                                        ProductName = payload.ContainsKey("productName") && payload["productName"].ValueKind != JsonValueKind.Null ? payload["productName"].GetString() : product.ProductName,
                                        Barcode = payload.ContainsKey("barcode") && payload["barcode"].ValueKind != JsonValueKind.Null ? payload["barcode"].GetString() : product.Barcode,

                                        // ImageURL Update sırasında da null olmamalı
                                        ImageURL = payload.ContainsKey("imageUrl") && payload["imageUrl"].ValueKind != JsonValueKind.Null
                                            ? payload["imageUrl"].GetString()
                                            : product.ImageURL, // Değiştirilmediyse eskisini koru

                                        Description = payload.ContainsKey("description") && payload["description"].ValueKind != JsonValueKind.Null ? payload["description"].GetString() : product.Description,
                                        IsActive = product.IsActive,

                                        CategoryId = product.CategoryId,
                                        UnitTypeId = product.UnitTypeId
                                    };

                                    if (payload.ContainsKey("categoryName") && payload["categoryName"].ValueKind != JsonValueKind.Null)
                                    {
                                        var catName = payload["categoryName"].GetString();
                                        var cat = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName.ToLower() == catName.ToLower());
                                        if (cat != null) updateCommand.CategoryId = cat.Id;
                                    }

                                    if (payload.ContainsKey("unitTypeName") && payload["unitTypeName"].ValueKind != JsonValueKind.Null)
                                    {
                                        var unitName = payload["unitTypeName"].GetString();
                                        // UnitType tablosunda isim alanı kontrolü
                                        var unit = await _context.Unit_Types.FirstOrDefaultAsync(u => u.UnitName.ToLower() == unitName.ToLower());
                                        if (unit != null) updateCommand.UnitTypeId = unit.Id;
                                    }

                                    await _mediator.Send(updateCommand);
                                    responseText = $"✅ '{updateCommand.ProductName}' güncellendi.";
                                }
                                catch (Exception ex)
                                {
                                    string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                                    responseText = $"❌ Güncelleme hatası: {msg}";
                                }
                            }
                            break;
                    }
                }
                else if (operation == "list")
                {
                    if (entity == "product")
                    {
                        var getProductsQuery = new GetProductsQuery { IsActive = command.Filters?.IsActive };
                        var products = await _mediator.Send(getProductsQuery);
                        var limitedProducts = products.Take(command.Filters?.Take ?? 10).ToList();

                        if (limitedProducts.Any())
                            responseText = $"📦 Ürün Listesi:\n" + string.Join("\n", limitedProducts.Select(p => $"- {p.ProductName} (Barkod: {p.Barcode})"));
                        else
                            responseText = "Ürün bulunamadı.";
                    }
                    else if (entity == "supplier")
                    {
                        var suppliersQuery = _context.Suppliers.AsQueryable();
                        var count = await suppliersQuery.CountAsync();
                        var list = await suppliersQuery.Take(5).ToListAsync();
                        var listStr = string.Join(", ", list.Select(x => x.SupplierName));
                        responseText = $"Sistemde {count} tedarikçi var. İlk 5: {listStr}";
                    }
                    else
                    {
                        responseText = "Listeleme komutu anlaşıldı ancak entity desteklenmiyor.";
                    }
                }
                else if (operation == "delete")
                {
                    if (entity == "product")
                    {
                        if (command.Filters != null)
                        {
                            Products product = null;
                            if (!string.IsNullOrEmpty(command.Filters.Id))
                                product = await _context.Products.FindAsync(Guid.Parse(command.Filters.Id));
                            else if (!string.IsNullOrEmpty(command.Filters.Name))
                                product = await _context.Products.FirstOrDefaultAsync(p => p.ProductName.ToLower() == command.Filters.Name.ToLower());

                            if (product == null)
                            {
                                responseText = "Silinecek ürün bulunamadı.";
                            }
                            else
                            {
                                var deleteCommand = new DeleteProductsCommand(product.Id);
                                try
                                {
                                    await _mediator.Send(deleteCommand);
                                    responseText = $"✅ '{product.ProductName}' başarıyla silindi (pasif yapıldı).";
                                }
                                catch (Exception ex)
                                {
                                    responseText = $"❌ Silme işlemi başarısız: {ex.Message}";
                                }
                            }
                        }
                    }
                }
                else if (operation == "activate")
                {
                    if (entity == "product")
                    {
                        if (command.Filters != null)
                        {
                            Products product = null;
                            if (!string.IsNullOrEmpty(command.Filters.Id))
                                product = await _context.Products.FindAsync(Guid.Parse(command.Filters.Id));
                            else if (!string.IsNullOrEmpty(command.Filters.Name))
                                product = await _context.Products.FirstOrDefaultAsync(p => p.ProductName.ToLower() == command.Filters.Name.ToLower());

                            if (product == null)
                            {
                                responseText = "Aktifleştirilecek ürün bulunamadı.";
                            }
                            else
                            {
                                var activateCommand = new ActivateProductCommand(product.Id);
                                try
                                {
                                    await _mediator.Send(activateCommand);
                                    responseText = $"✅ '{product.ProductName}' başarıyla aktif edildi.";
                                }
                                catch (Exception ex)
                                {
                                    responseText = $"❌ Aktifleştirme hatası: {ex.Message}";
                                }
                            }
                        }
                    }
                }
                else if (operation == "calculate")
                {
                    if (entity == "inventorytotalquantity")
                    {
                        var total = await _context.Inventories.SumAsync(x => x.Quantity);
                        responseText = $"Toplam envanter miktarı: {total}";
                    }
                    else if (entity == "productcount")
                    {
                        var count = await _context.Products.CountAsync();
                        responseText = $"Toplam ürün sayısı: {count}";
                    }
                    else
                    {
                        responseText = "Hesaplama komutu anlaşıldı ancak entity desteklenmiyor.";
                    }
                }
                else
                {
                    responseText = $"Komut işlendi: {command.Operation} {command.Entity}";
                }
            }
            catch (JsonException ex)
            {
                responseText = $"JSON hatası: {ex.Message} | Gelen: {llmJson}";
            }
            catch (Exception ex)
            {
                responseText = $"Hata: {ex.Message}";
            }

            return Ok(new { response = responseText });
        }

        [HttpPost("clear-session")]
        public IActionResult ClearSession([FromBody] string sessionId)
        {
            if (_sessions.ContainsKey(sessionId))
            {
                _sessions.Remove(sessionId);
                return Ok(new { message = "Session temizlendi." });
            }
            return NotFound(new { message = "Session bulunamadı." });
        }
    }
}