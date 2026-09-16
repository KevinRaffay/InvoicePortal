using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("BottlerId", Name = "IX_Payers_BottlerId")]
[Index("CreatedAt", Name = "IX_Payers_CreatedAt")]
[Index("Name", Name = "IX_Payers_Name")]
[Index("UpdatedAt", Name = "IX_Payers_UpdatedAt")]
public partial class Payer
{
    [Key]
    public int Id { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    public int BottlerId { get; set; }

    [StringLength(128)]
    public string Address { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [StringLength(16)]
    public string ZipCode { get; set; } = null!;

    [StringLength(128)]
    public string City { get; set; } = null!;

    [StringLength(5)]
    public string State { get; set; } = null!;

    public int? StatementContactId { get; set; }

    public int? VendorId { get; set; }

    public bool? DoNotUse { get; set; }

    [ForeignKey("BottlerId")]
    [InverseProperty("Payers")]
    public virtual Bottler Bottler { get; set; } = null!;

    [InverseProperty("Payer")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("Payer")]
    public virtual ICollection<SalesCenter> SalesCenters { get; set; } = new List<SalesCenter>();
}
