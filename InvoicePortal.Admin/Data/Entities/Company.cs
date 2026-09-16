using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("CreatedAt", Name = "IX_Companies_CreatedAt")]
[Index("Name", Name = "IX_Companies_Name")]
[Index("UpdatedAt", Name = "IX_Companies_UpdatedAt")]
public partial class Company
{
    [Key]
    public int Id { get; set; }

    [StringLength(3)]
    public string Code { get; set; } = null!;

    [StringLength(50)]
    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Company")]
    public virtual ICollection<BrandSegmentCategory> BrandSegmentCategories { get; set; } = new List<BrandSegmentCategory>();

    [InverseProperty("Company")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
