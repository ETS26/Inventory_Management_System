using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory_Management.Persistance.Context
{
    public class Inventory_Management_Context : DbContext
    {
        public Inventory_Management_Context(DbContextOptions<Inventory_Management_Context> options) : base(options)
        {
        }

        public DbSet<Categories> Categories { get; set; }
        public DbSet<Companies> Companies { get; set; }
        public DbSet<Delivery_Rules> Delivery_Rules { get; set; }
        public DbSet<Inventories> Inventories { get; set; }
        public DbSet<Move_Types> Move_Types { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Stock_Movements> Stock_Movements { get; set; }
        public DbSet<Suppliers_Delivery> Suppliers_Deliveries { get; set; }
        public DbSet<Suppliers> Suppliers { get; set; }
        public DbSet<Unit_Types> Unit_Types { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<UsersRoles> UsersRoles { get; set; }

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
        }
    }
}
