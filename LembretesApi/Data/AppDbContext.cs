using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using LembretesApi.Models;

namespace LembretesApi.Data
{
    public class AppDbContext : IdentityDbContext<Usuario>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Lembrete> Lembretes { get; set; }
        public DbSet<PushSubscription> PushSubscriptions { get; set; }

        // Converte todas as datas para UTC antes de salvar no PostgreSQL
        // CrateDB não suporta transações, então não usamos Database.BeginTransaction
        public override int SaveChanges()
        {
            ConvertDatesToUtc();
            // Não usar transações para CrateDB
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ConvertDatesToUtc();
            // Não usar transações para CrateDB
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ConvertDatesToUtc()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime))
                    {
                        var dateTime = (DateTime)property.CurrentValue!;
                        if (dateTime.Kind == DateTimeKind.Unspecified || dateTime.Kind == DateTimeKind.Local)
                        {
                            property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                        }
                    }
                    else if (property.Metadata.ClrType == typeof(DateTime?))
                    {
                        var nullableDateTime = (DateTime?)property.CurrentValue;
                        if (nullableDateTime.HasValue && 
                            (nullableDateTime.Value.Kind == DateTimeKind.Unspecified || nullableDateTime.Value.Kind == DateTimeKind.Local))
                        {
                            property.CurrentValue = DateTime.SpecifyKind(nullableDateTime.Value, DateTimeKind.Utc);
                        }
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurar relacionamento Usuario -> Lembretes
            builder.Entity<Lembrete>()
                .HasOne(l => l.Usuario)
                .WithMany(u => u.Lembretes)
                .HasForeignKey(l => l.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurar Horario como interval (PostgreSQL nativo)
            // Neon.tech suporta o tipo interval padrão do PostgreSQL
            builder.Entity<Lembrete>()
                .Property(l => l.Horario)
                .HasColumnType("interval");

            // Índices para melhor performance
            builder.Entity<Lembrete>()
                .HasIndex(l => l.UsuarioId);

            builder.Entity<Lembrete>()
                .HasIndex(l => l.Data);

            // Configurar relacionamento Usuario -> PushSubscriptions
            builder.Entity<PushSubscription>()
                .HasOne(ps => ps.Usuario)
                .WithMany()
                .HasForeignKey(ps => ps.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice para melhor performance
            builder.Entity<PushSubscription>()
                .HasIndex(ps => ps.UsuarioId);
        }
    }
}

