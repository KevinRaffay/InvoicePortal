using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("BrandSegmentCategoryId", Name = "IX_Bottlers_BrandSegmentCategoryId")]
[Index("CommercialManagerId", Name = "IX_Bottlers_CommercialManagerId")]
[Index("CountryId", Name = "IX_Bottlers_CountryId")]
[Index("CreatedAt", Name = "IX_Bottlers_CreatedAt")]
[Index("CurrencyId", Name = "IX_Bottlers_CurrencyId")]
[Index("Name", Name = "IX_Bottlers_Name")]
[Index("SalesOrganization", Name = "IX_Bottlers_SalesOrganization")]
[Index("UpdatedAt", Name = "IX_Bottlers_UpdatedAt")]
public partial class Bottler
{
    [Key]
    public int Id { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    [StringLength(4)]
    public string SalesOrganization { get; set; } = null!;

    public int CurrencyId { get; set; }

    public int CountryId { get; set; }

    public int? CommercialManagerId { get; set; }

    public int? BrandSegmentCategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("BrandSegmentCategoryId")]
    [InverseProperty("Bottlers")]
    public virtual BrandSegmentCategory? BrandSegmentCategory { get; set; }

    [ForeignKey("CountryId")]
    [InverseProperty("Bottlers")]
    public virtual Country Country { get; set; } = null!;

    [ForeignKey("CurrencyId")]
    [InverseProperty("Bottlers")]
    public virtual Currency Currency { get; set; } = null!;

    [InverseProperty("Bottler")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("Bottler")]
    public virtual ICollection<Payer> Payers { get; set; } = new List<Payer>();

    [InverseProperty("Bottler")]
    public virtual ICollection<Program> Programs { get; set; } = new List<Program>();
}
