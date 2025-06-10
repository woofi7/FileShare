using FileShare.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileShare.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<DownloadLogEntity> DownloadLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DownloadToken).IsUnique();
            entity.Property(e => e.OriginalName).HasMaxLength(255);
            entity.Property(e => e.StoredName).HasMaxLength(255);
            entity.Property(e => e.DownloadToken).HasMaxLength(36);
        });
        
        modelBuilder.Entity<DownloadLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.File)
                .WithMany(f => f.DownloadLogs)
                .HasForeignKey(e => e.FileId);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
        });
    }
}