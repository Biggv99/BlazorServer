using Microsoft.EntityFrameworkCore;
using OptimusPrime.Features.Contracts.Models;

namespace OptimusPrime.Infrastructure.Database;

public class AppDbcontext(DbContextOptions<AppDbcontext> options) : DbContext(options) {

    // ==========================
    // PostgreSQL Tables
    // ==========================

    public DbSet<UserModel> User { get; set; }
    public DbSet<RefreshTokenModel> Refresh { get; set; }
    public DbSet<AuditModel> Audit { get; set; }

    // ==========================
    // Model Configuration
    // ==========================
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // ==========================
        // User
        // ==========================

        // unique email
        modelBuilder.Entity<UserModel>()
            .HasIndex(u => u.Email).IsUnique();

        // role conversion to string
        modelBuilder.Entity<UserModel>()
            .Property(u => u.Ruolo).HasConversion<string>();

        // database constraint
        modelBuilder.Entity<UserModel>().ToTable(t => t.HasCheckConstraint(
            "CK_User_Ruolo",
            "\"Ruolo\" IN ('Admin', 'Editor', 'Viewer')"
        ));
    }
}