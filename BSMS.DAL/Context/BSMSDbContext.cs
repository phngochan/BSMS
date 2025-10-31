using BSMS.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Context;
public class BSMSDbContext : DbContext
{
    public BSMSDbContext(DbContextOptions<BSMSDbContext> options) : base(options) { }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ChangingStation> ChangingStations { get; set; }
    public DbSet<Battery> Batteries { get; set; }
    public DbSet<SwapTransaction> SwapTransactions { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<BatteryServicePackage> BatteryServicePackages { get; set; }
    public DbSet<UserPackage> UserPackages { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Support> Supports { get; set; }
    public DbSet<StationStaff> StationStaffs { get; set; }
    public DbSet<BatteryTransfer> BatteryTransfers { get; set; }
    public DbSet<Config> Configs { get; set; }
    public DbSet<StationStatistics> StationStatistics { get; set; }
    public DbSet<SystemReport> SystemReports { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<UserActivityLog> UserActivityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USER
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Role)
                  .HasConversion<string>()
                  .HasMaxLength(50);
        });

        // VEHICLE
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId);
            entity.HasIndex(e => e.Vin).IsUnique();
            entity.Property(e => e.Vin).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(v => v.User)
                  .WithMany(u => u.Vehicles)
                  .HasForeignKey(v => v.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CHANGING STATION
        modelBuilder.Entity<ChangingStation>(entity =>
        {
            entity.HasKey(e => e.StationId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);
        });

        // BATTERY
        modelBuilder.Entity<Battery>(entity =>
        {
            entity.HasKey(e => e.BatteryId);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);
            entity.HasOne(b => b.Station)
                  .WithMany(s => s.Batteries)
                  .HasForeignKey(b => b.StationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // SWAP TRANSACTION
        modelBuilder.Entity<SwapTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.SwapTime).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(s => s.User)
                  .WithMany(u => u.SwapTransactions)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Vehicle)
                  .WithMany()
                  .HasForeignKey(s => s.VehicleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Station)
                  .WithMany(st => st.SwapTransactions)
                  .HasForeignKey(s => s.StationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.BatteryTaken)
                  .WithMany()
                  .HasForeignKey(s => s.BatteryTakenId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.BatteryReturned)
                  .WithMany()
                  .HasForeignKey(s => s.BatteryReturnedId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Payment)
                  .WithMany(p => p.SwapTransactions)
                  .HasForeignKey(s => s.PaymentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // RESERVATION
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Reservations)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Vehicle)
                  .WithMany()
                  .HasForeignKey(r => r.VehicleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Station)
                  .WithMany(s => s.Reservations)
                  .HasForeignKey(r => r.StationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // BATTERY SERVICE PACKAGE
        modelBuilder.Entity<BatteryServicePackage>(entity =>
        {
            entity.HasKey(e => e.PackageId);
            entity.Property(e => e.Active).HasDefaultValue(true);
        });

        // USER PACKAGE
        modelBuilder.Entity<UserPackage>(entity =>
        {
            entity.HasKey(e => e.UserPackageId);
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(up => up.User)
                  .WithMany(u => u.UserPackages)
                  .HasForeignKey(up => up.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(up => up.Package)
                  .WithMany(p => p.UserPackages)
                  .HasForeignKey(up => up.PackageId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // PAYMENT
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.PaymentTime).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Method)
                  .HasConversion<string>()
                  .HasMaxLength(50);
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(p => p.User)
                  .WithMany(u => u.Payments)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // SUPPORT
        modelBuilder.Entity<Support>(entity =>
        {
            entity.HasKey(e => e.SupportId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Type)
                  .HasConversion<string>()
                  .HasMaxLength(50);
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(s => s.User)
                  .WithMany(u => u.Supports)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.Station)
                  .WithMany(st => st.Supports)
                  .HasForeignKey(s => s.StationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // STATION STAFF
        modelBuilder.Entity<StationStaff>(entity =>
        {
            entity.HasKey(e => e.StaffId);
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Role)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(ss => ss.User)
                  .WithMany(u => u.StationStaffs)
                  .HasForeignKey(ss => ss.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ss => ss.Station)
                  .WithMany(s => s.StationStaffs)
                  .HasForeignKey(ss => ss.StationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // BATTERY TRANSFER
        modelBuilder.Entity<BatteryTransfer>(entity =>
        {
            entity.HasKey(e => e.TransferId);
            entity.Property(e => e.TransferTime).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(t => t.Battery)
                  .WithMany()
                  .HasForeignKey(t => t.BatteryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.FromStation)
                  .WithMany(s => s.FromTransfers)
                  .HasForeignKey(t => t.FromStationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToStation)
                  .WithMany(s => s.ToTransfers)
                  .HasForeignKey(t => t.ToStationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CONFIG
        modelBuilder.Entity<Config>(entity =>
        {
            entity.HasKey(e => e.ConfigId);
        });

        // STATION STATISTICS
        modelBuilder.Entity<StationStatistics>(entity =>
        {
            entity.HasKey(e => e.StatId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(s => s.Station)
                  .WithMany(st => st.StationStatistics)
                  .HasForeignKey(s => s.StationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // SYSTEM REPORT
        modelBuilder.Entity<SystemReport>(entity =>
        {
            entity.HasKey(e => e.ReportId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.ReportType)
                  .HasConversion<string>()
                  .HasMaxLength(50);
            entity.HasOne(r => r.GeneratedByUser)
                  .WithMany()
                  .HasForeignKey(r => r.GeneratedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ALERT
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.AlertId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.AlertType)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(a => a.Station)
                  .WithMany(s => s.Alerts)
                  .HasForeignKey(a => a.StationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Battery)
                  .WithMany()
                  .HasForeignKey(a => a.BatteryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // USER ACTIVITY LOG
        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(l => l.User)
                  .WithMany()
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
