using System;
using System.Collections.Generic;
using InvoicePortal.Admin.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data;

public partial class InvoicePortalDbContext : DbContext
{
    public InvoicePortalDbContext(DbContextOptions<InvoicePortalDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bottler> Bottlers { get; set; }

    public virtual DbSet<BrandSegmentCategory> BrandSegmentCategories { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Payer> Payers { get; set; }

    public virtual DbSet<Program> Programs { get; set; }

    public virtual DbSet<SalesCenter> SalesCenters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bottler>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<BrandSegmentCategory>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasDefaultValue("");
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedBy).HasDefaultValue("");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => e.PoNumber, "IX_Invoices_PoNumber").HasFilter("([PoNumber] IS NOT NULL)");

            entity.Property(e => e.CalculatedAdminInvoiceStatus).HasComputedColumnSql("(case when [ClaimId] IS NULL AND [InvoiceStatus]<>(3) AND [InvoiceStatus]<>(4) then (18) when [InvoiceStatus]=(3) then (3) else CONVERT([int],[InvoiceStatus]) end)", false);
            entity.Property(e => e.CalculatedBottlerInvoiceStatus).HasComputedColumnSql("(case when [InvoiceStatus]=(15) OR [InvoiceStatus]=(14) OR [InvoiceStatus]=(13) OR [InvoiceStatus]=(12) OR [InvoiceStatus]=(11) OR [InvoiceStatus]=(10) OR [InvoiceStatus]=(8) OR [InvoiceStatus]=(7) OR [InvoiceStatus]=(6) then (0) when [InvoiceStatus]=(19) OR [InvoiceStatus]=(16) OR [InvoiceStatus]=(9) then (1) when [InvoiceStatus]=(17) AND [HasPayment]=(0) then (17) when [InvoiceStatus]=(17) AND [HasPayment]=(1) then (2) else CONVERT([int],[InvoiceStatus]) end)", false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SapInvoiceNumber).HasComputedColumnSql("(case when len([Number])>(0) then substring([Number],(1),(16))  end)", false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UserEmail).HasDefaultValue("");
            entity.Property(e => e.UserName).HasDefaultValue("");
        });

        modelBuilder.Entity<Payer>(entity =>
        {
            entity.HasIndex(e => e.VendorId, "IX_Payers_VendorId")
                .IsUnique()
                .HasFilter("([VendorId] IS NOT NULL)");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasDefaultValue("");
            entity.Property(e => e.City).HasDefaultValue("");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DoNotUse).HasDefaultValue(false, "DF_Payers_DoNotUse");
            entity.Property(e => e.State).HasDefaultValue("");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ZipCode).HasDefaultValue("");
        });

        modelBuilder.Entity<Program>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<SalesCenter>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
