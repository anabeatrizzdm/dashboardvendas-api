using DashboardVendas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardVendas.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Cliente>()
            .HasMany(c => c.Vendas)
            .WithOne(v => v.Cliente!)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Venda>()
            .HasMany(v => v.Itens)
            .WithOne(i => i.Venda!)
            .HasForeignKey(i => i.VendaId);

        modelBuilder.Entity<Produto>()
            .Property(p => p.Preco)
            .HasColumnType("numeric(10,2)");

        modelBuilder.Entity<Venda>()
            .Property(v => v.ValorTotal)
            .HasColumnType("numeric(10,2)");

        modelBuilder.Entity<ItemVenda>()
            .Property(i => i.PrecoUnitario)
            .HasColumnType("numeric(10,2)");

        modelBuilder.Entity<ItemVenda>()
            .Property(i => i.Subtotal)
            .HasColumnType("numeric(10,2)");
    }
}