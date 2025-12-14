using Inventory_Management.Domain.Common;
using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace Inventory_Management.Persistance.Context
{
    public class Inventory_Management_Context : DbContext
    {
        // Kullanıcı servisi (Hangi şirkette olduğunu bilmek için)
        private readonly ICurrentUserService _currentUserService;

        public Inventory_Management_Context(
            DbContextOptions<Inventory_Management_Context> options,
            ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;
        }

        // ==========================================
        // DbSet Tanımlamaları (Mevcut Kodlarınız)
        // ==========================================
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Companies> Companies { get; set; }
        public DbSet<Delivery_Rules> Delivery_Rules { get; set; }
        public DbSet<Inventories> Inventories { get; set; }
        public DbSet<Move_Types> Move_Types { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Stock_Movements> Stock_Movements { get; set; }
        public DbSet<Suppliers> Suppliers { get; set; }
        public DbSet<Unit_Types> Unit_Types { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<UsersRoles> UsersRoles { get; set; }

        // ==========================================
        // Model Konfigürasyonu
        // ==========================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Stock_Movements>()
                .HasOne(sm => sm.Company)
                .WithMany(c => c.Stock_Movements)
                .HasForeignKey(sm => sm.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UsersRoles>()
                .HasOne(ur => ur.Company)
                .WithMany(c => c.UsersRoles) // Company tarafında UsersRoles listesi yoksa boş bırakın
                .HasForeignKey(ur => ur.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UsersRoles>().HasQueryFilter(ur =>
                _currentUserService.CompanyId == null ||
                ur.CompanyId == _currentUserService.CompanyId);

            modelBuilder.Entity<Inventories>().HasQueryFilter(x => x.IsActive);

            base.OnModelCreating(modelBuilder);

            // --- OTOMATİK FİLTRELEME MANTIĞI ---
            // Sistemdeki tüm tabloları tek tek kontrol et
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Eğer tablo IHasCompany arayüzüne sahipse (Yani şirket ID'si varsa)
                if (typeof(IHasCompany).IsAssignableFrom(entityType.ClrType))
                {
                    // O tabloya özel filtreleme metodunu çağır
                    var method = typeof(Inventory_Management_Context)
                        .GetMethod(nameof(SetGlobalQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.MakeGenericMethod(entityType.ClrType);

                    method?.Invoke(this, new object[] { modelBuilder });
                }
            }

            // Şirketler tablosu için özel filtre (Kendi şirketini görsün)
            modelBuilder.Entity<Inventory_Management.Domain.Entities.Companies>().HasQueryFilter(c =>
                _currentUserService.CompanyId == null || c.Id == _currentUserService.CompanyId);
        }

        private void SetGlobalQueryFilter<T>(ModelBuilder modelBuilder) where T : class, IHasCompany // IHasCompany eklendi
        {
            // Şart: (Admin ise/Giriş yoksa) VEYA (Kaydın CompanyId'si == Kullanıcının CompanyId'si)
            modelBuilder.Entity<T>().HasQueryFilter(e =>
                _currentUserService.CompanyId == null ||
                e.CompanyId == _currentUserService.CompanyId); // EF.Property kullanımına gerek kalmadı çünkü T zaten IHasCompany
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        // IsActive, BaseEntity'de varsayılan olarak true ayarlandığı için burada tekrar ayarlamaya gerek yok.

                        // Eğer varlık aynı zamanda IHasCompany arayüzünü uyguluyorsa ve kullanıcı oturum açmışsa, CompanyId'yi ayarla.
                        if (entry.Entity is IHasCompany hasCompanyEntity && _currentUserService.CompanyId.HasValue)
                        {
                            // Yeni eklenen kaydın CompanyId'si boşsa, mevcut kullanıcınınkiyle doldur.
                            if (hasCompanyEntity.CompanyId == Guid.Empty)
                            {
                                hasCompanyEntity.CompanyId = _currentUserService.CompanyId.Value;
                            }
                        }
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}