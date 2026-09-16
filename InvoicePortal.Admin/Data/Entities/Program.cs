using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("BottlerId", Name = "IX_Programs_BottlerId")]
[Index("CreatedAt", Name = "IX_Programs_CreatedAt")]
[Index("FundingElementId", Name = "IX_Programs_FundingElementId")]
[Index("UpdatedAt", Name = "IX_Programs_UpdatedAt")]
public partial class Program
{
    [Key]
    [StringLength(16)]
    public string Id { get; set; } = null!;

    [StringLength(4000)]
    public string Name { get; set; } = null!;

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int BottlerId { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal CoOpPercentage { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CoOpAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AmountSettled { get; set; }

    public int FundingElementId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("BottlerId")]
    [InverseProperty("Programs")]
    public virtual Bottler Bottler { get; set; } = null!;

    [InverseProperty("Program")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
