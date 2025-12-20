
using Inventory_Management.Domain.Common;
using Inventory_Management.Persistance.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Inventory_Management.Persistance.Context
{
    // Bu sınıf, SADECE "Add-Migration" veya "Update-Database" komutları çalıştığında devreye girer.
    // Program.cs'i çalıştırmadan veritabanı ayarlarını burdan alır.
    public class InventoryDbContextFactory : IDesignTimeDbContextFactory<Inventory_Management_Context>
    {
        public Inventory_Management_Context CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<Inventory_Management_Context>();

            // ⚠️ ÖNEMLİ: Buraya appsettings.json içindeki bağlantı cümlenizi (Connection String) yapıştırın.
            // Migration sırasında appsettings.json okunmayabilir, o yüzden buraya elle yazıyoruz.
            var connectionString = "Server=ETS;initial Catalog=Inventory_Management_DB;integrated Security=True;TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(connectionString);

            // Migration yaparken "Giriş Yapan Kullanıcı" olmadığı için
            // buraya sahte (Dummy) bir servis veriyoruz.
            return new Inventory_Management_Context(optionsBuilder.Options, new DesignTimeCurrentUserService());
        }
    }

    // Sahte Kullanıcı Servisi (Sadece Migration Hatasını Önlemek İçin)
    public class DesignTimeCurrentUserService : ICurrentUserService
    {
        // Migration yaparken şirket veya kullanıcı önemli değildir, null dönebiliriz.
        public Guid? CompanyId => null;
        public Guid UserId => Guid.Empty;
    }
}