using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("Code", Name = "IX_Currencies_Code", IsUnique = true)]
[Index("CreatedAt", Name = "IX_Currencies_CreatedAt")]
[Index("UpdatedAt", Name = "IX_Currencies_UpdatedAt")]
public partial class Currency
{
    [Key]
    public int Id { get; set; }

    [StringLength(3)]
    public string Code { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Currency")]
    public virtual ICollection<Bottler> Bottlers { get; set; } = new List<Bottler>();

    [InverseProperty("Currency")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
