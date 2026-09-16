using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("CreatedAt", Name = "IX_SalesCenters_CreatedAt")]
[Index("Name", Name = "IX_SalesCenters_Name")]
[Index("PayerId", Name = "IX_SalesCenters_PayerId")]
[Index("UpdatedAt", Name = "IX_SalesCenters_UpdatedAt")]
public partial class SalesCenter
{
    [Key]
    public int Id { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    [StringLength(128)]
    public string Address { get; set; } = null!;

    public int PayerId { get; set; }

    public bool IsNonVipShipTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("SalesCenter")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [ForeignKey("PayerId")]
    [InverseProperty("SalesCenters")]
    public virtual Payer Payer { get; set; } = null!;
}
