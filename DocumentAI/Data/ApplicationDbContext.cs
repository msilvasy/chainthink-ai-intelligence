using DocumentAI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocumentAI.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserQuerySettings> UserQuerySettings => Set<UserQuerySettings>();
    public DbSet<Bm25IndexedChunk> Bm25IndexedChunks => Set<Bm25IndexedChunk>();
    public DbSet<QueryLog> QueryLogs => Set<QueryLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserQuerySettings>(entity =>
        {
            entity.ToTable("UserQuerySettings");
            entity.HasKey(settings => settings.UserId);
            entity.Property(settings => settings.Mode).HasMaxLength(20).IsRequired();
            entity.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<UserQuerySettings>(settings => settings.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Bm25IndexedChunk>(entity =>
        {
            entity.ToTable("Bm25IndexedChunks");
            entity.HasKey(chunk => chunk.ChunkId);
            entity.Property(chunk => chunk.ChunkId).HasMaxLength(200);
            entity.Property(chunk => chunk.UserId).HasMaxLength(450).IsRequired();
            entity.Property(chunk => chunk.DocumentId).HasMaxLength(200).IsRequired();
            entity.Property(chunk => chunk.DocumentName).HasMaxLength(500).IsRequired();
            entity.Property(chunk => chunk.Content).IsRequired();
            entity.Property(chunk => chunk.MetadataJson).IsRequired();
            entity.HasIndex(chunk => new { chunk.UserId, chunk.DocumentId });
        });

        builder.Entity<QueryLog>(entity =>
        {
            entity.ToTable("QueryLogs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.UserId).HasMaxLength(450).IsRequired();
            entity.Property(log => log.Query).HasMaxLength(2000).IsRequired();
            entity.Property(log => log.Response).IsRequired();
            entity.HasIndex(log => new { log.UserId, log.CreatedAt });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
