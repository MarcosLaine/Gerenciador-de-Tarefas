using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LembretesApi.Models;

namespace LembretesApi.Data
{
    public class AppDbContext : IdentityDbContext<Usuario>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Lembrete> Lembretes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurar relacionamento Usuario -> Lembretes
            builder.Entity<Lembrete>()
                .HasOne(l => l.Usuario)
                .WithMany(u => u.Lembretes)
                .HasForeignKey(l => l.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices para melhor performance
            builder.Entity<Lembrete>()
                .HasIndex(l => l.UsuarioId);

            builder.Entity<Lembrete>()
                .HasIndex(l => l.Data);
        }
    }
}

