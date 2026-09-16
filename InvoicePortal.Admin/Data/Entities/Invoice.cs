using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("Amount", Name = "IX_Invoices_Amount")]
[Index("BottlerId", Name = "IX_Invoices_BottlerId")]
[Index("BrandSegmentCategoryId", Name = "IX_Invoices_BrandSegmentCategoryId")]
[Index("CalculatedAdminInvoiceStatus", Name = "IX_Invoices_CalculatedAdminInvoiceStatus")]
[Index("CalculatedBottlerInvoiceStatus", Name = "IX_Invoices_CalculatedBottlerInvoiceStatus")]
[Index("CancellationCategoryId", Name = "IX_Invoices_CancellationCategoryId")]
[Index("CompanyId", Name = "IX_Invoices_CompanyId")]
[Index("CountryId", Name = "IX_Invoices_CountryId")]
[Index("CreatedAt", Name = "IX_Invoices_CreatedAt")]
[Index("CurrencyId", Name = "IX_Invoices_CurrencyId")]
[Index("ExportedToSap", Name = "IX_Invoices_ExportedToSap")]
[Index("InvoiceFinalizedDetailsId", Name = "IX_Invoices_InvoiceFinalizedDetailsId")]
[Index("InvoiceStatus", Name = "IX_Invoices_InvoiceStatus")]
[Index("InvoiceType", Name = "IX_Invoices_InvoiceType")]
[Index("IsDeleted", Name = "IX_Invoices_IsDeleted")]
[Index("IsDeleted", "InvoiceStatus", Name = "IX_Invoices_IsDeleted_InvoiceStatus")]
[Index("Number", Name = "IX_Invoices_Number")]
[Index("PayerId", Name = "IX_Invoices_PayerId")]
[Index("ProgramId", Name = "IX_Invoices_ProgramId")]
[Index("RevisedInvoiceId", Name = "IX_Invoices_RevisedInvoiceId")]
[Index("SalesCenterId", Name = "IX_Invoices_SalesCenterId")]
[Index("SamplesPurposeId", Name = "IX_Invoices_SamplesPurposeId")]
[Index("SapInvoiceNumber", Name = "IX_Invoices_SapInvoiceNumber")]
[Index("UnlistedProgramTypeId", Name = "IX_Invoices_UnlistedProgramTypeId")]
[Index("UpdatedAt", Name = "IX_Invoices_UpdatedAt")]
[Index("UserEmail", Name = "IX_Invoices_UserEmail")]
[Index("UserId", Name = "IX_Invoices_UserId")]
public partial class Invoice
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "decimal(19, 4)")]
    public decimal? Amount { get; set; }

    public Guid UserId { get; set; }

    [StringLength(32)]
    public string? Number { get; set; }

    public int? BottlerId { get; set; }

    [StringLength(16)]
    public string? ProgramId { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? CoOpPercentage { get; set; }

    public int? AccountType { get; set; }

    public int? PayerId { get; set; }

    public int? SalesCenterId { get; set; }

    public int? BrandSegmentCategoryId { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public int InvoiceStatus { get; set; }

    public int? CurrencyId { get; set; }

    public bool AutoGenerateInvoiceAttachment { get; set; }

    public int? RevisedInvoiceId { get; set; }

    public DateTime? RevisionDate { get; set; }

    public int? RevisionReason { get; set; }

    public Guid? HistoryCorrelationId { get; set; }

    public int RevisionNumber { get; set; }

    public DateTime? ExportedToSap { get; set; }

    public int? ClaimId { get; set; }

    public DateOnly? PromoPeriodFrom { get; set; }

    public DateOnly? PromoPeriodTo { get; set; }

    public int? CompanyId { get; set; }

    [StringLength(256)]
    public string UserName { get; set; } = null!;

    [StringLength(256)]
    public string UserEmail { get; set; } = null!;

    [StringLength(16)]
    public string? SapInvoiceNumber { get; set; }

    public int? CountryId { get; set; }

    [StringLength(32)]
    public string InvoiceType { get; set; } = null!;

    [StringLength(4000)]
    public string? UnlistedProgramName { get; set; }

    public int? UnlistedProgramTypeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public int? InvoiceFinalizedDetailsId { get; set; }

    public bool HasPayment { get; set; }

    public int? CalculatedBottlerInvoiceStatus { get; set; }

    public int? CalculatedAdminInvoiceStatus { get; set; }

    public int? CancellationCategoryId { get; set; }

    [StringLength(11)]
    public string? PoNumber { get; set; }

    public int? SamplesPurposeId { get; set; }

    public bool? DoNotHavePoNumber { get; set; }

    [StringLength(16)]
    public string? RefDocNumber { get; set; }

    [Column(TypeName = "decimal(19, 4)")]
    public decimal? PoGrandTotal { get; set; }

    [StringLength(32)]
    public string? PoInvoiceNumber { get; set; }

    [ForeignKey("BottlerId")]
    [InverseProperty("Invoices")]
    public virtual Bottler? Bottler { get; set; }

    [ForeignKey("BrandSegmentCategoryId")]
    [InverseProperty("Invoices")]
    public virtual BrandSegmentCategory? BrandSegmentCategory { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("Invoices")]
    public virtual Company? Company { get; set; }

    [ForeignKey("CountryId")]
    [InverseProperty("Invoices")]
    public virtual Country? Country { get; set; }

    [ForeignKey("CurrencyId")]
    [InverseProperty("Invoices")]
    public virtual Currency? Currency { get; set; }

    [InverseProperty("RevisedInvoice")]
    public virtual ICollection<Invoice> InverseRevisedInvoice { get; set; } = new List<Invoice>();

    [ForeignKey("PayerId")]
    [InverseProperty("Invoices")]
    public virtual Payer? Payer { get; set; }

    [ForeignKey("ProgramId")]
    [InverseProperty("Invoices")]
    public virtual Program? Program { get; set; }

    [ForeignKey("RevisedInvoiceId")]
    [InverseProperty("InverseRevisedInvoice")]
    public virtual Invoice? RevisedInvoice { get; set; }

    [ForeignKey("SalesCenterId")]
    [InverseProperty("Invoices")]
    public virtual SalesCenter? SalesCenter { get; set; }
}
