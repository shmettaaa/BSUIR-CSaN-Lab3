using Microsoft.EntityFrameworkCore;
using CSaN_Lab3_Backend.Entities;

namespace CSaN_Lab3_Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<FileMetadata> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileMetadata>()
            .HasIndex(f => f.RelativePath)
            .IsUnique();

        modelBuilder.Entity<FileMetadata>(entity =>
        {
            entity.Property(f => f.RelativePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(f => f.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(f => f.ContentType)
                .HasMaxLength(100);
        });
    }
}