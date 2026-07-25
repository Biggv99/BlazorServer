using Microsoft.EntityFrameworkCore;
using OptimusPrime.Features.Contracts.Models;

namespace OptimusPrime.Infrastructure.Database;

public class AppDbcontext(DbContextOptions<AppDbcontext> options) : DbContext(options) {

    // ==========================
    // PostgreSQL Tables
    // ==========================

    public DbSet<UserModel> User { get; set; }
    public DbSet<UserDeviceModel> Devices { get; set; }
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
            .Property(u => u.Role).HasConversion<string>();

        // database constraint
        modelBuilder.Entity<UserModel>().ToTable(t => t.HasCheckConstraint(
            "CK_User_Role",
            "\"Role\" IN ('Admin', 'Editor', 'Viewer')"
        ));

        // ==========================
        // User Devices
        // ==========================

        // unique device 
        modelBuilder.Entity<UserDeviceModel>()
            .HasIndex(d => d.DeviceId).IsUnique();

        // delete user devices when user is deleted
        modelBuilder.Entity<UserDeviceModel>()
            .HasOne<UserModel>() 
            .WithMany()          
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==========================
        // Refresh Token
        // ==========================

        // delete refresh tokens when user is deleted
        modelBuilder.Entity<RefreshTokenModel>()
            .HasOne<UserModel>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==========================
        // Audit Logs
        // ==========================

        // set null on user deletion
        modelBuilder.Entity<AuditModel>()
            .HasOne<UserModel>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);


    }
}