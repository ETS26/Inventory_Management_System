# Inventory Management System

Bu depo, şirketlerin envanter yönetimini kolaylaştırmak için geliştirilmiş bir uygulama olan "Inventory Management System" (Envanter Yönetim Sistemi) projesini içerir.

## Genel Bakış

- Dil / Teknolojiler: C# (arka uç), JavaScript, HTML, CSS (ön yüz)
- Amaç: Ürünleri, stok seviyelerini, tedarikçileri ve envanter hareketlerini takip etmek.
- Mevcut durum: Kod tabanı C# ağırlıklı olup istemci tarafında JavaScript/HTML/CSS kullanımı bulunmaktadır.

> Not: Bu README genel kurulum ve kullanım talimatları içerir. Projeye özgü dosya isimleri veya komutlar (ör. çözüm dosyası adı, veritabanı sağlayıcısı) farklı olabilir — lütfen gerektiğinde README içeriğini proje dosyalarına göre güncelleyin.

## Özellikler

- Ürün ekleme / düzenleme / silme
- Stok giriş/çıkış hareketleri
- Tedarikçi ve kategori yönetimi
- Basit raporlama ve arama

## Gereksinimler

- .NET SDK (genellikle .NET 6 veya üstü) — projenin kökünde hangi sürümün kullanıldığını kontrol edin.
- (Varsa) Node.js ve npm/yarn — eğer frontend ayrı bir paket olarak yönetiliyorsa.
- Veritabanı: (örn. SQL Server, SQLite, PostgreSQL) — proje yapılandırmasına göre bağlantı dizesini ayarlayın.

## Hızlı Başlangıç (Yerel)

1. Depoyu klonlayın:

   git clone https://github.com/ETS26/Inventory_Management_System.git
   cd Inventory_Management_System

2. Çözüm dosyasını açın veya projeyi restore edin:

   # .NET araçlarıyla
   dotnet restore
   dotnet build

3. Veritabanı yapılandırması

- appsettings.json veya uygun konfigürasyon dosyasında ConnectionString (bağlantı dizesi) ayarlarını güncelleyin.
- Eğer Entity Framework Core kullanılıyorsa, database migration uygulamak için örnek:

   dotnet ef database update

(Not: `dotnet ef` komutunu kullanmak için EF Core araçlarının kurulu olması gerekir: `dotnet tool install --global dotnet-ef`)

4. Uygulamayı çalıştırın:

   dotnet run --project <ProjeKlasoru>

Varsa, frontend ayrı dizindeyse:

   cd frontend
   npm install
   npm start

5. Tarayıcıda uygulamayı açın: http://localhost:5000 veya proje yapılandırmasında belirtilen URL

## Testler

- Proje test projeleri içeriyorsa çalıştırmak için:

  dotnet test

## Katkıda Bulunma

Katkılar memnuniyetle karşılanır. Lütfen bir issue açın veya doğrudan pull request gönderin. Kod stili, commit mesajları ve branch stratejisi için proje içindeki CONTRIBUTING.md dosyasını inceleyin (varsa).

## Yayınlama

- Ürünü üretim ortamına taşıma, CI/CD yapılandırmaları ve ortam değişkenleri için proje sahibi veya README içindeki yayımlama rehberini takip edin.

## Lisans

Bu depo için lisans bilgisi eklenmemişse, lisans dosyası eklemeyi düşünün (örn. MIT, Apache-2.0). Eğer lisans zaten ekliyse, burayı güncelleyin.

## İletişim

Sorular veya geri bildirimler için proje sahibi ile iletişime geçin.

---

README dosyasını proje dosyalarına göre özelleştirmeniz önerilir: çözüm/proje adları, kullanılan .NET sürümü, veritabanı sağlayıcısı ve çalıştırma portu gibi bilgiler güncellenmelidir.
