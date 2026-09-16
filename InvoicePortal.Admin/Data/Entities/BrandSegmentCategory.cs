using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("CompanyId", Name = "IX_BrandSegmentCategories_CompanyId")]
[Index("CreatedAt", Name = "IX_BrandSegmentCategories_CreatedAt")]
[Index("CreatedBy", Name = "IX_BrandSegmentCategories_CreatedBy")]
[Index("Name", Name = "IX_BrandSegmentCategories_Name", IsUnique = true)]
[Index("UpdatedAt", Name = "IX_BrandSegmentCategories_UpdatedAt")]
[Index("UpdatedBy", Name = "IX_BrandSegmentCategories_UpdatedBy")]
public partial class BrandSegmentCategory
{
    [Key]
    public int Id { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    public int CompanyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [StringLength(256)]
    public string CreatedBy { get; set; } = null!;

    public int Status { get; set; }

    [StringLength(256)]
    public string UpdatedBy { get; set; } = null!;

    [InverseProperty("BrandSegmentCategory")]
    public virtual ICollection<Bottler> Bottlers { get; set; } = new List<Bottler>();

    [ForeignKey("CompanyId")]
    [InverseProperty("BrandSegmentCategories")]
    public virtual Company Company { get; set; } = null!;

    [InverseProperty("BrandSegmentCategory")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
